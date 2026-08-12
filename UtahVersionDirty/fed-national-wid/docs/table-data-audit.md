# National WID Table Data Audit

**Purpose:** Documents every table in the WID 3.0 schema — its data source, current ingestion status, why data may be missing, and the recommended action before handoff.

**Last updated:** 2026-08-04  
**Status legend:** ✅ Has data | ⚠️ Schema only / empty | 🔴 Not yet implemented  

---

## Core Tables

| Table | Status | Source | Notes / Recommended Action |
|---|---|---|---|
| **LaborForce** | ✅ | BLS LAUS flat files (`la.data.*`) | Monthly ingestion. State + national. Verified populated. |
| **CES** | ✅ | BLS CES flat files (`ce.data.*`, `sm.data.55`) | Monthly ingestion. National + state total nonfarm. |
| **Industry** | ✅ | BLS QCEW CSV zip files | Monthly ingestion. All-industry by county + state. Heavy dataset. |
| **IOWage** | ✅ | BLS OEWS Excel workbooks | Annual ingestion (May release). State + national OES occupational wages. |
| **ProjectionsMatrix** | ✅ | BLS Employment Projections flat files | National 10-year projections. Re-runs when new edition is released. |
| **License** | ✅ | WID Center `COSFlatExport` + per-state WID 2.8 license MDB exports | National coverage is populated for 11,752 licenses. Per-state flag detail is loaded for 9,937 records; remaining national/undetermined rows retain WID `9` codes. The API now exposes `licenseUpdatedDate` parsed from the source stamp when available. |
| **LicenseAuthorities** | ✅ | Same as License | Populated from `COSFlatExport` authority contact rows. |
| **LicenseHistory** | ⚠️ | Same as License | Table is implemented, but every currently available per-state `lichist` table in the WID Center exports is empty (0 rows across all checked states). |
| **LicenseXOcc** | ✅ | Same as License | Populated from `COSFlatExport` occupation crosswalk rows. |

### Why LicenseHistory Is Still Empty

The WID 3.0 spec defines `LicenseHistory` as a historical series companion to the current licensing tables. We now populate the current licensing tables from WID Center exports, but the historical table remains empty because:
- The WID Center's currently published per-state Access exports expose a `lichist` table with **0 rows** in every checked state file.
- `COSFlatExport` does not contain license history observations.
- No alternate national machine-readable source for WID-compliant license history was identified.

**Action for handoff:** Keep `LicenseHistory` documented as source-constrained, and update it if/when states begin publishing populated `lichist` records through the WID Center feed.

---

## Non-Core Lookup Tables

| Table | Status | Source | Notes |
|---|---|---|---|
| **Geographies** | ✅ | WID Center lookup download | Area code → name mappings for each state. |
| **AreaTypes** | ✅ | WID Center lookup download | |
| **StateFips** | ✅ | WID Center lookup download + in-memory fallback | Falls back to hardcoded list if DB is empty — always returns data. |
| **PeriodTypes** | ✅ | Derived from `periodyears` table | Distinct period types present in ingested data. |
| **PeriodYears** | ✅ | Derived from LAUS/CES ingestion | |
| **Periods** | ✅ | Derived from `periodyears` | |
| **IndustryCodes** | ✅ | Derived from Industry + IndDirectories | Cross-source union with titles from IndDirectories. |
| **OccupationCodes** | ✅ | Derived from IOWage + OccDirectories | |
| **CESCodes** | ✅ | WID Center lookup download | CES series code → title mapping. |
| **Ownerships** | ✅ | Derived from Industry | Distinct ownership codes (NAICS). |
| **WageSources** | ✅ | Derived from IOWage | |
| **WageRateTypes** | ✅ | Derived from IOWage | |
| **Benchmark** | ✅ | Derived from PeriodYears | |
| **GrowthCodes** | ✅ | Hardcoded in `LookupCatalog` | 4 standard WID growth band codes (AA/BB/CC/DD). Static — will not change. |
| **IndDirectories** | ✅ | BLS Projections flat files | Industry titles used for code enrichment. |
| **OccDirectories** | ✅ | BLS Projections flat files | Occupation titles. |
| **MatrixXInd** | ✅ | Materialized from `IndDirectories` | Physical table is now refreshed during projections ingestion so status and exports match the derived API behavior. |
| **MatrixXOcc** | ✅ | Materialized from `OccDirectories` | Physical table is now refreshed during projections ingestion so status and exports match the derived API behavior. |

---

## CPI Tables (New — Added in Migration 014)

| Table | Status | Source | Notes |
|---|---|---|---|
| **CPI** | ✅ | BLS CPI-U flat files (`cu.data.*`) | Populated in dev: 95,427 observations after fixing the CPI flat-file path and series parsing. |
| **CpiSeries** | ✅ | BLS `cu.series` flat file | Populated in dev: 8,104 series descriptors. |
| **CpiItems** | ✅ | BLS `cu.item` flat file | Populated in dev: 400 item codes. |
| **CpiAreas** | ✅ | BLS `cu.area` flat file | Populated in dev: 58 area codes. |

**Why CPI was not in the original schema:**  
CPI is not a BLS WID 3.0 core table (WID focuses on labor market data — employment, wages, projections). CPI was explicitly added based on stakeholder feedback (July 2026) as a high-value supplement to wage data for computing real purchasing power.

---

## Non-Core View Tables

These are SQL-join views served by the API — they do not have independent storage. They always have data as long as the underlying core tables do.

| View | Status | Underlying Tables | Notes |
|---|---|---|---|
| **CesWithGeography** | ✅ | CES + Geographies | Adds `areaName` and `seriesTitle` to CES rows. |
| **LaborForceWithGeography** | ✅ | LaborForce + Geographies | Adds `areaName`. |
| **IndustryWithGeography** | ✅ | Industry + Geographies | Adds `areaName`. |
| **WageWithDescriptions** | ✅ | IOWage + IndDirectories + OccDirectories | Adds industry and occupation titles to wage rows. |
| **ProjectionWithTitles** | ✅ | ProjectionsMatrix + IndDirectories + OccDirectories | Adds titles and growth code label. |
| **LicensingByOccupation** | ✅ | LicenseXOcc | Now populated because `LicenseXOcc` is populated. |

---

## Summary

| Category | Total Tables | Has Data | Empty / Schema-only |
|---|---|---|---|
| Core | 9 | 8 | 1 (`LicenseHistory`) |
| Non-Core Lookups | 14 | 14 | 0 |
| CPI (new) | 4 | 4 | 0 |
| Views | 6 | 6 | 0 |
| **Total** | **33** | **32** | **1** |

---

## Remaining Gap: Recommended Short-Term Actions

1. **Document the `LicenseHistory` constraint clearly:** Note that the schema and endpoint are complete, but the current upstream WID Center exports publish zero historical rows.
2. **Document submission process:** Add a `LICENSING-DATA-SUBMISSION.md` describing how states can submit richer licensing data to the WID Center, including history if available.
3. **Re-check future WID Center exports periodically:** If populated `lichist` tables begin appearing, the existing extraction workflow can be extended to ingest them quickly.
