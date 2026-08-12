# Decision: Filter and Sort Query Behavior

Date: 2026-07-29

## Status
Accepted (working draft behavior for National WID API implementation alignment)

## Context

State users requested richer query behavior for data extraction workflows:

1. Multiple values in one filter parameter.
2. Wildcard filtering on classification code fields.
3. Explicit user-controlled sorting by one or more fields.

The implementation in fed-national-wid now supports these behaviors and widapi is the contract source of truth, so the spec must document them.

## Decision

1. String filter parameters may accept comma-separated values as an OR-set.
   - Example: stFips=49,53
2. Code-like filter parameters may accept wildcard * at the beginning and/or end.
   - Prefix: occCode=11*
   - Suffix: occCode=*11
   - Contains: occCode=*11*
3. Sorting remains the standardized sort query parameter behavior:
   - Comma-separated fields
   - Optional :asc or :desc per field
   - Unknown fields return HTTP 400

## Consequences

1. Callers can express subset extraction in a single request without issuing many API calls.
2. API implementations must validate sort fields and reject unknown fields.
3. API implementations should document wildcard support only on approved code-like fields to avoid expensive broad scans.
4. Test clients and consumer examples should demonstrate comma-list filters and sort expressions.

## Cross-Repo Impact

1. fed-national-wid controllers now parse comma-list filters and wildcard code filters and apply dynamic sorting.
2. widapi draft spec text has been updated to reflect this contract behavior.
3. draft.json is regenerated from draft.yaml to keep rendered Swagger documentation in sync.
