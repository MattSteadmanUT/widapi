# WID API Spec Repository — Agent Instructions (AGENTS.md)

## Mission
Maintain the canonical OpenAPI contract and supporting documentation for the National WID 3.0 API.

---

## Source of truth rules

1. `specs/wid-3.0/draft.yaml` is the human-edited source of truth for the WID 3.0 contract.
2. `specs/wid-3.0/draft.json` is generated/derived for Swagger UI consumption and must remain in sync with YAML.
3. API docs and decision records under `docs/` must reflect the implemented behavior of the production-bound API.
4. If implementation in `fed-national-wid` conflicts with `widapi`, `widapi` takes precedence and implementation must be reconciled.

---

## Cross-repo contract relationship (widapi <-> fed-national-wid)

`widapi` is the contract authority; `fed-national-wid` is the implementation authority. They must describe the same API behavior.

### Parity requirement
1. Any API change in `fed-national-wid` that affects routes, parameters, auth, filtering, sorting, pagination, response envelope, or field schema must be reflected in `widapi`.
2. In the ideal state, generated OpenAPI/Swagger from `fed-national-wid` is identical in contract semantics to `widapi/specs/wid-3.0/draft.yaml`.
3. During reviews, treat `widapi/specs/wid-3.0/draft.yaml` as the canonical acceptance contract for API behavior.

### Required synchronization workflow
1. When implementing API changes in `fed-national-wid`, update `widapi/specs/wid-3.0/draft.yaml` in the same workstream.
2. Update examples and parameter descriptions to match runtime behavior exactly.
3. Add a decision document in `docs/decisions/` for material design changes.
4. Do not close implementation work until contract parity is complete or an explicit temporary drift note is recorded.

### Temporary drift policy
1. If parity cannot be completed immediately, record the drift and remediation plan in both repositories.
2. Drift notes must include:
   - changed endpoints/fields/params,
   - expected final behavior,
   - owner,
   - target date to reconcile.

---

## Editing conventions

1. Edit YAML, not JSON, when changing spec content.
2. Keep endpoint naming and field semantics aligned with WID 3.0 structure documentation.
3. Preserve stable contract behavior unless a design decision explicitly authorizes a breaking change.
