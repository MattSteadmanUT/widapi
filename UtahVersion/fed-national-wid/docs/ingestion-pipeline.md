# Ingestion Pipeline

Deep dive into `NationalWid.Ingestion` — the Lambda that populates every table this API serves.
Written so North Carolina doesn't have to re-derive source quirks Utah already discovered the hard
way. If you're auditing or extending an ingestor, read this first, then the ingestor's own source
— all seven live in `src/NationalWid.Ingestion/Ingestors/`.

## How a run is dispatched (`Function.cs`)

EventBridge Scheduler invokes the Lambda with an `IngestRequest` payload. `Function.cs` routes on
`request.DatasetGroup`:

- **Empty/null** → all 7 groups run sequentially in one invocation. The code comments call this
  "rarely used" because a full sequential run risks the Lambda's 15-minute timeout.
- **One of `lookups`, `laus`, `ces`, `qcew`, `oes`, `projections`, `cpi`** → only that group runs.
  This is the production pattern: separate EventBridge schedules invoke the same Lambda with
  different `datasetGroup` payloads so each group gets its own 15-minute window and groups run in
  effective parallel across invocations.

### Schedule (from `cloud-deployment/lambda.template`)

| Schedule | Cron (UTC) | ET | Group |
|---|---|---|---|
| `DailyFastIngestLaus` | `05 15 ? * MON-FRI` | 10:05am weekdays | `laus` |
| `DailyFastIngestCes` | `05 15 ? * MON-FRI` | 10:05am weekdays | `ces` |
| `WeeklyFullIngestLookups` | `0 7 ? * SUN` | ~2am Sunday | `lookups` |
| `WeeklyFullIngestQcew` | `05 15 ? * SUN` | 10:05am Sunday | `qcew` |
| `WeeklyFullIngestOes` | `15 15 ? * SUN` | 10:15am Sunday | `oes` |
| `WeeklyFullIngestProjections` | `25 15 ? * SUN` | 10:25am Sunday | `projections` |
| `WeeklyFullIngestCpi` | `35 15 ? * SUN` | 10:35am Sunday | `cpi` |

LAUS and CES run daily on weekdays, deliberately co-scheduled right after BLS's ~10:01am ET data
release. Everything else runs weekly on Sunday, staggered 10 minutes apart to avoid resource
contention. Lambda timeout is 900s (15 min), memory 2048MB, each schedule has a 10-15 min
`FlexibleTimeWindow` for jitter.

### Payload options (`IngestRequest`)

- **`DatasetGroup`** — routes to one group, or all groups if omitted (see above).
- **`ForceRefreshDatasets`** — comma-separated dataset keys (e.g. `"BLS-QCEW,BLS-FLAT-CE"`) or the
  literal `"ALL"`. Sets the `WID_FORCE_SOURCE_REFRESH_DATASETS` **process environment variable**
  at the start of the handler, which `SourceHashService` reads to force reprocessing even when a
  source's hash hasn't changed. **Known risk:** because this is a process-wide env var and Lambda
  containers are reused across warm invocations, there is no code path that clears it — a
  force-refresh request could leak into a later invocation on the same warm container that didn't
  ask for one. Worth fixing or at least confirming isn't currently biting anyone before NC leans on
  this feature.
- **`SkipHeavyDatasets`** — skips `qcew`, `oes`, `projections` (the slow ones) even on a run that
  would otherwise include them. Meant for ad hoc "just refresh LAUS/CES quickly" invocations.
- **`RunMigrationsOnly`** — applies+verifies DB migrations, then returns; no ingestion runs.
- **`VerifyOnly`** — checks migration/table state without applying anything, then returns; checked
  ahead of `RunMigrationsOnly` in the early-return condition.
- **`SkipMigrations`** — skips the migration-apply step before ingestion (only relevant when
  neither of the above is set).

### Failure isolation — one dataset failing doesn't cancel the run

