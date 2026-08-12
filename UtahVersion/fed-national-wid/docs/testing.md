# Testing

Two xUnit projects: `tests/NationalWid.Api.Tests/` and `tests/NationalWid.Ingestion.Tests/`. Run
both with `just test` (or `dotnet test` directly); `just test-coverage` adds Cobertura output to
`./TestResults/`.

## `NationalWid.Api.Tests` — 13 files

Tests here target the shared query engine and services described in
[architecture.md](./architecture.md), using in-memory `IQueryable<T>` collections rather than a
real Postgres connection — this works specifically because `QueryService.CountAsync`/`ToListAsync`
fall back to synchronous LINQ-to-objects evaluation when the query provider isn't EF Core's
`IAsyncQueryProvider` (see `QueryService.cs`). No test database or Testcontainers setup exists or
is needed for this project.

| File | Covers |
|---|---|
| `QueryFilteringTests.cs` | CSV multi-value filters, wildcard patterns, exact/StartsWith/EndsWith/Contains matching |
| `QuerySortingTests.cs` | `sort=field:asc,field2:desc` parsing, case/casing-insensitive field resolution |
| `QueryServiceTests.cs` | Page-number and cursor pagination, the "can't specify both" validation |
| `CursorCodecTests.cs` | Cursor encode/decode round-tripping, malformed-cursor rejection |
| `ResponseEnvelopeTests.cs` | `{ meta, data, links }` shape, `links.next` cursor-form construction |
| `WidCodesTests.cs` | stFips validation, area left-padding, SOC-hyphen normalization |
| `StatusServiceTests.cs` | Dataset-name canonicalization/aliasing (the `"OES"` → `IOWage` etc. mapping) |
| `DownloadServiceTests.cs` | Format resolution, CSV/XLSX rendering, spreadsheet-formula-injection protection |
| `LicenseModelTests.cs` | License model shape/parsing |
| `UnitTest1.cs` | Placeholder scaffold test (from the original `dotnet new xunit` template) — safe to remove once real coverage exists in its place, or leave as a harmless no-op |

**What's not covered here**: no controller-level integration tests exist (no `WebApplicationFactory`
/ in-process HTTP test harness) — the query engine is unit tested, but a full request through
`Program.cs`'s auth/CORS/response-caching pipeline into a controller action is not exercised by
any automated test. If NC wants confidence that, say, the Cognito JWT audience validator or the
`FallbackPolicy` actually rejects unauthenticated requests end-to-end, that's currently verified by
manual testing only (see the auth verification checklist in the historical handoff notes) — adding
an integration test suite here would be the highest-leverage testing gap to close.

## `NationalWid.Ingestion.Tests` — 5 files

| File | Covers |
|---|---|
| `BlsCpiIngestorTests.cs` | The only **ingestor-class-level** test in either project — exercises `BlsCpiIngestor` logic |
| `BlsFlatFileServiceTests.cs` | The shared BLS flat-file downloader/parser (`TryLoadSeriesAsync`/`ProcessDataFileAsync`), using fixture data (an `LNS11000000`-style series) |
| `BlsNumericParserTests.cs` | Decimal parsing / BLS placeholder-value rejection |
| `BlsPeriodMapperTests.cs` | BLS period code → WID `(periodType, period)` mapping, including the `M13`/`Q5` annual-average special cases |
| `ParameterPathTests.cs` | `Function.GetParameterPath` — env var override / fallback / trimming logic for resolving Parameter Store paths |
| `UnitTest1.cs` | Placeholder scaffold test |

**The real gap**: LAUS, CES, QCEW, OEWS, Projections, and WID Center Lookups — six of the seven
ingestors — have **no dedicated test file at the ingestor-class level**. Their correctness rests on
the shared-dependency tests above plus whatever manual verification happened during past sessions
(documented in [HANDOFF.md](../../HANDOFF.md)). Concretely, nothing in the automated test suite
would catch a regression in:
- LAUS/CES's three-tier flat-file → API → API fallback logic
- QCEW's area-code resolution (national/state/county/MSA) or the `codetype`/`indcodetype`
  ON CONFLICT mismatch flagged in [database-schema.md](./database-schema.md)
- OEWS's `SafeFetchWorkbookAsync` per-file error isolation actually working as intended
- Projections' pagination-until-exhausted loop or its occupation-title cross-write into
  `occdirectories`
- WID Center Lookups' nested-zip unwrapping or area-code remapping switch

If NC's first project here is "make this pipeline trustworthy to hand off further," writing
ingestor-level unit tests for these six — probably using recorded fixture responses the same way
`BlsFlatFileServiceTests.cs` already does — is the single highest-value place to start.

## Running a single test / filtering

Standard `dotnet test` filtering works, e.g.:

```powershell
dotnet test --filter "FullyQualifiedName~QueryFilteringTests"
dotnet test tests/NationalWid.Ingestion.Tests
```

No custom test runner configuration beyond what `dotnet test` provides out of the box.
