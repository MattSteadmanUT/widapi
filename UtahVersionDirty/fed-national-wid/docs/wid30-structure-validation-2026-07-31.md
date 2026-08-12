# WID 3.0 Structure Validation (2026-07-31)

## Scope

This validation focuses on the tables and endpoints currently implemented in `fed-national-wid` and compares them against the canonical contract and structure references in `widapi`:

1. `widapi/specs/wid-3.0/draft.yaml`
2. `widapi/specs/wid-3.0/WID-3.0-Structure-20251120.md`

It does not claim full validation of every optional WID 3.0 table family (for example CPI and other future non-core expansions).

## Verified Implementation Surface

### Core tables

1. `LaborForce`
2. `CES`
3. `Industry`
4. `IOWage`
5. `ProjectionsMatrix`
6. `License`

### Lookup and non-core tables currently implemented

1. `Geographies`
2. `PeriodYears`
3. `CESCodes`
4. `IndDirectories`
5. `OccDirectories`
6. `MatrixXInd`
7. `MatrixXOcc`
8. `LicenseAuthorities`
9. `LicenseHistory`
10. `LicenseXOcc`

### View-style endpoints currently implemented

1. `CesWithGeography`
2. `LaborForceWithGeography`
3. `IndustryWithGeography`
4. `WageWithDescriptions`
5. `ProjectionWithTitles`
6. `LicensingByOccupation`

## Schema Parity Findings

## 1) Table presence and migration coverage

1. Migration verification now checks all required implemented tables, including licensing and matrix crosswalks.
2. Migration `004_extend_wid30_schema.sql` adds:
   - `matrixxind`
   - `matrixxocc`
   - `licenseauthorities`
   - `license`
   - `licensehistory`
   - `licensexocc`
   - ETL provenance tables (`ingestsourcehash`, `ingestdetail`)

## 2) IOWage structure alignment

1. `IOWage` now includes `wageSource` and `rateType` in the API model and filter surface.
2. DB migration adds `wagesource` and `ratetype` columns to `iowage`.
3. OEWS ingestion upsert now populates both fields.

## 3) Projections crosswalk alignment

1. `MatrixXInd` and `MatrixXOcc` endpoints are implemented and mapped.
2. `projectionsWithTitles` endpoint accepts matrix alias filters and projected-year semantics.

## 4) Licensing family alignment

1. Licensing endpoints are implemented for authorities, licenses, history, and occupation crosswalks.
2. `licensingByOccupation` view endpoint is implemented.

## ETL Provenance and Hash-Change Detection Findings

1. Source URL + SHA-256 tracking is implemented in `ingestsourcehash` and `ingestdetail`.
2. Hash-based skip logic is wired for:
   - BLS flat-file sources (LAUS/CES)
   - BLS API fallbacks (LAUS/CES)
   - QCEW source CSVs
   - OEWS ZIP source
   - Projections REST pages
3. Projections pagination behavior was adjusted so unchanged pages do not truncate traversal of later pages.

## Known Remaining Gaps

1. Full optional WID table families beyond the currently implemented scope are not yet migrated or exposed.
2. No automated table-by-table validator currently compares every column definition in Aurora against every field in the full WID 3.0 structure document.
3. If strict full-structure compliance is required (including non-implemented optional families), add a dedicated contract-check tool/report that diffs:
   - `information_schema.columns`
   - canonical WID 3.0 structure catalog

## Conclusion

Within the implemented API surface, schema and endpoint coverage is now aligned with current `widapi` contract intent for core, lookup, matrix, and licensing families, with ETL provenance and hash-change detection integrated across ingestion sources.