Each group runs inside `RunSafeAsync`, which scopes a `CancellationTokenSource` to
`remainingLambdaTime - 30s` (30s floor — this budget shrinks across sequential groups when running
"all groups" in one invocation), catches `OperationCanceledException` as a timeout ("cancelled")
and any other `Exception` as a failure, and in both cases returns a result rather than throwing.
Either way `WriteTableLogsAsync` still records the outcome to `IngestLog`, and the handler moves on
to the next group. Only a Parameter Store read failure at the very start of the handler fails the
whole invocation. `Errors` (the per-invocation failure counter reported in the final summary log
line) is explicitly reset at the top of every invocation to guard against a stale count surviving
Lambda warm-container reuse.

### Per-table observability

Before/after each group, the handler snapshots row counts for the tables that group touches
(`CaptureTableCountsAsync`), and reads back the most recent `SourceHashService`
(`ingestsourcehash`/`ingestdetail`) rows for that table's known source-dataset keys
(`GetSourceDatasetsForTable`, e.g. `"laborforce" → ["BLS-FLAT-LN","BLS-FLAT-LA","BLS-API-LAUS","BLS-API-LAUS-STATE"]`)
to enrich the `IngestLog` write with source URL/hash/changed-flag/bytes-downloaded — this is the
bridge between `SourceHashService`'s low-level per-source hash cache and the human-readable
per-table freshness `GET /status` reports.

## Idempotency: `SourceHashService`

Every ingestor's change detection goes through the same primitive:
`ShouldProcessAsync(connectionString, dataset, sourceUrl, sourceHash, bytesDownloaded, ct)`.

1. Row-locks (`FOR UPDATE`) the existing `ingestsourcehash` row for `(dataset, sourceUrl)`.
2. `changed = existingHash != newHash` (or no row exists yet).
3. `forceRefresh` is checked against `WID_FORCE_SOURCE_REFRESH_DATASETS` (see above).
4. `shouldProcess = changed || forceRefresh`.
5. Upserts `ingestsourcehash` (hash + timestamps if changed, otherwise just touches
   `lastseenat`), and unconditionally appends an audit row to `ingestdetail`.
6. Returns `shouldProcess`.

