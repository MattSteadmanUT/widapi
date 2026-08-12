# Database Schema

Aurora PostgreSQL Serverless v2, schema built by 17 sequential SQL migrations in
`cloud-deployment/migrations/`. This doc is the table inventory and the migration history with
rationale — read it before touching schema, and especially before trusting the README's migration
table (see the staleness note below).

## Table inventory

### Core WID 3.0 data tables

| Table | Purpose | C# model |
|---|---|---|
| `laborforce` | LAUS labor force / unemployment estimates by area/period | `LaborForce` |
| `ces` | Current Employment Statistics (nonfarm payroll) estimates | `Ces` |
| `industry` | QCEW covered employment & wages by industry/ownership | `Industry` |
| `iowage` | OES/OEWS wages by industry × occupation | `IOWage` |
| `projectionsmatrix` | Industry/occupation employment projections | `ProjectionsMatrix` |
| `license` | Occupational license definitions/requirements | `License` |
| `licenseauthorities` | Licensing board/agency contact info | `LicenseAuthority` |
| `licensehistory` | Historical license counts by area/period | `LicenseHistory` |
| `licensexocc` | License-to-SOC-occupation crosswalk | `LicenseXOcc` |
| `cpi` | BLS CPI observation values (seriesid/year/period) | `Cpi` |

### Lookup / reference tables

| Table | Purpose | C# model |
|---|---|---|
| `geographies` | Geographic area descriptors (name, lat/long, precision) | `Geography` |
| `periodyears` | Valid data years/periods per state | `PeriodYear` |
| `cescodes` | CES series code → title/industry mapping | `CesCode` |
| `inddirectories` | Industry codes valid for a given projections period | `IndDirectory` |
| `occdirectories` | Occupation codes valid for a given projections period | `OccDirectory` |
| `matrixxind` | Projections-matrix industry code → detailed industry code crosswalk | `MatrixXInd` |
| `matrixxocc` | Projections-matrix occupation code → detailed occupation code crosswalk | `MatrixXOcc` |
| `areatypes` | Area type code → name | `AreaTypeReference` |
| `statefips` | State FIPS → name/abbreviation | `StateFipsReference` |
| `cpitems` | CPI item codes (e.g. `SA0` = All items) | `CpiItem` |
| `cpiareas` | CPI geographic area codes | `CpiArea` |
| `cpiperiodicities` | CPI periodicity codes (M/S/A) | `CpiPeriodicity` |
| `cpiseasonaladjustments` | CPI seasonal-adjustment codes (S/U) | `CpiSeasonalAdjustment` |
| `cpiseries` | BLS CPI series index (one row per time series) | `CpiSeries` |
| `apikeyratepolicies` | Named rate-limit profiles for API keys | *(none — read via raw config)* |

**Not actual DB tables**: `PeriodTypeLookup`, `OwnershipLookup`, `WageSourceLookup`,
`WageRateTypeLookup`, `BenchmarkLookup`, `GrowthCodeLookup` (all in `Models/NonCoreLookups.cs`)
are assembled by `LookupController` from a static in-code `LookupCatalog`
(`Services/LookupCatalog.cs`) combined with `DISTINCT` pulls from core tables — e.g. distinct
`ownership` values from `industry`. If NC needs to edit these lookup titles, edit the C# code, not
the database.

### "View" endpoints are runtime joins, not SQL views

No `CREATE VIEW` appears anywhere in the 17 migrations. `CesWithGeography`,
`LaborForceWithGeography`, `IndustryWithGeography`, `WageWithDescriptions`, `ProjectionWithTitles`,
and `LicensingByOccupation` (the `GET /views/*` endpoints, `Models/NonCoreViews.cs`) are DTOs the
API assembles via LINQ joins at query time against core + lookup tables. A BI tool connecting
directly to Postgres will not see these as queryable objects — only the API surfaces them.

### Operational tables

| Table | Purpose | C# model |
|---|---|---|
| `ingestlog` | One row per ingestion Lambda run (status, counts, timing) | `IngestLog` |
| `ingestdetail` | Per-source-file detail for an ingest run (FK → `ingestlog.id`) | *(none)* |
| `ingestsourcehash` | SHA-256 hash of last-seen source file per dataset — the idempotency table for `SourceHashService`, see [ingestion-pipeline.md](./ingestion-pipeline.md) | *(none)* |
| `apikeys` | User-managed API keys (SHA-256 hash only, plaintext never stored) | `ApiKey` |
| `migrations` | Tracks which migration filenames have been applied | *(none)* |

