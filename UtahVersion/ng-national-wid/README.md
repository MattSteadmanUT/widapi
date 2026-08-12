# @ulmita/ng-national-wid — Component Architecture

This document explains what the library actually renders and how its pieces fit together. For
"how do I install and use this in my app," see [INTEGRATION.md](./INTEGRATION.md) instead — that
guide is unchanged from how Utah shipped it and remains accurate. For "how do I build this from
source in my own workspace," see [docs/standalone-build-guide.md](./docs/standalone-build-guide.md).

## What this is

A single Angular component (`<nwid-national-wid>`) that renders a complete, self-contained data
explorer UI for the National WID 3.0 API — every core table, lookup table, and view the API
exposes, each behind a query-builder form with pagination, sorting, and CSV/TSV/PSV/XLSX/JSON
export. It was built as a reference client and internal test harness for the API as much as a
production UI component; it exercises effectively every query parameter the API supports.

## Files

| File | Role |
|---|---|
| `src/public-api.ts` | Package entry point — re-exports everything below |
| `src/lib/national-wid.module.ts` | `NationalWidModule` — the Angular module consumers import |
| `src/lib/national-wid.component.ts` | The data explorer itself (~720 lines) |
| `src/lib/national-wid.component.html` | Its template (~1400 lines) |
| `src/lib/national-wid.component.css` | Scoped styles (layout only — visual styling leans on W3.CSS) |
| `src/lib/national-wid-api.service.ts` | Thin HTTP client used internally by the component |
| `src/lib/national-wid-config.ts` | `NationalWidConfig`, `NATIONAL_WID_CONFIG`, `provideNationalWid()` |

## `NationalWidComponent` — the data explorer

### Tabs

The component is a single Angular component with client-side tab switching (`activeTab: TabId`,
no router dependency). Each tab is a full query-builder + results-table + export UI wired to one
or more API endpoints:

| Tab | `TabId` | API endpoint(s) |
|---|---|---|
| Status | `status` | `GET /status`, `GET /status?report=history&dataSet=...` |
| Labor Force (LAUS) | `laus` | `GET /labor-force`, `/labor-force/metadata` |
| CES | `ces` | `GET /ces`, `/ces/metadata` |
| Industry (QCEW) | `industry` | `GET /industry`, `/industry/metadata` |
| Wages (OEWS) | `wages` | `GET /wages`, `/wages/metadata` |
| Projections | `projections` | `GET /projections`, `/projections/metadata` |
| Licensing | `licensing` | Selectable endpoint: `licensing/licenses`, `/authorities`, `/history`, `/occupationCrosswalks` |
| CPI | `cpi` | Selectable endpoint: `cpi`, `/metadata`, `/items`, `/areas` |
| Non-Core Lookups | `nonCoreLookups` | Selectable from all 18 `/lookups/*` and `/projections/matrixX*` endpoints |
| Non-Core Views | `nonCoreViews` | Selectable from all 6 `/views/*` endpoints |

The **Status** tab is the landing view (`ngOnInit` calls `loadStatus()`). It renders per-dataset
freshness cards (`GET /status`) split into core vs. non-core (`coreDatasets` / `nonCoreDatasets`
getters filter on `tableClass`), and each card is clickable — `goToDatasetControls()` maps a
dataset name back to its tab and endpoint (via `lookupEndpointByDataset` / `viewEndpointByDataset`
lookup tables) and jumps the user there, auto-scrolling to that tab's controls
(`selectTabAndScroll`, keyed by `controlsElementByTab`). Clicking a status card that isn't a
queryable dataset (e.g. an operational-only entry) instead expands its ingestion history inline
(`toggleStatusHistory` → `loadStatusHistory`, `GET /status?report=history&dataSet=X&page=1&pageSize=50`).

### Per-table state pattern

Every table tab follows the same shape, which is why the component reads as repetitive but is
actually one pattern applied ten times:

- A `TableState` object (`{ loading, rows, total, page, pageSize, error, downloading, downloadError }`)
  — one instance per tab (`lausState`, `cesState`, `indState`, `wageState`, `projState`,
  `licensingState`, `cpiState`, `lookupState`, `viewState`), created by `freshState()`.
- A `*Params` object holding the current filter form values for that tab (e.g. `lausParams`,
  `cesParams`).
- A `*Sort: SortControl` (`{ field, direction }`) plus a `*SortFields` list that drives the sort
  dropdown in the template.
