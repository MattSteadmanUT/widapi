# National WID Stakeholder Feedback Plan (2026-07-29)

This document captures post-demo feedback on the 5 core WID tables and maps each item to concrete work in the current National WID codebase.

## Scope

- Workspace: `fed-national-wid`
- Related spec source: `widapi/specs/wid-3.0/draft.yaml`
- Current API implementation: `src/NationalWid.Api`

## Current Baseline (as of 2026-07-29)

1. API currently exposes the 5 core data endpoints plus selected lookup endpoints.
2. API uses Cognito JWT auth behind API Gateway HTTP API.
3. Filters are single-value equality filters per query parameter.
4. Controllers apply fixed default ordering; user-provided `sort` is not currently implemented.
5. Schema/migrations currently include core tables + a small lookup subset, not the wider WID 3.0 optional table set.

## Feedback Items and Implementation Decisions

## 1) User-manageable API keys (with throttling, disable-on-user-disable, expiration)

### Feedback
Users want API keys for use from other systems, with dynamic throttling, expiration, and automatic disablement when the Cognito account is disabled.

### Current state
- No API key issuance/management exists in the API.
- Current gateway type is HTTP API (`AWS::Serverless::HttpApi`).

### Decision
Implement API keys as a first-class feature with key lifecycle and policy-driven throttling.

### Design notes
1. Add key management endpoints (authenticated with Cognito JWT):
   - `POST /api-keys` create key
   - `GET /api-keys` list current user keys
   - `PATCH /api-keys/{keyId}` rotate/disable/extend
   - `DELETE /api-keys/{keyId}` revoke
2. Store only key hashes (never plaintext keys after creation).
3. Add `expiresAt`, `status`, `rateProfile`, `lastUsedAt`, `revokedAt`.
4. Auto-disable keys when Cognito user is disabled:
   - Event-driven sync (Cognito admin disable/enable events via EventBridge), and
   - Defensive runtime check/cached status verification path.
5. Dynamic throttling:
   - Keep policy definitions in config or data table (per key/profile), not hardcoded.

### AWS architecture constraint to resolve
API Gateway HTTP API does not provide usage plans in the same way as REST API usage plans/api keys. Choose one of:
1. Migrate public data routes to REST API and use usage plans + API keys.
2. Keep HTTP API and implement app-level/perimeter throttling (for example app-level token bucket and/or AWS WAF rate-based controls).

### Acceptance criteria
1. Key can be created, used, rotated, revoked.
2. Expired key returns 401/403.
3. Disabled Cognito user cannot call API with previously-issued key.
4. Rate limits can be changed without redeploying code.

## 2) Multi-value filters and wildcard matching (comma lists, prefix/suffix wildcard)

### Feedback
Users want subset filtering via comma-separated values and wildcard patterns such as `11*` and `*11`.

### Current state
- API filters are single-value equality checks.
- UI currently renders one text input per parameter and does not guide multi-value or wildcard usage.
- WID draft spec currently defines most filter params as single `string` values; wildcard semantics are not formally defined.

### Decision
Add backward-compatible filter enhancements while preserving exact-match default behavior.

### Proposed filter semantics (v1)
1. Comma-separated IN list:
   - `occCode=11-1011,11-1021,11-1031`
2. Wildcards using `*`:
   - `11*` => starts-with
   - `*11` => ends-with
   - `*11*` => contains
3. Apply wildcard behavior only on approved code/title fields to avoid expensive scans.
4. Keep default exact match when no comma and no wildcard are present.

### Implementation approach
1. Add a shared query filter parser service used by all controllers.
2. Translate parsed filters into EF expressions (`==`, `IN`, `LIKE`).
3. Validate unsupported wildcard usage and return HTTP 400 with clear error text.
4. Extend UI to support:
   - multi-value entry helper text,
   - wildcard hint text,
   - example query preview.

### Acceptance criteria
1. Comma lists return union subset in one call.
2. Wildcard prefix/suffix/contains behavior works for approved fields.
3. Invalid wildcard patterns return informative HTTP 400.

## 3) User-selectable sorting by fields

### Feedback
UI should allow sorting by fields. Question was raised whether this is in spec.

### Spec status
Yes. WID draft spec already defines `sort` as:
- comma-separated fields,
- optional `:asc` / `:desc` direction,
- unknown fields should return HTTP 400.

### Current implementation gap
Controllers currently apply fixed ordering and do not parse a `sort` query parameter.

### Decision
Implement spec-compliant `sort` handling across all list endpoints and surface it in UI.