The `migrations` tracking table has **no DDL in any of the 17 files** — it's bootstrapped by the
deploy tooling (`dev-scripts/Run-Migrations.ps1`), not version-controlled SQL. Don't expect to find
its schema by reading the migrations folder.

## Migration history

Each entry notes whether it's **additive** (new capability, no prior mistake) or **corrective**
(fixing something an earlier migration got wrong) — the corrective ones are the ones worth
understanding deeply, since they explain why some things look inconsistent at first glance.

| # | File | Additive / Corrective | What it did |
|---|---|---|---|
| 001 | `001_create_schema.sql` | Additive | Initial schema: the 5 core data tables (composite PKs on `stfips, areatype, areatypeversion, area, ...`, `area char(6)`), plus `geographies`, `periodyears`, `cescodes`, `inddirectories`, `occdirectories`, `ingestlog`. |
| 002 | `002_create_indexes.sql` | Additive | ~15 non-unique indexes for common query patterns. |
| 003 | `003_seed_lookups.sql` | Additive (but see caveat below) | Seeds the national geography row and `periodyears` 2000–current+1. |
| 004 | `004_extend_wid30_schema.sql` | Additive | The licensing table family, projections crosswalks (`matrixxind`/`matrixxocc`), `ingestlog` telemetry columns, `ingestsourcehash`/`ingestdetail`. |
| 005 | `005_add_lookup_reference_tables.sql` | Additive | `areatypes`, `statefips`. |
| 006 | `006_ingestlog_activity_fields.sql` | Additive | More `ingestlog` telemetry (`recordschanged`, `recordsadded`, `totalrecords`, timestamps). |
| 007 | `007_fix_geography_area_codes.sql` | Corrective | Wipes non-national `geographies`/`statefips` rows, clears the `WIDCENTER-GEOG` hash to force re-ingestion. First sign of area-code churn — see caveat below. |
| 008 | `008_seed_state_periodyears.sql` | Additive | Backfills `periodyears` for all 56 states, so lookups work before ingestion completes. |
| 009 | `009_fix_national_area_codes.sql` | Corrective | Tried to standardize national-level area codes from 6-char `"000000"` to 7-char `"0000000"`. **This direction was wrong** — see 010. |
| 010 | `010_revert_to_char6_area.sql` | Corrective (reverts 009) | Explicit revert: "Converts area columns from char(7) (wrong) to char(6) (WID 3.0 spec)." Deletes 7-char-zero rows, re-seeds national geography at `'000000'`, `ALTER COLUMN area TYPE char(6)` across all core + `geographies`/`licenseauthorities`/`licensehistory`. |
| 011 | `011_cleanup_area_codes.sql` | Corrective (different bug than 009/010) | Fixes state FIPS **padding direction** — old convention right-padded (`"490000"` for Utah), WID 3.0 left-pads (`"000049"`). Deletes rows where `area` starts with a non-zero digit. |
| 012 | `012_schema_compliance.sql` | Additive + documents deviations | The big compliance pass — see below. |
| 013 | `013_add_api_keys.sql` | Additive | `apikeys`, `apikeyratepolicies` (unrelated to WID data). |
| 014 | `014_add_cpi_tables.sql` | Additive | The full CPI subsystem (6 tables). |
| 015 | `015_license_data.sql` | Additive (bulk data) | ~41k rows of license data from CareerOneStop + WID 2.8 per-state exports, all 57 states/territories. Flag columns defaulted to `'9'` (undetermined). |
| 016 | `016_license_flags_national.sql` | Corrective (data, not schema) | Re-upserts license flag columns with real per-state WID 2.8 values across all 56 states, replacing the `'9'` placeholders from 015. |
| 017 | `017_widen_cpi_baseyear.sql` | Corrective | `cpiseries.baseyear` was `varchar(8)`; real BLS base-period strings like `"1982-84=100"` are 11+ characters. Widened to `varchar(32)`. |

### Migration 012 in detail — the schema-compliance pass

This is the largest migration and the one to actually read if you're doing schema work. It widens
and renames columns across nearly every table to match a formal WID 3.0 structure document, and —
notably — its own header comment **explicitly documents 5 deliberate deviations from spec** rather
than silently leaving them unexplained:

1. `projectionsmatrix`'s primary key uses a single `projectionsperiod char(9)` (`"YYYY-YYYY"`)
   field instead of the spec's separate PeriodYear/PeriodType/Period/ProjectedYear/MatrixIndCode/
   MatrixOccCode fields — because the ingestor sources projections from ProjectionsCentral's
   base-projected range format, not a BLS period-coded series.
