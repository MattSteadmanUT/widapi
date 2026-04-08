# WID 3.0 API Design Decisions

**Date:** 2026-04-07  
**States involved:** Working Group / Matt-Copilot branch  
**Context:** Initial draft of the WID 3.0 national API spec, informed by the Oregon WID 2.8
reference spec and the official WID 3.0 structure documentation.

---

## Decision 1: Table-native endpoints as the primary resource model

**Decision:** Every WID 3.0 core table is exposed as its own endpoint (e.g., `/ces`, `/laborForce`,
`/industry`). View endpoints (`/views/*`) are layered on top for common join patterns.

**Rationale:** The problem statement explicitly requests that "Core tables should essentially all
be queryable in their native format." Keeping each table as a first-class resource also makes the
API predictable — a developer who knows the WID 3.0 structure document can immediately identify
the corresponding endpoint.

---

## Decision 2: Eliminate per-domain `listAreas`, `maxPeriod`, `minPeriod` sub-endpoints

**Decision:** The Oregon spec exposes `listAreas`, `listYears`, `maxPeriod`, and `minPeriod`
as separate sub-paths under each domain (e.g., `/ces/employment/listAreas`,
`/ces/employment/maxPeriod`). The WID 3.0 spec removes all of these.

**Options considered:**
- Keep the Oregon sub-endpoint pattern
- Replace with a shared `/lookups/geographies` and `/lookups/periods` endpoint

**Rationale:** The Geographies table is shared across all data domains — duplicating it under
every domain creates an explosion of redundant endpoints and inconsistent behaviour (the data
returned by `/ces/employment/listAreas` vs `/industry/listAreas` should be identical).
Callers who need the max or min period for a dataset can sort the `/lookups/periodYears`
response. Standard `minYear`/`maxYear` query parameters on data endpoints handle range filtering.

---

## Decision 3: `AreaTypeVersion` included in all geography keys

**Decision:** The `areaTypeVersion` parameter is exposed on every endpoint that filters by
geography.

**Context:** WID 3.0 adds `AreaTypeVersion` as part of the Geographies primary key to allow
historical vintage definitions of area types (particularly MSAs) to coexist with current
definitions. The Oregon WID 2.8 spec predates this field entirely.

**Rationale:** Omitting it would make it impossible to query data stored under non-default
vintage definitions. The default value is `'0'` (current definition), so existing callers are
unaffected when they omit the parameter.

---

## Decision 4: CES is a single endpoint, not split by measure type

**Decision:** `/ces` returns the full WID 3.0 `CES` table, which includes employment,
production-worker employment, and hours/earnings in a single record. The Oregon spec used
separate endpoints for employment vs. hours/earnings.

**Context:** In the WID 3.0 structure document the `CES` table is a single table with fields
`EmpCES`, `EmpProductionWorkers`, `HoursPerWeek`, `EarningsPerWeek`, etc. all on the same row.
Oregon's choice to split these was an application-layer decision, not a WID 2.8 structural one.

**Rationale:** Reflecting the native table structure keeps the API honest. Suppress flags
(`suppRecord`, `suppHoursEarnings`, `suppProdWorkers`) tell callers which sub-measures may be
displayed — these flags are on the same row, so separating the endpoint would require clients
to make two requests for data that lives in one table row.

---

## Decision 5: `SeriesCodeType` exposed alongside `SeriesCode` on CES

**Decision:** Both `seriesCodeType` and `seriesCode` are filter parameters on `/ces` and
`/lookups/cesCodes`.

**Context:** The WID 3.0 `CES` table's primary key includes `SeriesCodeType`. The Oregon spec
only exposed `seriescode`, implicitly assuming a single code type.

**Rationale:** Supporting multiple industry classification systems for CES series (e.g., a state
that indexes series under both NAICS 2017 and NAICS 2022) requires both fields.

---

## Decision 6: Suppress fields are strings ('0'/'1'), not booleans

**Decision:** All suppress/flag fields (e.g., `suppRecord`, `prelim`, `adjusted`) are modelled
as `string` with `enum: ['0', '1']`, not as `boolean`.

**Context:** The WID 3.0 structure document defines these as `char(1)` fields.

**Rationale:** Using string types avoids ambiguity between JSON `false`/`null`/`"0"` when
serialising char fields from legacy database drivers. Consumers that prefer booleans can map
`'1'` → `true` in their own layer.

---

## Decision 7: Projections use `IndDirectories` / `OccDirectories`, not `MatrixXInd` / `MatrixXOcc`

**Decision:** The code enumeration endpoints for projections are `/projections/indDirectories`
and `/projections/occDirectories`, which map directly to the `IndDirectories` and `OccDirectories`
WID 3.0 lookup tables. The `MatrixXInd` / `MatrixXOcc` crosswalk tables are also exposed at
`/projections/matrixXInd` and `/projections/matrixXOcc`.

**Context:** The Oregon spec used `ioMatrix/listIndustries` and `ioMatrix/listOccupations` as
ad-hoc sub-endpoints with no clear WID table backing. WID 3.0 formalises the directory concept
with `IndDirectories` and `OccDirectories`, and separately has crosswalk tables (`MatrixXInd`,
`MatrixXOcc`) that map matrix codes to standard industry/occupation codes.