### Implementation approach
1. Add shared sort parser/validator.
2. Maintain endpoint-specific allowlist of sortable fields.
3. Apply dynamic ordering before pagination/export.
4. Return HTTP 400 for unknown sort field/direction.
5. Add UI sort builder with one or more sort keys and directions.

### Acceptance criteria
1. API accepts `sort=periodYear:desc,period:asc` style queries.
2. Unknown sort field yields HTTP 400.
3. UI can compose multi-field sort and sends valid query string.

## 4) Add non-core (non-standard) tables available from WID Center flat files

### Feedback
Utility of only core tables was questioned; users want additional WID tables available from data.widcenter.org flat files.

### Current state
- Project currently focuses on core 5 + key lookups.
- Broader WID 3.0 table family is not yet implemented.

### Decision
Create a phased table-expansion roadmap with clear prioritization and source mapping.

### Proposed rollout framework
1. Build a table catalog matrix with columns:
   - table name,
   - endpoint path,
   - source file/url,
   - refresh cadence,
   - data owner,
   - public vs restricted classification.
2. Implement in priority waves (Wave 1, Wave 2, ...), each with:
   - migration SQL,
   - model + DbSet + controller,
   - ingestion loader,
   - API tests,
   - documentation updates.

### Candidate first-wave additions
1. CPI family (see item 5)
2. Population / demographics related high-demand reference tables
3. Income / transfer / UI claims high-use labor-market adjunct tables

## 5) Add CPI specifically

### Feedback
CPI was explicitly requested.

### Current state
- WID 3.0 structure includes CPI-related tables (CPI, CPIPlus, CPIItems, CPISources, CPITypes).
- Current National WID API does not yet expose CPI endpoints.

### Decision
Prioritize CPI as the first non-core expansion.

### Proposed endpoint set
1. `GET /cpi`
2. `GET /cpi-plus`
3. `GET /lookups/cpi-items`
4. `GET /lookups/cpi-sources`
5. `GET /lookups/cpi-types`

### Data/ingestion notes
1. Respect WID `areaType` / `areaTypeVersion` conventions for CPI geographies.
2. Support same pagination/download conventions as existing endpoints.
3. Add scheduled ingestion for CPI source files with idempotent upsert and ingest logging.

### Acceptance criteria
1. CPI endpoints respond with standard envelope and filters.
2. CPI lookup tables are queryable.
3. CPI ingestion is automated and status-visible.

## 6) Keep table structures aligned with WID documentation and display in same structure

### Feedback
Responses should always follow WID documented structure.

### Current state
- API generally aligns with WID table intent, but conformance is not currently enforced by automated contract checks across all fields/tables.

### Decision
Define and enforce a strict structure conformance policy.

### Conformance policy
1. For each exposed table, maintain a field contract:
   - field name,
   - type,
   - nullability,
   - semantic definition,
   - order for flat-file exports.
2. Add contract tests that compare API DTO contracts against documented WID structure.
3. Ensure CSV/XLSX export column order matches WID field order.
4. For JSON, preserve field names exactly as standardized by API contract and keep consistency across endpoints and releases.

### Acceptance criteria
1. Contract tests fail when a field drifts from agreed WID contract.
2. CSV/XLSX exports mirror WID column structure.
3. Release notes explicitly call out any intentional schema deltas.

## Priority and Delivery Plan

## P0 (next sprint)
1. Implement API `sort` support (spec-aligned).
2. Implement multi-value comma filters.
3. Implement wildcard filters on approved code fields.
4. Add UI controls/hints for sort + multi-value/wildcard.

## P1
1. API key lifecycle MVP (create/list/revoke/expiry).
2. Resolve gateway-level throttling architecture decision (REST API migration vs app-level throttling on HTTP API).
3. Add disable-on-Cognito-disable synchronization.

## P2
1. CPI schema + endpoints + ingestion.
2. CPI lookup tables.

## P3
1. Broader non-core table expansion waves.
2. Full conformance automation for all exposed tables.

## Open Decisions Requiring Product/Architecture Sign-off

1. API key throttling implementation path:
   - REST API usage plans, or
   - HTTP API retained with application/perimeter throttling.
2. Exact wildcard policy scope (which fields permit wildcard and which do not).
3. Which non-core tables are in Wave 1 after CPI.

## Suggested Work Items (Backlog Seed)

1. Add shared `SortParser` service + endpoint field allowlists.
2. Add shared `FilterParser` supporting exact/IN/wildcard modes.
3. Add UI query-builder enhancements for multi-value + sort.
4. Add CPI migrations/models/controllers and CPI ingest job.
5. Add API key domain model, management endpoints, and auth path.
6. Add conformance test suite for WID structure compliance.