2. `inddirectories`/`occdirectories` use `(stfips, projperiod, indcodetype/occcodetype,
   indcode/occcode)` as their key instead of the spec's matrix-period-scoped shape — these tables
   serve as general code/title directories, not per-matrix-period tables.
3. `ces.seriescodetype` stores the literal string `"NAICS"` rather than the WID code `"10"` for
   roughly 35,000 existing CES rows — explicitly flagged in the migration comment as a **deferred,
   unresolved data cleanup task**, not a schema decision. Worth picking up early in NC's tenure if
   consumers rely on `seriesCodeType` being a WID code rather than a literal string.
4. `iowage.meanhourly`/`iowage.annualmean` are BLS-OEWS-only extension fields with no WID 3.0 spec
   equivalent.
5. `periodyears` carries extension fields (StFips, PeriodType, Period, PeriodTitle) beyond the
   spec's single PeriodYear field.

It also does the `geographies.areatitle` → `geographies.areaname` rename (see the migration-003
caveat below), renames `industry.codetype`→`indcodetype`, widens numerous `varchar`/`char` column
lengths, and — per its own comments — keeps some older extension columns
(`industry.avgmonthlyemp`, `industry.empcount`) **alongside** their spec-compliant replacements
(`quarteravgemp`, `establishments`) rather than dropping them, for backward compatibility. Treat
the newer spec-named columns as authoritative; the older ones are QCEW-extension leftovers still
present in the table.

## Things to verify or fix, not just know about

These are genuine oddities in the migration history, confirmed by reading the actual SQL (not just
inferred) — flagging them here so NC doesn't have to rediscover them from a production incident.

1. **Migration 003 references a column that doesn't exist yet.** `001_create_schema.sql` defines
   `geographies.areatitle`. `003_seed_lookups.sql`'s seed `INSERT` targets
   `geographies (..., areaname, ...)` — a column that isn't created until migration 012 renames
   `areatitle` → `areaname`, nine migrations later. Replayed strictly in order against a fresh
   database, migration 003 as currently written should fail. **Before relying on
   `Run-Migrations.ps1` to bootstrap a brand-new environment from scratch, do a clean-database dry
   run and confirm 003 actually applies** — it's possible this was hand-patched in Utah's actual
   dev/prod databases and the file in source control doesn't reflect what really ran.
2. **Migration 007 compares `area` (defined as `char(6)`) against a 7-character literal.**
   `DELETE FROM geographies WHERE NOT (stfips='00' AND areatype='00' AND area='0000000')` — the
   literal is 7 zeros against a 6-char column. Postgres's `bpchar` comparison semantics may have
   made this a no-op, a truncated match, or something else entirely depending on how it was
   evaluated at the time. Worth a sanity check of current `geographies` row content rather than
   assuming it did what its author intended.
3. **The area-code column went through three separate fixes in a row (009 → 010 → 011)** for two
   *different* bugs: 009/010 argued about column *length* (7 chars vs. the correct 6), and 011
   separately fixed *padding direction* (state FIPS was being right-padded instead of left-padded).
   Both are believed resolved as of 011, but there's no automated test asserting `area` values
   match the expected `^0*\d+$` shape going forward — a regression here would be silent. Consider
   adding one.
4. **Migration 010 runs its cleanup/re-seed/ALTER-COLUMN sequence twice in the same file**, not as
   an exact duplicate but as two passes with slightly different table coverage (the second pass
   additionally touches `license` and explicitly skips `matrixxind` with an inline "no area column"
   comment). Every statement involved is idempotent, so it's harmless as-is, but it reads as a
   copy-paste artifact worth consolidating if the migration history is ever squashed.
5. **Two column names carry preserved typos**, both explicitly called out in `WIDDbContext.cs`
   comments so nobody "fixes" them and breaks existing data:
   - `laborforce.emppoproatio` (extra "r" — should be `emppopratio`)
   - `iowage.userdefinedranagemean` ("ranage" instead of "range")
   Any raw SQL, BI tooling, or reporting NC builds directly against Postgres needs to use these
   misspelled names.
6. **The README's migration table is stale** — as of this handoff it lists "Current migrations (014
   total)" and stops at `014_add_cpi_tables.sql`, omitting 015, 016, and 017 entirely. This
   document supersedes it; if the README isn't updated in the same change as a future migration,
   don't trust it as the source of truth — check `cloud-deployment/migrations/` directly.
7. **`license.areatype` is added by both migration 012 and migration 015** (both
   `ADD COLUMN IF NOT EXISTS`, so harmless) — a sign that the license bulk-data migrations (015,
   generated by `dev-scripts/generate-license-migration.py`) were authored somewhat independently
   of the main schema-compliance line. Not a bug, just a provenance note.
