# National WID Table Classification

This document makes explicit which endpoints represent **core** WID 3.0 tables and which represent **non-core** lookups/views.

## Core Tables

These are the primary WID 3.0 tables exposed with table-native endpoints:

1. `LaborForce` -> `/labor-force`
2. `CES` -> `/ces`
3. `Industry` -> `/industry`
4. `IOWage` -> `/wages`
5. `ProjectionsMatrix` -> `/projections`
6. `License` -> `/licensing/licenses`

## Non-Core Lookup Tables

These are reference and discovery endpoints under `/lookups`:

1. `/lookups/geographies`
2. `/lookups/areaTypes`
3. `/lookups/stateFips`
4. `/lookups/periodTypes`
5. `/lookups/periodYears`
6. `/lookups/periods`
7. `/lookups/industryCodes`
8. `/lookups/occupationCodes`
9. `/lookups/cesCodes`
10. `/lookups/ownerships`
11. `/lookups/wageSources`
12. `/lookups/wageRateTypes`
13. `/lookups/benchmarks`
14. `/lookups/growthCodes`
15. `/lookups/ind-directories` (compatibility)
16. `/lookups/occ-directories` (compatibility)

## Non-Core View Tables

These are joined, display-oriented endpoints under `/views`:

1. `/views/cesWithGeography`
2. `/views/laborForceWithGeography`
3. `/views/industryWithGeography`
4. `/views/wagesWithDescriptions`
5. `/views/projectionsWithTitles`
6. `/views/licensingByOccupation`

## Notes

1. Core endpoints are table-native and should be the default source for data pipelines.
2. Non-core lookup/view endpoints are intended to reduce client-side joins and improve discoverability.
3. Where lookup title tables are not yet physically loaded in Aurora, non-core endpoints may return derived/static titles or null optional title fields.
4. `/status` now includes `tableClass` values when dataset names map to known WID tables.
