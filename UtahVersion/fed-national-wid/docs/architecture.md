# API Architecture

This document explains the design of `NationalWid.Api` — the parts that aren't obvious from
reading a single controller in isolation. If you're new to the codebase, read this before making
changes to filtering, sorting, pagination, or auth; almost everything here is shared machinery
that all 11 controllers sit on top of.

## The core idea: one generic query engine, eleven thin controllers

Every list endpoint (`CesController`, `LaborForceController`, `IndustryController`,
`WagesController`, `ProjectionsController`, `LookupController`, `ViewsController`,
`LicensingController`, `CpiController`, plus the API-key and health endpoints) follows the exact
same five-step shape:

```
IQueryable<T> from DbSet
  → apply filters (QueryFiltering / WidCodes)
  → apply sort (QuerySorting)
  → branch: download format? → DownloadService renders CSV/TSV/PSV/XLSX/JSON and returns
           : JSON?           → QueryService.PageAsync paginates, ResponseEnvelope wraps it
```

Nothing here uses reflection-heavy generic controllers or a runtime table registry — each
controller still writes out its own parameter list and filter calls explicitly (see
`CesController.Get` for the canonical example). What's shared is the *machinery each filter/sort/
page/export call is built on*, in `src/NationalWid.Api/Services/`:

| File | Responsibility |
|---|---|
| `QueryFiltering.cs` | Builds `Expression<Func<T,bool>>` predicates dynamically per-property, for CSV multi-value + wildcard string filters |
| `QuerySorting.cs` | Builds dynamic `OrderBy`/`ThenBy` expression chains from a `sort=field:asc,field2:desc` string |
| `WidCodes.cs` | Domain-specific value normalization (stFips validation, area left-padding, SOC-hyphen stripping) applied *before* filters run |
| `CursorCodec.cs` | Opaque pagination cursor encode/decode |
| `IQueryService` / `QueryService.cs` | Turns an already-filtered-and-sorted `IQueryable<T>` into a page of results, page- or cursor-based |
| `ResponseEnvelope.cs` | Wraps a `PagedResult<T>` into the public `{ meta, data, links }` JSON shape |
| `IDownloadService` / `DownloadService.cs` | Format negotiation + CSV/TSV/PSV/XLSX/JSON-file rendering for any `T` |
| `TableCatalog.cs` | Static registry of every WID table → endpoint → core/non-core classification, used by `/status` and `GET /` discovery |
| `LookupCatalog.cs` | Static reference data that has no natural BLS source table (area type titles, state FIPS names, ownership titles, wage source/rate-type/growth-code titles) |
| `MetadataFields.cs` | Validates/parses the `metadataFields=` param on `/metadata` sub-endpoints |

### Why expression trees instead of a switch statement per filter

`QueryFiltering.ApplyStringFilter<T>(query, propertyName, raw, normalizer, allowWildcard)` takes a
**property name as a string**, not a typed lambda, and builds `x => x.PropertyName == value` (or
`.StartsWith`/`.EndsWith`/`.Contains` for wildcards) via `System.Linq.Expressions` at runtime. This
is what lets all 11 controllers apply CSV-multi-value + wildcard filtering to any of their own
columns with a one-line call (`QueryFiltering.ApplyStringFilter(query, nameof(Ces.SeriesCode),
seriesCode, WidCodes.NormalizeCodePattern, allowWildcard: true)`) instead of hand-writing the
`Where` clause and the OR-across-CSV-values loop for every filterable field on every table. The
same reasoning applies to `QuerySorting.Apply<T>`, which resolves a client-supplied `sort=`
string against `T`'s public properties via a case/casing-insensitive property map (accepts
`PeriodYear`, `periodYear`, or `periodyear` — see `NormalizeFieldToken`) and builds the
`OrderBy`/`ThenBy` chain reflectively.

**Trade-off to know about:** because this is `System.Linq.Expressions` translated by EF Core's
Npgsql provider, every filter/sort call has to produce an expression EF can translate to SQL. If
you add a filter over a computed/non-column property, EF will throw at query-execution time, not
compile time — there's no static check that a `nameof(Model.Property)` passed to
`QueryFiltering`/`QuerySorting` is actually EF-mapped and translatable. `QueryServiceTests.cs`,
`QueryFilteringTests.cs`, and `QuerySortingTests.cs` cover the engine itself against in-memory
`IQueryable`s (note `QueryService.CountAsync`/`ToListAsync` explicitly fall back to synchronous
LINQ-to-objects when `query.Provider` isn't an EF `IAsyncQueryProvider`, precisely so these unit
tests can run without a real Postgres connection) — extend those, not per-controller tests, when
touching the engine.

