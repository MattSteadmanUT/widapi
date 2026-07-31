# Decision: Non-Core Lookup and View Endpoints from Loaded National Data

Date: 2026-07-29

## Status
Accepted (implementation step in fed-national-wid; widapi remains source of truth)

## Context

The National WID API implementation currently has robust coverage of core data tables and a subset of lookup tables physically loaded in Aurora:

1. Geographies
2. PeriodYears
3. CESCodes
4. IndDirectories
5. OccDirectories

The canonical WID 3.0 contract in widapi includes additional non-core lookup and view endpoints. Some lookup title tables are not yet loaded as standalone physical tables in the current national database.

## Decision

Until all non-core lookup and view dependencies are physically ingested as first-class WID tables, fed-national-wid will implement these endpoints using a derived strategy:

1. Use loaded lookup/core tables to derive distinct code/value sets where possible.
2. Use canonical/static reference mappings where appropriate (for example, state FIPS names and common title mappings).
3. Preserve endpoint names and response field shapes aligned with widapi `draft.yaml`.
4. Expose camelCase route forms matching widapi paths; compatibility aliases may exist in implementation.
5. Keep core and non-core scope explicit in documentation and operational metadata.

## Implemented Scope (This Workstream)

### Non-core lookup endpoints

1. `/lookups/areaTypes`
2. `/lookups/stateFips`
3. `/lookups/periodTypes`
4. `/lookups/periods`
5. `/lookups/industryCodes`
6. `/lookups/occupationCodes`
7. `/lookups/ownerships`
8. `/lookups/wageSources`
9. `/lookups/wageRateTypes`
10. `/lookups/benchmarks`
11. `/lookups/growthCodes`

### Non-core view endpoints

1. `/views/cesWithGeography`
2. `/views/laborForceWithGeography`
3. `/views/industryWithGeography`
4. `/views/wagesWithDescriptions`
5. `/views/projectionsWithTitles`

### Explicit core/non-core clarity

1. Core vs non-core classifications are documented in implementation docs.
2. Runtime `/status` output includes `tableClass` when a dataset maps to a known WID table name.

## Consequences

1. Non-core lookup and view endpoints become available immediately for consumers and UI workflows.
2. Some optional descriptive fields may be null when source title tables are not yet loaded.
3. The long-term target remains full parity with physical WID non-core lookup tables and complete title/description coverage.
4. This approach reduces delivery risk while preserving contract continuity.

## Cross-Repo Impact

1. fed-national-wid adds non-core lookup endpoint implementations under `/lookups/*` and non-core view implementations under `/views/*`.
2. widapi remains the governing contract and records this temporary implementation strategy.
3. Future ingestion work should replace derived/static values with table-native values without breaking endpoint contracts.