Every ingestor below hashes at the granularity of **one URL/file per call** — a multi-file source
(OEWS's per-year-per-geography ZIPs, QCEW's per-quarter CSVs, CPI's per-data-file downloads) gets
independent change detection per file, so one changed file doesn't force reprocessing of the
others.

## The seven ingestors

### LAUS (`BlsLausIngestor.cs`) → `laborforce`

Three-tier fallback, each tier only engaged if the previous produced nothing:

1. **BLS flat files** (primary) — national: `ln/ln.data.1.AllData`, filtered to 4 hardcoded series
   (`LNS11000000`/`12`/`13`/`14000000` — labor force, employment, unemployment, unemployment rate).
   State: `la/la.data.2.AllStatesU` + `la/la.data.3.AllStatesS`, filtered to
   `LA[S|U]ST{fips}00000000000{measure}` for measures 03-06.
2. **BLS Public API, state series** — only if flat files produced no state-level rows. Builds
   `LA{U|S}ST{fips}00000000000{measure}` for all 51 state/DC FIPS × 2 adjustments × 4 measures
   (408 series), batched 50/request (BLS API limit) with a 200ms delay between batches.
3. **BLS Public API, national series** — only if the row set is still completely empty after both
   of the above.

20-year lookback (`currentYear - 20`). `BlsPeriodMapper` converts BLS period codes to WID
`(periodType, period)`; unparseable values are dropped via `BlsNumericParser`. State FIPS is
parsed out of the series ID itself; `adjusted` is derived from a character position in the series
ID. National rows are hardcoded `Adjusted = "1"` (LAUS national series are SA-only). No dedicated
`BlsLausIngestorTests.cs` exists — only its shared dependencies (`BlsFlatFileService`,
`BlsPeriodMapper`, `BlsNumericParser`) are unit tested.

### CES (`BlsCesIngestor.cs`) → `ces`

Same fallback shape as LAUS: national flat file (`ce/ce.data.0.AllCESSeries`, 20 hardcoded
supersector series) → state flat file (`sm/sm.data.55.TotalNonFarmStateWide.All`, filtered to
statewide-total rows only — the 13-char area/industry segment must be all zeros) → state API
fallback (510 series IDs, 51 states × 2 adjustments × 5 data types) → national API fallback (only
if the row dictionary is completely empty). Unlike LAUS, the state flat file is **always**
processed (not conditional on the national result). `dataTypeCode` maps to distinct columns
(`01`→EmpCes, `02`→HoursPerWeek, `03`→EarningsPerHour, `06`→EmpProductionWorkers,
`11`→EarningsPerWeek); national-series rows only ever populate `EmpCes` since the national series
list here has no hours/earnings breakdown. No dedicated ingestor-level test file.

### QCEW (`BlsQcewIngestor.cs`) → `industry`

Per-quarter CSV from `https://data.bls.gov/cew/data/api/{year}/{quarter}/industry/10.csv`
(industry `10` = all-industries total), last 5 years plus the current partial year. A 404/204 is
treated as "not yet published" (Info log, not an error) and the loop continues. Before ingesting,
loads `geographies` (area types `04`=county, `21`/`31`=MSA) into in-memory lookup dictionaries
once per invocation (memoized) to resolve QCEW's area codes (`US000`=national, `#####`
ending `000`=state, other 5-digit=county, `C####`=MSA looked up via the county/MSA maps — an
unresolvable MSA code is silently dropped). Filters to `sizeCode == "0"` (all-establishment-size,
excludes QCEW's size-class breakdowns). `AvgMonthlyEmp` is only computed if all three monthly
columns parse successfully.

**Known issue to verify**: the upsert's `ON CONFLICT` column list references `codetype` while the
`INSERT` column list uses `indcodetype` — this needs to be checked against the actual `industry`
table DDL (`cloud-deployment/migrations/`) before treating QCEW's upsert as fully trustworthy;
either the constraint name differs from what's expected or there's a genuine mismatch. Also unlike
LAUS/CES (single transaction across all batches), QCEW opens **a new transaction per 500-row
batch** — a partial-failure mid-run leaves earlier batches committed. No dedicated ingestor-level
test file.

### OEWS (`BlsOesIngestor.cs`) → `iowage`

Fetches `https://www.bls.gov/oes/special.requests/oesm{yy}{kind}.zip` for `kind` ∈
`{nat, st, msa}` (national, state, metro) across a 20-year window (`currentYear - 20` through
current year), all fetched **in parallel** — 63 concurrent tasks per run (21 years × 3
geographies). Each fetch is independently wrapped in `SafeFetchWorkbookAsync`, a try/catch that
logs and returns 0 on any failure rather than letting `Task.WhenAll` fail the whole ingestor — this
was added specifically because a single corrupted or not-yet-published ZIP (e.g. a stale
`oesm22st.zip`) used to crash the entire OEWS run. Each ZIP's single `.xlsx` entry is parsed with
ClosedXML: header row builds a column-name → index map, area code `"99"` maps to national
(`stFips="00"`), 2-digit numeric areas map to state FIPS directly. Occupation codes are normalized
to digits-only; suppressed values (`*` or `#`) become `null` with `SuppRecord="1"`.

**Known history**: as of the 2026-07-28 session, this ingestor was audited and fixed
(`SafeFetchWorkbookAsync` wrapper, verified 154,542 rows) specifically *because* it was crashing;
the same handoff explicitly flagged that LAUS/CES/QCEW had **not** yet received the equivalent
audit — see [known-issues-and-gaps.md](./known-issues-and-gaps.md) for whether that was ever done.
No dedicated `BlsOesIngestorTests.cs`.

### Projections (`BlsProjectionsIngestor.cs`) → `projectionsmatrix`

Pulls from ProjectionsCentral's REST JSON endpoints — **not** a BLS flat file or the BLS Public
API — `https://public.projectionscentral.org/Projections/{LongTerm,ShortTerm}RestJson`, paginated
1000 rows/page, each page independently hash-checked. This ingestor went through a full redesign
(v3→v4) after Utah discovered **the API does not support querying by individual SOC group** — only
the unfiltered/base query returns data, so the ingestor fetches all pages of the base endpoint and
lets the natural upsert key (`stFips:areaType:areaTypeVersion:area:projectionsPeriod:indCodeType:
indCode:occCodeType:occCode`) de-duplicate rather than trying to filter server-side. Both
industry and occupation codes are effectively fixed to `"000000"`/normalized-digits-only per
row — projections in this source aren't industry-cross-tabbed, only occupation. Each page fetch
has an independent 15-second timeout so one slow page doesn't stall the whole run.
After upserting projection rows, it **also** upserts occupation titles it collects along the way
into `occdirectories` (same table `WidCenterLookupIngestor` writes reference-only rows into with a
constant `"0000-0000"` period — worth checking for collisions/overwrites between the two writers
if occupation titles look inconsistent) and finally rebuilds `matrixxind`/`matrixxocc` (the
projection-matrix crosswalk tables) by `TRUNCATE`+`INSERT ... SELECT DISTINCT` from
`inddirectories`/`occdirectories` — meaning those two crosswalk tables are fully derived/rebuilt
on every Projections run, not incrementally upserted.

### CPI (`BlsCpiIngestor.cs`) → `cpi`, `cpiseries`, `cpiitems`, `cpiareas`, `cpiperiodicities`,
### `cpiseasonaladjustments`

The newest ingestor (added after the original 5-table core was working). Pulls BLS's `cu.*` flat
files from `download.bls.gov/pub/time.series/cu/` (base URL overridable via
`BLS_FLATFILE_BASE_URL` env var, same var the shared `BlsFlatFileService` respects). Loads
reference tables first — `cu.item` (price basket components), `cu.area` (geography), `cu.
periodicity`, `cu.seasonal`, `cu.series` (the series-definition table connecting area × item ×
seasonality × periodicity, fully replaced/upserted every run) — then streams two data files:
`cu.data.1.AllItems` (full history for the All-items series) and `cu.data.0.Current` (recent
values across all series), tab-split line-by-line rather than loaded fully into memory, logging
progress every 50,000 rows. Reference-table loads are unconditional every run (no hash check);
only the two large observation data files go through `SourceHashService`. Series definitions
(`cu.series`) are parsed by fixed column position (13 tab-delimited columns, documented inline)
rather than a header map, so a BLS column-order change would silently misparse rather than error —
worth a defensive header-validation pass if BLS revises this file's layout.

### WID Center Lookups (`WidCenterLookupIngestor.cs`) → `geographies`, `statefips`, `areatypes`,
### `cescodes`, `inddirectories`, `occdirectories`

The only ingestor that doesn't pull from BLS — it hits **data.widcenter.org** directly, because
per its own doc comment the goal was populating lookup endpoints from canonical WIDCenter content
"instead of sparse derivations." Five independent, individually hash-checked sub-loads:

| Sub-load | Source | Format |
|---|---|---|
| Geography bundle | `.../national/geog15.zip` | **Nested ZIP** — outer zip contains `geog15txt.zip`, which contains `geog.txt` + `stfipstb.txt` + optional `AREATYPE.txt` |
| Area types | `.../structure/lookup/areatype.txt` | delimited text |
| CES codes | `.../national/cescode.txt` | delimited text |
| Industry codes | `.../download/naics2022/indcodes2022.csv` | CSV |
| Occupation codes | `.../download/soc2018/occcodes.csv` | CSV |

The geography bundle is the most involved: it unwraps a zip-inside-a-zip in memory (no temp
files), picks the *longer* of two candidate title columns per row as a defensive workaround for
WIDCenter's inconsistent truncation, remaps WIDCenter's area codes into WID 3.0's `char(6)`
convention with a per-area-type switch (national→`"000000"`, state→FIPS padded, county→last-3-
digits-of-raw-code padded, everything else→raw code padded), and falls back from state-specific to
national (`"00"`) area-type titles when `AREATYPE.txt` doesn't have a state-specific entry.
Industry/occupation code loads hardcode `ProjPeriod = "0000-0000"` to mark themselves as
static reference data rather than period-specific projections (see the Projections cross-write
note above). A `NormalizeArea` helper exists in this file but is not called anywhere in the current
flow — likely dead code from an earlier area-code convention, safe to remove once confirmed unused
elsewhere. No dedicated ingestor-level test file.

## Shared services (`src/NationalWid.Ingestion/Services/`)

| Service | Role | Used by |
|---|---|---|
| `BlsFlatFileService.cs` | Downloads + parses BLS tab-delimited flat files; `TryLoadSeriesAsync` (load a fixed series list fully into memory) and `ProcessDataFileAsync` (streaming, predicate-filtered, for large all-states files) | LAUS, CES |
| `BlsNumericParser.cs` | `TryParseDecimal` — culture-invariant decimal parsing that rejects BLS's non-numeric placeholders | LAUS, CES (QCEW has its own separate parser — see below) |
| `BlsPeriodMapper.cs` | Maps BLS period codes (`M01-M13`, `Q1-Q5`, `A`) to WID `(periodType, period)` | LAUS, CES (QCEW hardcodes `periodType="02"` directly since it's inherently quarterly) |
| `IngestLogService.cs` | Writes one audit row per dataset run to `ingestlog`; failures are logged and swallowed, never propagate | `Function.cs`, called after every group run regardless of outcome |
| `SourceHashService.cs` | The idempotency backbone — see above | All seven ingestors, directly or via `BlsFlatFileService` |
| `ParameterStoreService.cs` | AWS SSM Parameter Store wrapper; required-DB-connection-string + optional-BLS-API-key lookups | `Function.cs` at startup |

**Two things worth fixing, not just knowing about:**

1. `BlsFlatFileService`'s outbound HTTP requests carry a hardcoded
   `User-Agent: "ULMITA/1.0 (ulmita.org; mattsteadman@utah.gov)"` header, plus browser-like
   `Accept`/`Accept-Language`/`Accept-Encoding` headers. BLS may rate-limit or block based on
   User-Agent identity — update this to identify North Carolina's project/contact before it either
   breaks or, worse, silently attributes NC's traffic to a Utah contact who's no longer involved.
2. `BlsNumericParser` (used by LAUS/CES) and QCEW's own inline `ParseNullableDecimal` use different
   `NumberStyles` (`Number` vs. `Any`) for what is conceptually the same "parse a BLS decimal or
   treat placeholder as null" operation — a minor inconsistency, not a bug per se, but worth
   consolidating if you touch either.

## Test coverage snapshot

Ingestor-**class**-level tests exist only for CPI (`BlsCpiIngestorTests.cs`). LAUS, CES, QCEW, OEWS,
Projections, and WID Center Lookups have **zero** dedicated test files — their correctness today
rests on the shared-dependency tests (`BlsFlatFileServiceTests.cs`, `BlsPeriodMapperTests.cs`,
`BlsNumericParserTests.cs`, `ParameterPathTests.cs`) plus whatever manual verification happened
during the sessions documented in [HANDOFF.md](../../HANDOFF.md). This is the single biggest gap
between "the ingestion pipeline works" and "the ingestion pipeline is verified to keep working" —
see [testing.md](./testing.md).
