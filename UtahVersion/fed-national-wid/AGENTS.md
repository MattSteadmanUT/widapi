# National WID API — Agent Instructions (AGENTS.md)

## Status

This project is **built and was operated in production-like use by Utah DWS**, then handed off to
North Carolina to continue. This file used to be the original greenfield build brief — it has been
rewritten to describe *current* state, since a stale "build this from scratch" brief would mislead
an AI coding agent into recreating things that already exist. For full handoff status, start at
[`../HANDOFF.md`](../HANDOFF.md); for deep architecture/operations docs, see `docs/` in this
folder. This file stays focused on what a coding agent working in this repo needs to know.

## Mission

The **National WID 3.0 API** is a nationally-hosted REST API that provides state workforce
agencies with programmatic access to nationally-available labor market data aligned with the WID
3.0 database structure, pre-loaded from BLS and other public sources so states can query national
benchmarks or pull data into their own state WID systems.

## Project Context

### What is WID?
The Workforce Information Database (WID) is a standardized database structure (governed by the
ARC consortium / ETA grant) that state workforce agencies use to store and disseminate labor market
information. WID 3.0 (released 2024, revised May 2025) is the current version.

### What this API does — current state
- Serves nationally-available labor market data (BLS LAUS/CES/QCEW/OEWS/Employment Projections,
  BLS CPI-U, WID Center licensing and lookup data) pre-loaded into a WID 3.0-aligned Aurora
  PostgreSQL database via a scheduled ingestion Lambda — see `docs/ingestion-pipeline.md`.
- Exposes 11 controllers covering core WID tables, non-core lookups, joined "views," licensing,
  CPI, and self-service API key management — see the README's API Reference section for the full
  endpoint list, and `docs/architecture.md` for how the shared query/filter/sort/pagination engine
  under all of them works.
- Supports JSON, CSV, TSV, PSV, and XLSX output on every list endpoint.
- Authenticates via any OIDC-compliant identity provider (deployed against Utah's ULMITA Cognito
  user pool today, but not coupled to Cognito in code — see `docs/architecture.md`'s auth section)
  plus a self-service, long-lived API key system. There is **no per-state data access
  restriction** — any authenticated caller can query any state's data; this is intentional (a
  nationally-hosted public-data API), not a gap.

## Authentication

See `docs/architecture.md`'s "Auth: dual scheme" section for the full mechanism. Quick reference:

