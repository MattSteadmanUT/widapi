# API Contract Parity Closure (2026-07-31)

## Objective

Close remaining National WID API contract gaps between implementation (`fed-national-wid`) and canonical contract (`widapi/specs/wid-3.0/draft.yaml`).

## Result

No remaining endpoint-level gaps were found in the current implemented scope.

## What was validated

1. Spec path inventory (`draft.yaml`) includes 41 paths.
2. Implementation route inventory (`src/NationalWid.Api/Controllers`) covers:
   - all lookup paths, including:
     - `geographies/{stFips}/{areaType}/{areaTypeVersion}/{area}`
     - wage source/rate type lookups
     - directory lookups
   - all core table paths with metadata endpoints:
     - `ces`, `laborForce`, `industry`, `projections`, `wages`
   - all licensing and crosswalk paths:
     - `licensing/authorities`
     - `licensing/licenses`
     - `licensing/history`
     - `licensing/occupationCrosswalks`
   - non-core view paths:
     - `views/cesWithGeography`
     - `views/laborForceWithGeography`
     - `views/wagesWithDescriptions`
     - `views/projectionsWithTitles`
     - `views/industryWithGeography`
     - `views/licensingByOccupation`
   - health/status paths.

## Additional parity fixes completed in this pass

1. Added `wageSource` and `rateType` to `IOWage` schema/model/filter surface.
2. Added projections alias handling for `projectedYear`, `matrixIndCode`, `matrixOccCode` in view/controller filtering.
3. Ensured matrix crosswalk endpoints and licensing family remain listed as implemented in table catalog metadata.

## Notes

1. Implementation includes compatibility aliases (kebab-case + camelCase on several routes). Contract authority remains `widapi` path naming.
2. Optional WID table families not yet implemented are tracked separately and are outside this parity-closure scope.
