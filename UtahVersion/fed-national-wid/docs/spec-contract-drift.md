# Spec vs. Implementation: Contract Drift

**Bottom line: don't treat `specs/wid-3.0/draft.yaml` (this repository's root `specs/` folder) as
ground truth for this API without checking it against this document first.** Utah's own prior
parity claims — `api-contract-parity-closure-2026-07-31.md`'s "No remaining endpoint-level gaps
were found" and this handoff's earlier draft of `HANDOFF.md` repeating that claim — are
**demonstrably wrong**, confirmed by reading the actual spec file and all 11 controllers side by
side, not by trusting either document's prior conclusions.

This isn't a criticism of the earlier work — the spec and implementation drifted apart gradually
over real, incremental feature work, which is exactly how contract drift normally happens. It's
flagged here so NC doesn't inherit the same false confidence.

## Why this happened

`specs/wid-3.0/draft.yaml` was last substantively edited **2026-04-08** (per this repository's own
git history). CPI and the API key system — two entire feature areas with zero representation in
the spec — were built **2026-07-29 through 2026-08-04**, more than three months later.
`AGENTS.md`'s governance rule ("update the spec in the same workstream," "do not treat API work as
complete until spec/docs parity is addressed") was not followed for either feature. The spec has
no version-date field a consumer could use to detect this staleness without cross-referencing git
history the way this document does.

## What's undocumented in the spec entirely

- **All four CPI endpoints** (`/cpi`, `/cpi/metadata`, `/cpi/items`, `/cpi/areas`) — no `cpi` tag,
  no paths, no schemas anywhere in `draft.yaml`.