**Rationale:** Exposing all four tables keeps the native structure accessible, and their distinct
purposes are now clear from the endpoint names.

---

## Decision 8: Licensing tables fully included

**Decision:** The WID 3.0 spec includes five licensing-related endpoints:
`/licensing/authorities`, `/licensing/licenses`, `/licensing/history`,
`/licensing/occupationCrosswalks`, and `/views/licensingByOccupation`.

**Context:** Licensing tables (`LicenseAuthorities`, `License`, `LicenseHistory`, `LicenseXOcc`)
are new in WID 3.0 with no equivalent in WID 2.8. The Oregon spec therefore has no coverage of
this data.

**Rationale:** Licensing data is a core WID 3.0 deliverable for states receiving ETA funding.
Omitting it from the API spec would make the spec incomplete relative to the WID 3.0 standard.

---

## Decision 9: State-agnostic `stFips` parameter

**Decision:** The `stFips` query parameter accepts any valid 2-digit state FIPS code. It is never
restricted to a single state in the spec.

**Context:** The Oregon spec hardcoded `stFips` to only allow `'00'` (national) or `'41'` (Oregon).
This was appropriate for a single-state deployment but is not appropriate for a national spec.

**Rationale:** A national spec must be usable by any state. Restricting the enum in the spec
would prevent other states from deploying a conforming implementation.

---

## Decision 10: Dual pagination modes supported on all list endpoints

**Decision:** Every list endpoint supports both page-number pagination (`page`, `pageSize`) and
cursor pagination (`cursor`, `limit`) using the same response envelope.

**Context:** The initial draft used page-number pagination only and raised cursor pagination as an
open question for large result sets. The working preference is to keep pagination behaviour
consistent across the API instead of introducing endpoint-specific differences.

**Rationale:** A uniform model keeps client SDKs and implementation templates simple. Callers can
start with page-based traversal and switch to cursor mode for stable deep scans without changing
endpoint-level assumptions.

**Implementation notes:**
- If `cursor` is supplied, implementations should treat cursor mode as authoritative and ignore
   `page`.
- `limit` controls page size for cursor mode; if omitted, implementations may use `pageSize`.
- `meta.paginationMode` indicates which strategy was used.
- `meta.nextCursor` is populated when cursor mode has additional records.

---

## Resolved questions

- **`AreaTypeVersion` default (was #1)** — Resolved: no implicit default filter applied.
  Omitting `areaTypeVersion` returns records across all versions. Callers who want only the
  current definition supply `areaTypeVersion=0` explicitly. Documented in the parameter
  description in the spec.

- **Industry/area code padding (was #3)** — Resolved: server-side normalization.
  - Area codes (6 chars): accept bare numeric code, left-pad with `'0'` to 6 characters.
  - Industry/occupation codes (10 chars): accept bare code, right-pad with blanks (ASCII 32)
    to 10 characters per WID 3.0 structure specification.
  - Supplying the already-padded form also works. Documented in parameter descriptions.

## Open questions / items for discussion

1. **Suppress field semantics** — Should suppress flags be promoted to booleans in the API
   response even though the database stores them as char(1)? This would be friendlier for
   API consumers but creates a mismatch with the WID structure document.

2. **`empDB` table** — The Employer Database (`EmpDB`) table is a WID 3.0 core table but was
   omitted from this draft because it contains employer-level data with significant PII
   considerations. Should a read-only aggregate or suppressed view be included?

3. **Query parameter names vs. response schema field names for industry/occupation codes** —
   The WID 3.0 database uses `IndCodeType` / `IndCode` / `OccCodeType` / `OccCode`.  The
   current API spec uses the more readable `industryCodeType` / `industryCode` /
   `occupationCodeType` / `occupationCode` as query parameter names, but the response
   schemas use the abbreviated DB names (`indCodeType` / `indCode` / `occCodeType` /
   `occCode`).  This inconsistency means a developer filters by `industryCodeType` but
   receives `indCodeType` in the response.

   **Options:**
   - **Option A — Defer to DB spec everywhere:** rename query params to match DB abbreviations
     (`indCodeType`, `indCode`, `occCodeType`, `occCode`).  Consistent with DB, less readable.
   - **Option B — Use readable names everywhere:** rename schema fields to match the current
     query params (`industryCodeType`, `industryCode`, `occupationCodeType`, `occupationCode`).
     Consistent with the "clarity" design goal, diverges from DB field names.

   Recommendation for group discussion: Option B is more consistent with the stated design
   philosophy ("avoid abbreviations") and produces a better developer experience.  Since the
   API layer is explicitly allowed to improve on DB naming, this is an appropriate place to do so.

4. **`Benchmark` field meaning** — The `Benchmark` field appears in the `CES` and `LaborForce`
   tables (char(4), references the `Benchmark` lookup table).  Its relationship to BLS benchmark
   revision cycles and how it should be used by API consumers is not clearly documented in the
   current draft.  The group should clarify:
   - What does a specific `Benchmark` value (e.g., `'2023'`) mean for a CES record?
   - Should the API expose `benchmark` as a filterable parameter on `/ces` and `/laborForce`?
   - Is the `/lookups/benchmarks` endpoint sufficient, or does the spec need more explanation
     of how benchmark revisions affect time-series continuity?