### Pagination: page-number and cursor are the same underlying mechanism

`QueryService.PageAsync` accepts either `page`/`pageSize` or `cursor`, never both (throws
`BadHttpRequestException` if both are supplied). Internally there is no separate cursor code path —
a page-number request computes an `offset` the same way a cursor request decodes one; the cursor
itself (`CursorCodec`) is just a base64url JSON blob of `{ "o": offset, "s": pageSize }` over a
**stable ordering** (the query must already be ordered before `PageAsync` is called — that's why
every controller calls `QuerySorting.Apply` before `queryService.PageAsync`). This means cursors
are not resumable across a *different* sort order, and are computed identically to an offset-based
page — this is offset pagination with an opaque token, not a true keyset/seek pagination
implementation. That's a reasonable trade-off for BLS data (append-mostly, rarely reordered
mid-scroll) but would need to change if a table with heavy concurrent writes were added to this
model. `ResponseEnvelope.Create` always emits `links.next` using cursor form regardless of how the
current page was requested, so clients that page forward via `links.next` naturally end up on
cursor pagination even if they started with `?page=1`.

## Auth: dual scheme, JWT by default, API key as a fallback

`Program.cs` registers **two** authentication schemes and lets ASP.NET Core's scheme chaining pick
the right one per-request:

1. **JWT bearer, any OIDC-compliant provider** (`JwtBearerDefaults.AuthenticationScheme`, the
   default) — standard ASP.NET Core `JwtBearer`/OIDC validation, not Cognito-specific machinery.
   By default the issuer URL is built from `Cognito:Region`/`Cognito:UserPoolId` in Cognito's
   URL shape (`https://cognito-idp.{region}.amazonaws.com/{userPoolId}`), which is what Utah's
   deployment uses today. An explicit `Oidc:Authority` configuration value — added as part of this
   handoff — overrides that construction entirely, so pointing *this application code* at Auth0,
   Okta, Azure AD B2C, Keycloak, a self-hosted IdP, or any other OIDC-compliant provider is a
   **configuration change, not a code change**. As deployed on AWS today, though, API Gateway's own
   native JWT authorizer sits in front of this code and validates independently with a
   Cognito-only issuer shape hardcoded into `lambda.template` — that layer *does* need a template
   edit for a non-Cognito provider, even though `Program.cs` doesn't. See
   [deployment-and-operations.md](./deployment-and-operations.md#platform-portability--whats-aws-specific-vs-portable)
   for the full picture of which layer needs what. Validation itself happens against that issuer's OIDC discovery document
   (`{issuer}/.well-known/openid-configuration`), with one small Cognito-specific fallback in the
   `AudienceValidator` — Cognito *access* tokens (as opposed to ID tokens) carry the app client ID
   in a `client_id` claim rather than the standard `aud` claim, so that claim is checked as a
   fallback after the standard `aud` check — see `Program.cs:50-57`. That fallback is harmless
   against providers that don't set a `client_id` claim, so it doesn't need to be removed for a
   non-Cognito provider to work. See
   [deployment-and-operations.md](./deployment-and-operations.md#platform-portability--whats-aws-specific-vs-portable)
   for the full portability picture (this API isn't just auth-portable — it isn't AWS-locked
   either), and the Cognito checklist item in
   [HANDOFF.md](../../HANDOFF.md#infrastructure--access-transition-checklist) for the actual
   decision NC needs to make: continue sharing Utah's ULMITA pool, stand up a separate pool
   (Cognito or otherwise), or trust both at once (`ClientIds` already accepts a list). None of
   those are more than a config change. `CognitoClaimsExtensions.cs` exposes typed extension
   methods for reading custom claims off the token (`custom:stFips`, `custom:ulmita_activated`,
   `custom:ulmita_roles`) for any future use, but the access model itself is intentionally simple:
   the JWT auth requirement is "any authenticated user from the configured pool"
   (`RequireAuthenticatedUser()` fallback policy) — every authenticated caller can query any
   state's data. There is no per-state data restriction, by design; this is a nationally-hosted
   public-data API, not a multi-tenant system that needs to wall states off from each other's
   queries.
2. **API key** (`ApiKeyAuthHandler`, scheme name `"ApiKey"`) — recognizes an `Authorization: ApiKey
   <key>` header, hashes the presented key (SHA-256) and looks it up via `ApiKeyService`. If no
   `ApiKey ` header is present it returns `AuthenticateResult.NoResult()` rather than failing, so
   the JWT handler remains the default for ordinary browser/interactive callers — the two schemes
   don't fight over the same request. API-key callers get **global (unrestricted) access**: no
   `custom:stFips` claim is attached (see the doc comment in `ApiKeyAuthHandler.cs`), by design,
   since API keys represent automation/external systems rather than a specific state user.

`ApiKeyService` never stores the plaintext key — only a SHA-256 hex digest and an 8-character
`keyPrefix` for display purposes. The plaintext is returned exactly once, at creation
(`POST /api-keys`). See the README's [API Keys](../README.md#api-keys) section for the HTTP
contract; `ApiKeyService.cs` is the implementation to read if extending key lifecycle behavior
(status transitions are validated in `PatchAsync` — a `revoked` key can never be reactivated to
`active`, only `active ⇄ disabled`).

`FallbackPolicy = RequireAuthenticatedUser()` in `Program.cs` applies to every controller by
default; individual actions opt out with `[AllowAnonymous]` (used on `HealthController` and parts
of `StatusService`'s consumers) rather than the more common pattern of opting *in* per-controller
— worth knowing if you add a new controller and forget the implication is "authenticated by
default."

## Response envelope and downloads

Every list endpoint returns `{ meta: { total, page, pageSize, nextCursor, message }, data: [...],
links: { self, next } }` (`ResponseEnvelope.Create`) or, for single-object endpoints like
`/status` and `/{table}/metadata`, `{ meta, data: {...}, links }` (`ResponseEnvelope.CreateObject`).
`meta.message` is populated with a human-readable "No records found with the supplied filters."
only when `total == 0` — a UX nicety for the Angular client rather than a machine-readable error
code.

`DownloadService` is generic over any row type `T` via reflection (`ColumnsFor<T>` caches
`PropertyInfo[]` per type in a `ConcurrentDictionary`, using each property's
`JsonPropertyNameAttribute` if present, else camelCase — so exported column headers match the JSON
field names the API otherwise returns). All delimited formats share one code path
(`DelimitedResult` parameterized by delimiter character); XLSX uses ClosedXML and includes basic
spreadsheet-injection protection (`SanitizeSpreadsheetText`/`NeedsFormulaProtection` — prefixes
values starting with `=`, `+`, `-`, or `@` with a `'` so Excel doesn't interpret them as formulas
when a user opens an exported file). Format is resolved by `?format=` query param first, falling
back to the `Accept` header, defaulting to JSON (`DownloadService.ResolveFormat`). Export size is
capped by `Api:ExportRowLimit` (default 100,000 rows, see `CesController.Get` for the pattern
every controller repeats) — there's no streaming export; the full row set is materialized in
memory before being written out, which is the thing to revisit first if NC needs to export tables
larger than that comfortably fits.

## Database mapping conventions

`WIDDbContext.OnModelCreating` maps each entity to an explicit lowercase table name and composite
primary key matching the WID 3.0 natural key for that table (e.g. `Ces` is keyed on `(StFips,
AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period, SeriesCodeType, SeriesCode,
Adjusted)` — no surrogate key). A model-wide loop at the bottom of `OnModelCreating` force-lowercases
every column name so raw SQL in migrations never needs quoted identifiers to match EF's generated
SQL. Two columns carry intentional, documented typos preserved from the WID spec / an earlier
migration for backward compatibility rather than "fixed": `laborforce.emppoproatio` (extra "r") and
`iowage.userdefinedranagemean` ("ranage" instead of "range") — both called out with inline comments
in `WIDDbContext.cs` specifically so a future contributor doesn't "fix" the typo and break existing
data/migrations. See [database-schema.md](./database-schema.md) for the full table inventory and
migration history.

## Status/health surface

`StatusService` (`GET /status`) is the one place that reasons about ingestion freshness rather than
WID data itself. It reads the most recent 200 `IngestLog` rows, groups them by a **canonicalized**
dataset name (`CanonicalizeDataSet` — collapses historical aliases like `"OES"`, `"OEWS"`, and
`"OEWS (IOWAGE)"` down to the current canonical `IOWage`, because the ingestion Lambda's dataset
naming changed over the project's life — see `ingestion-pipeline.md`), and joins that against
`TableCatalog.Entries` to report one freshness row per implemented table, including tables that
have never had an ingest log entry (falls back to a live `COUNT(*)` via `ResolveTotalRecordsAsync`).
`GetHistoryAsync` and the alias table (`GetAliases`) exist so `/status?report=history&dataSet=X`
keeps working with either the old or new dataset label a client might pass. If you rename a dataset
again, update `CanonicalizeDataSet`/`GetAliases` in the same change or `/status` will silently
double-count or drop history for that dataset.