- **All API key management endpoints** (`/api-keys` CRUD) — likewise absent.
- **Auth entirely** — `draft.yaml` has no `security`/`securitySchemes` section at all. The spec is
  silent on authentication for every endpoint, so it can't currently be used to answer "does this
  endpoint require a token" — that's only answerable from the implementation (see
  [architecture.md](./architecture.md) and [README.md](../README.md#authentication)), including the
  CPI-is-anonymous anomaly flagged in [known-issues-and-gaps.md](./known-issues-and-gaps.md).

## Where the spec and implementation actively disagree

### The `/projections/indDirectories` and `/projections/occDirectories` paths don't exist where the spec says

The spec places these at `/projections/indDirectories`/`/projections/occDirectories` with
parameters `periodType`, `periodYear`, `period`, `projectedYear`, `matrixIndCode`. The
implementation serves this data at `/lookups/ind-directories`/`/lookups/occ-directories` instead
(inside `LookupController`, not `ProjectionsController`), with a completely different parameter
set (`stFips`, `projPeriod`, `indCodeType`, `indCode`). A client built strictly against the spec's
literal URLs gets a 404 here — unlike most other lookup/view endpoints, which got both a spec-style
and an `AGENTS.md`-style route as aliases, this one only got the `AGENTS.md`-style route.
Interestingly, **`AGENTS.md` itself (the original build brief) told the implementation to put these
under `/lookups/`** — so the implementation followed one internal source of truth while the spec
represents a different, contradictory one. This is worth resolving explicitly (which URL is
"right" going forward) rather than silently picking one.

### `ProjectionsMatrix` is architecturally different, not just field-renamed

- The spec's `Projection` key is `(..., periodYear, periodType, period, matrixIndCode,
  matrixOccCode)` — separate year/type/period fields. The implementation collapses base/projected
  year into a single string `ProjectionsPeriod` (e.g. `"2022-2032"`), matched via `StartsWith`/
  `EndsWith` string comparisons. **Concretely**: if a caller passes `periodType`/`period` per the
  spec's generic parameter model with any value other than the implementation's hardcoded `"01"`/
  `"00"`, the controller silently returns zero rows rather than an error — a spec-conformant
  request that looks reasonable produces an empty result set with no indication anything's wrong.
- The spec's `matrixIndCode`/`matrixOccCode` are described as a distinct "MicroMatrix" code space,
  translated to real industry/occupation codes only via the `MatrixXInd`/`MatrixXOcc` crosswalk
  tables. In the implementation, `matrixIndCode`/`matrixOccCode` are literal aliases for
  `indCode`/`occCode`, and the crosswalk endpoints derive rows via an **identity self-mapping**
  (`MatrixIndCode = x.IndCode`, `IndCode = x.IndCode`). The two-code-space concept the spec and
  `docs/decisions/2026-04-07-wid-3-api-design.md` (Decision 7, in this repo's root `docs/`)
  describe doesn't exist in the implementation as built.
- The `/views/projectionsWithTitles` response drops most of the base table's fields (only
  `BaseYearEmp, ProjectedEmp, Change, PctChange, Openings, SuppRecord` survive into the view DTO)
  even though the underlying `ProjectionsMatrix` model has the growth/exits/transfers fields the
  spec's `Projection` schema expects.

### Core table field names diverge from the spec in places

Checked against the actual EF models, not just view DTOs:

| Table | Spec fields the implementation doesn't populate | Implementation fields not in the spec |
|---|---|---|
| `Ces` | `benchmark`, `empFemaleWorkers`, `suppFemaleWorkers`, `hoursAllWorkers`, `earningsAllWorkers`, `hourlyEarningsAllWorkers`, `suppHEAllWrkr` | `AvgWeeklyEarningsPctChange` |
| `Industry` | `avgWeeklyWage` (spec name) vs. `WeeklyWage`/`weeklyWage` (implementation JSON key — different key, same concept) | `AvgMonthlyEmp`, `EmpCount`, `HighEmpQ`, `LowEmpQ` (explicitly commented in code as kept for backward compat, not spec fields — see [database-schema.md](./database-schema.md)) |
| `IOWage` | — (all spec fields present) | `MeanHourly`, `AnnualMean` (explicitly commented as OEWS-only extensions) |
| `Geography` | — | `AreaTypeTitle` (explicitly commented as an extension field) |
| `LaborForce` | — (all spec fields present) | `SuppRecord`, `SuppRate` (not in the spec's `LaborForce` schema at all) |

Most of this is additive extension fields (already documented as intentional in
[database-schema.md](./database-schema.md)'s migration-012 section) rather than missing
functionality — but the CES gap (7 spec fields never populated) is a real content gap, not just a
naming difference, worth flagging to whoever maintains the spec.

### Route casing: two internally-contradicting sources of truth, inconsistently reconciled

The spec uses camelCase paths (`/lookups/periodYears`, `/lookups/cesCodes`, `/lookups/areaTypes`,
etc.); `AGENTS.md`'s original table used kebab-case (`/lookups/period-years`,
`/lookups/ces-codes`). The implementation added **both** as route aliases on most lookup/view/
labor-force endpoints — except `/projections/indDirectories`/`occDirectories` (see above, no
alias at all) and `LicensingController`, which only implements the spec's camelCase style
(`occupationCrosswalks`) with no kebab alternative. There's no single consistent rule being
followed; it reads as endpoint-by-endpoint reactive patching rather than a settled convention.

### `/status` has real sub-resources and query parameters the spec doesn't describe

The spec's `/status` takes no parameters and returns a fixed `{apiVersion, generatedAt, tables[]}`
shape. The implementation adds an undocumented `?report=coverage|history` switch plus two entirely
separate sub-paths, `/status/history` and `/status/coverage`, each with their own `dataSet`/`page`/
`pageSize` parameters — see [architecture.md](./architecture.md#statushealth-surface) for what
these actually do. None of it is in the spec.

## Endpoints in the spec that aren't implemented

None found as outright missing — every spec-described resource has *some* implementation, except
the `/projections/indDirectories`/`occDirectories` routing mismatch above (which is functionally
"not implemented at the spec-literal URL," even though equivalent data exists elsewhere).

## Governance questions this raises for NC — not answerable from source

1. **Who is the maintainer/approver for `specs/wid-3.0/draft.yaml` now?** Every commit in this
   repository (including the audit-fix commits from this handoff) is authored by the same Utah
   contact. There's no `CODEOWNERS` file and no listed reviewer contact beyond a generic "WID 3.0
   Working Group" name in the spec's own metadata. Once Utah steps back, who actually merges spec
   changes?
2. **Is there still a live ARC-consortium review process for spec changes?** The spec's own header
   says "this is a living document — open a pull request to propose changes," implying a review
   workflow, but nothing in the repository documents who reviews ARC-wide changes, what approval
   is required, or how competing state interests get resolved.
3. **How should NC propose fixing the drift documented here?** `AGENTS.md`'s prescribed workflow
   (implement → update spec same-workstream → add a decision record → close parity) was
   demonstrably not followed for CPI or API keys. Is that workflow still the expectation going
   forward, or does it need revising now that a second state is actively involved?
4. **Should the CPI-is-anonymous behavior be a spec decision or a bug fix?** This affects
   data-sharing/access posture for a nationally-hosted, multi-state API — worth an explicit
   governance-level answer, not just a unilateral code change either way.
5. **The four "Open questions" already listed in
   `docs/decisions/2026-04-07-wid-3-api-design.md`** (this repo's root `docs/decisions/`) —
   suppress-field booleans vs. strings, an `EmpDB` omission, industry/occupation query-param vs.
   response-field naming inconsistency, and `Benchmark` field semantics — are all still unresolved
   and unaddressed by anything written since. Confirm these get tracked, not just re-discovered.

## What NC should actually do with this

- **Don't build client code against `draft.yaml` for CPI, API keys, Projections/matrix semantics,
  or the `/lookups` vs. `/projections` directory split** until it's explicitly reconciled with the
  real implementation (this document, or a corrected spec).
- **The core CRUD shape for CES/LaborForce/Industry/Wages/Licensing** — pagination envelope,
  filtering/sorting conventions, area/code normalization rules — is substantially aligned and
  lower-risk to build against as-is, aside from the specific field gaps in the table above.
- **Adding real OpenAPI generation to the API** (Swashbuckle or NSwag — see
  [known-issues-and-gaps.md](./known-issues-and-gaps.md)) would make future drift like this
  continuously detectable by diffing two machine-readable specs, instead of requiring another
  manual side-by-side read every time someone needs to trust the spec again.