| Setting | Dev (Utah's ULMITA pool, as currently deployed) |
|---|---|
| User Pool ID | `us-gov-west-1_OGMbjPhYh` |
| Client ID | `1151pibjvfgecq8d761drjcmu8` |
| Region | `us-gov-west-1` |

`Program.cs` builds its OIDC issuer URL from `Cognito:Region`/`Cognito:UserPoolId` by default
(Cognito's URL shape), but an `Oidc:Authority` configuration value overrides that entirely — so
pointing this at a different identity provider is a configuration change, not a code change.

Public (unauthenticated) endpoints: `GET /health`, `GET /status`, `GET /status/history`,
`GET /status/coverage`.

## WID 3.0 Data Model — current table set

The WID 3.0 spec lives in this repository's sibling location (two directories up from this file):
`../../specs/wid-3.0/draft.yaml` (OpenAPI 3.1) and `../../specs/wid-3.0/WID-3.0-Structure-20251120.md`
(structure document). See the "Cross-Repo API Contract Governance" section below for how this
implementation and that spec are supposed to stay in sync — and `docs/known-issues-and-gaps.md`
for where that governance process itself has open questions for NC (e.g. no automated OpenAPI
generation currently exists in this API to diff against the spec).

### Core tables
| WID Table | API Path | Source |
|---|---|---|
| `LaborForce` | `GET /labor-force` | BLS LAUS |
| `CES` | `GET /ces` | BLS CES |
| `Industry` | `GET /industry` | BLS QCEW |
| `IOWage` | `GET /wages` | BLS OES/OEWS |
| `ProjectionsMatrix` | `GET /projections` | BLS/ETA Employment Projections |
| `License`, `LicenseAuthorities`, `LicenseHistory` (empty, source-constrained), `LicenseXOcc` | `GET /licensing/*` | WID Center `COSFlatExport` + per-state WID 2.8 exports |

### Added after the original build brief (not in the original spec-era table list)
| WID Table | API Path | Source |
|---|---|---|
| `CPI`, `CpiSeries`, `CpiItems`, `CpiAreas` | `GET /cpi*` | BLS CPI-U — added per 2026-07 stakeholder feedback as a wage-purchasing-power supplement, not a WID 3.0 core table |
| — | `POST/GET/PATCH/DELETE /api-keys` | Self-service API key management, unrelated to WID data |

### Non-core lookups and views
14 lookup tables under `/lookups/*` and `/projections/matrixX*`, 6 joined "view" endpoints under
`/views/*` (these are runtime LINQ joins, not persisted SQL views). Full inventory in
`docs/database-schema.md` and `docs/table-classification.md`.

### Key WID 3.0 design decisions still in effect
1. Table-native endpoints (each WID table = one endpoint).
2. No per-domain `listAreas`/`maxPeriod` sub-endpoints; shared `/lookups/geographies` plus
   per-table `/metadata` sub-endpoints instead.
3. `areaTypeVersion` is part of every geography key.
4. CES is a single endpoint (not split employment/hours-earnings).
5. `SeriesCodeType` is exposed alongside `SeriesCode` on CES.
6. Suppress fields are strings `'0'`/`'1'`, not booleans.
7. Standard response envelope: `{ meta, data, links }` with cursor+page pagination.

Five deliberate, documented deviations from the WID 3.0 spec exist in the schema (e.g.
`projectionsmatrix`'s period key shape, `ces.seriescodetype` storing `"NAICS"` instead of a WID
code for ~35k existing rows) — see `docs/database-schema.md`'s migration-012 section for the full
list and rationale before treating any of them as bugs to silently "fix."

## Technical Architecture

- **Language/Runtime:** .NET 8, C# — ASP.NET Core Web API.
- **Hosting:** AWS Lambda behind API Gateway HTTP API today — but the API itself is a portable
  ASP.NET Core app; AWS Lambda hosting is additive (see
  `docs/deployment-and-operations.md`'s "Platform portability" section for exactly what is and
  isn't AWS-specific before assuming this has to stay on AWS/Lambda).
- **IaC:** AWS SAM + CloudFormation (`cloud-deployment/lambda.template`).
- **Database:** Amazon Aurora Serverless v2 (PostgreSQL 16) — plain Npgsql/EF Core, not
  Aurora-specific at the code level.
- **Scheduler:** Amazon EventBridge Scheduler → ingestion Lambda, 7 dataset-group schedules.
- **Secrets:** AWS Systems Manager Parameter Store (ingestion Lambda only — the API project has no
  runtime AWS SDK dependency at all).

### Project structure

```
fed-national-wid/
├── AGENTS.md                     (this file)
├── README.md                     (API reference, quick start)
├── justfile                      (task runner)
├── nuget.config
├── NationalWid.Api.slnx
├── src/
│   ├── NationalWid.Api/          (ASP.NET Core Lambda project — 11 controllers)
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Controllers/
│   │   ├── Models/
│   │   ├── Services/              (the shared query/filter/sort/pagination/download engine)
│   │   └── Auth/
│   └── NationalWid.Ingestion/    (separate Lambda — 7 ingestors)
│       ├── Function.cs
│       ├── Ingestors/
│       └── Services/
├── cloud-deployment/
│   ├── lambda.template           (SAM template: API + Ingestion Lambdas + Aurora)
│   ├── dev.deployment-profile.jsonc
│   ├── prod.deployment-profile.jsonc (still has unfilled TODO_SET_PROD_* placeholders)
│   └── migrations/               (17 SQL migrations, sequential + idempotent)
├── dev-scripts/                  (PowerShell deployment/ops helpers)
├── tests/                        (xUnit — see docs/testing.md for coverage gaps)
└── docs/                         (architecture, ingestion pipeline, schema, deployment,
                                    testing, known-issues, and a first-deployment playbook)
```

## Key Conventions

1. **justfile** for all tasks: `just build`, `just run`, `just test`, `just deploy dev`.
2. **Deployment** via `dotnet lambda deploy-serverless` (wrapped by `dev-scripts/Deploy.ps1`).
3. **No hardcoded secrets** — always Parameter Store (ingestion) or config (API).
4. **Health endpoint** `GET /health` returns `200 OK { "status": "healthy" }`, unauthenticated.
5. Deployment-profile tags (`tagElcid`, `tagDept`, `tagDivision`, `tagContact`) are Utah's internal
   cost-center conventions — replace with NC's own before deploying, don't treat them as required
   values with fixed meaning.

## Immediate priorities for whoever picks this up next

See `../HANDOFF.md`'s "Recommended next steps" for the full prioritized list. In short: decide the
identity-provider and cloud-platform questions first (both are genuinely open, not assumed-AWS/
assumed-Cognito), fix the plaintext-SSM database password, write ingestor-level tests (6 of 7
ingestors currently have none), and complete an actual prod deployment (Utah never did).

---

## Cross-Repo API Contract Governance (fed-national-wid <-> widapi)

The API implementation (this repository, `UtahVersion/fed-national-wid/`) and the specification
(this repository's root `specs/wid-3.0/`) must remain synchronized. This section is unchanged from
the original build brief because the governance rule itself is still correct — only the sibling-repo
paths have changed now that both live in one repository.

### Source-of-truth hierarchy
1. `specs/wid-3.0/draft.yaml` (repo root) is the source of truth for API contract behavior and
   documentation.
2. This implementation must match the contract defined there.
3. If there is any conflict between implementation and spec/docs, resolve in favor of the spec and
   then update the implementation.

### Canonical contract rule
1. Any behavioral or contract change in this API's routes, query parameters, filtering, sorting,
   pagination, auth model, response envelope, or field names/types MUST be documented in
   `specs/wid-3.0/draft.yaml`.
2. In the ideal state, a generated OpenAPI spec from this API would be identical in contract
   semantics to `specs/wid-3.0/draft.yaml`. **As of this handoff, no such generation exists** — the
   API project has no Swagger/OpenAPI library registered (confirmed: no Swashbuckle, no NSwag, no
   generation code anywhere). Any comparison between the spec and this implementation is currently
   manual. Adding real OpenAPI generation (e.g. via Swashbuckle or NSwag) would make this rule
   actually enforceable instead of aspirational — worth prioritizing early.

### Required workflow for API changes
1. Implement the API change here.
2. Update `specs/wid-3.0/draft.yaml` in the same workstream.
3. Add/update supporting rationale in `docs/decisions/` (repo root) when the change is non-trivial.
4. Ensure examples and parameter descriptions in the spec reflect actual implementation behavior.
5. Do not treat API work as complete until spec/docs parity is addressed.

### Drift policy
1. If implementation temporarily diverges from the spec, record the drift explicitly and create a
   follow-up task to close the gap.
2. Prefer spec-first or same-PR synchronization over deferred backfill.
3. Governance-process question for NC: with Utah stepping back, who is the maintainer/approver for
   `specs/wid-3.0/draft.yaml` now? Is there still an active ARC-consortium review process for spec
   changes? This isn't answerable from the code — resolve it directly with the ARC consortium
   before assuming any particular approval process still applies.