- A `query*(page)` method that increments/sets `state.page`, then calls the shared
  `runQuery(state, endpoint, params)` helper, which builds the query string
  (`buildPath` — merges params + page/pageSize, URL-encodes, drops blanks), calls
  `apiService.get<any>(path)`, and unpacks the standard `{ meta, data }` envelope into
  `state.rows` / `state.total`.
- Core-table tabs (LAUS/CES/Industry/Wages/Projections) additionally have a `*Metadata:
  MetadataState` and call `loadMetadata()` alongside `runQuery()` — this hits the endpoint's
  `/metadata` sub-route (`buildMetadataPath` always requests
  `areas,years,periods,minPeriod,maxPeriod,projectedYears`) to show the user what area/year
  coverage exists for their current filter before they page through results.
- A `download*(format)` method that reuses the same params/sort via `doDownload()`, which appends
  `format=` to the query string and delegates to `NationalWidApiService.download()` for the actual
  blob-download/browser-save-dialog behavior.

Licensing, CPI, non-core-lookups, and non-core-views tabs are a variant of the same pattern where
the **endpoint itself is a dropdown-selectable param** (`licensingParams.endpoint`,
`cpiParams.endpoint`, `lookupParams.endpoint`, `viewParams.endpoint`) rather than fixed — the
`endpoint` field is destructured out of the params object before building the query string so it
doesn't leak into the filter query params.

### Formatting helpers

`fNum`, `fCur`, `fPct`, `fAdj`, `fSupp` in the template layer format raw API values for display:
locale-formatted numbers, currency, percentages, `SA`/`NSA` adjustment codes, and a `⚑` marker for
BLS-suppressed values (`supprecord = '1'`). `objectKeys(row)` backs a fallback generic-table
renderer for endpoints whose shape isn't hardcoded into the template.

## `NationalWidApiService`

Injectable (`providedIn: 'root'`), also exported for advanced consumers who want to call the API
directly rather than through the component (see [INTEGRATION.md](./INTEGRATION.md#nationalwidapiservice-advanced)).

- `get<T>(path)` — resolves `config.getToken()`, attaches it as `Authorization` (auto-detects
  whether the caller already supplied a `Bearer `/`ApiKey ` prefix and only adds `Bearer ` if
  not), does the `HttpClient.get`, and returns a uniform `{ success, data?, error? }` result
  instead of throwing — every call site in the component can `if (result.success)` rather than
  wrapping in try/catch.
- `download(path, fileName)` — same auth handling, but requests `responseType: 'blob'`,
  reads the actual filename from the `Content-Disposition` response header when the API supplies
  one (falls back to the caller's suggested name), and drives a synthetic `<a download>` click to
  trigger the browser's save flow.
- `extractErrorMessage(err)` — normalizes ASP.NET Core `ProblemDetails` error shapes
  (`err.error.title`, `err.error.message`) and generic HTTP errors into one string for display.

## Configuration (`NationalWidConfig`)

Two fields, both required: `apiBaseUrl` (trailing slash required) and `getToken: () =>
Promise<string | null>`. There is deliberately no built-in auth implementation — the library is
auth-agnostic by design so it can sit behind either a Cognito JWT (Utah's ULMITA deployment) or a
National WID API key (`ApiKey <key>` — see the API's [API Keys section](../fed-national-wid/README.md#api-keys)),
or whatever auth scheme the host application already has. `provideNationalWid(config)` is a small
factory that wraps the `NATIONAL_WID_CONFIG` `InjectionToken` so consumers don't need to import
the token directly.

## What North Carolina will need to decide

- **Branding/styling**: the component leans on W3.CSS + Font Awesome classes rather than shipping
  its own design system. If NC's portal has its own component library / design tokens, expect to
  either keep loading these two CDN dependencies (see `INTEGRATION.md` step 4) or restyle
  `national-wid.component.css` and the `w3-*` classes in the template to match.
- **No router dependency by design**: `(navigateBack)` is the only navigation hook. This was a
  deliberate choice so the library doesn't force a routing strategy on the host app — preserve
  this if extending the component rather than reaching for `@angular/router` inside it.
- **Angular version**: `package.json` declares `peerDependencies` of `>=17.0.0` for
  `@angular/common`/`core`/`forms`, but the library was last built and tested against Angular 21
  (see the standalone-build-guide for what that means for reproducing the build).
