# National WID API — Handoff Document
**Last updated: 2026-06-12**

> Latest pause-point handoff: see `HANDOFF-2026-06-12.md` first.

This document captures the complete current state of the project.
A new agent or developer can use this as the sole starting point.

---

## 1. What This Project Is

The **National WID 3.0 API** is a federally-hosted REST API (GovCloud, AWS GovDev/GovProd)
that gives state workforce agencies programmatic access to nationally-available labor market
data aligned with the WID 3.0 database structure. State users authenticate via the ULMITA SSO
portal (Cognito).

**Repository:** `c:\Users\mattsteadman\source\repos\fed-national-wid`
**Reference spec:** `../widapi/specs/wid-3.0/draft.json` (OpenAPI 3.1.0)
**Reference doc:** `../widapi/specs/wid-3.0/WID-3.0-Structure-20251120.md`

---

## 2. Live Environment (GovDev)

| Resource | Value |
|---|---|
| AWS profile | `GovDev` |
| Region | `us-gov-west-1` |
| CloudFormation stack | `dev-national-wid-api` |
| Stack status | `UPDATE_COMPLETE` |
| API Gateway URL | `[REDACTED-LIVE-API-URL]` |
| Aurora cluster endpoint | `[REDACTED-LIVE-AURORA-ENDPOINT]` |
| Aurora DB name | `nationalwid` |
| Aurora master user | `widadmin` |
| Aurora master password secret | `dev-national-wid-db-password` (Secrets Manager) |
| Ingestion Lambda | `dev-national-wid-ingestion` |
| API Lambda | `dev-national-wid-api` (served by API Gateway) |
| BLS flat-file base URL env var | `BLS_FLATFILE_BASE_URL` (defaults to `https://download.bls.gov/pub/time.series`) |
| BLS API key Parameter Store | `/wid-api/bls-api-key` |
| DB connection string Parameter Store | `/wid-api/db-connection-string` |
| Cognito user pool | `us-gov-west-1_OGMbjPhYh` (ULMITA dev) |
| Cognito client ID | `1151pibjvfgecq8d761drjcmu8` |

---

## 3. Project Structure

```
fed-national-wid/
├── justfile                           — task runner (just build, just deploy-dev, etc.)
├── nuget.config
├── NationalWid.Api.slnx
├── cloud-deployment/
│   ├── lambda.template                — SAM/CloudFormation: Aurora + both Lambdas + API GW + Cognito authorizer
│   ├── dev.deployment-profile.jsonc   — dev override params (VPC, Cognito, capacity, etc.)
│   ├── prod.deployment-profile.jsonc  — prod override params
│   └── migrations/
│       ├── 001_create_schema.sql      — all WID 3.0 tables (PK, FK constraints)
│       ├── 002_create_indexes.sql     — performance indexes
│       └── 003_seed_lookups.sql       — lookup seeding (geographies, period-years, etc.)
├── dev-scripts/
│   ├── Deploy.ps1                     — main deploy script; call with profile path
│   ├── Run-Migrations.ps1             — invokes ingestion Lambda in migration-only mode
│   ├── Tail-Logs.ps1                  — tails CloudWatch logs for either Lambda
│   └── Watch-Stack.ps1                — polls stack status during deploy
├── src/
│   ├── NationalWid.Api/
│   │   ├── Program.cs                 — app wiring: EF Core, Cognito JWT, controllers
│   │   ├── Controllers/               — one controller per WID table
│   │   │   ├── HealthController.cs    — GET /health (unauthenticated)
│   │   │   ├── CesController.cs       — GET /ces
│   │   │   ├── LaborForceController.cs — GET /labor-force
│   │   │   ├── IndustryController.cs  — GET /industry
│   │   │   ├── WagesController.cs     — GET /wages
│   │   │   ├── ProjectionsController.cs — GET /projections
│   │   │   └── LookupController.cs    — GET /lookups/*
│   │   ├── Data/WIDDbContext.cs        — EF Core DbContext (all WID 3.0 entities)
│   │   ├── Models/                    — C# records for all WID 3.0 tables
│   │   └── Services/
│   │       ├── QueryService.cs        — cursor+page pagination helper
│   │       └── DownloadService.cs     — CSV/XLSX response generation
│   └── NationalWid.Ingestion/
│       ├── Function.cs                — Lambda entrypoint; orchestrates migration + ingestors
│       ├── Ingestors/
│       │   ├── BlsLausIngestor.cs     — LAUS flat-file-first, then API fallback
│       │   ├── BlsCesIngestor.cs      — CES flat-file-first, then API fallback
│       │   ├── BlsQcewIngestor.cs     — QCEW via BLS CEW JSON API (quarters)
│       │   └── BlsOesIngestor.cs      — OES via BLS Public API v2
│       ├── Services/
│       │   ├── MigrationService.cs    — idempotent migration runner (history table)
│       │   ├── IngestLogService.cs    — writes per-dataset status to ingestlog table
│       │   └── BlsFlatFileService.cs  — downloads + parses BLS tab-delimited flat files
│       └── Models/                    — BLS API/response shapes, row transfer objects
└── tests/
    ├── NationalWid.Api.Tests/
    └── NationalWid.Ingestion.Tests/   — MigrationService helper tests + flat-file parse tests
```

---

## 4. What Is Confirmed Working (as of 2026-06-11)

### 4a. Infrastructure
- CloudFormation stack `dev-national-wid-api` is `UPDATE_COMPLETE`.
- Aurora Serverless v2 (PostgreSQL 16) is provisioned and reachable from Lambda (VPC + SG ingress fixed).
- VPC DNS hostnames are enabled (previously needed and fixed).
- API Gateway HTTP API is live with Cognito authorizer.
  - `GET /health` returns `200 OK` unauthenticated.
  - All other routes return `401` without a valid ULMITA JWT.

### 4b. Database / Schema
- All three migration scripts have been applied idempotently to the Aurora dev cluster.
- Migration history is tracked in `wid_migration_history`.
- Table-existence verification passes for all required WID 3.0 tables:
  `laborforce`, `ces`, `industry`, `iowage`, `projectionsmatrix`, `geographies`,
  `periodyears`, `ingestlog`.
- Last verified run output:
  ```json
  { "MigrationsApplied": 0, "MigrationsSkipped": 3,
    "TableChecks": { "laborforce": true, "ces": true, "industry": true,
                     "iowage": true, "projectionsmatrix": true,
                     "geographies": true, "periodyears": true, "ingestlog": true } }
  ```

### 4c. Ingestion (latest successful invoke: 2026-06-11 21:55 UTC)
- **LAUS**: 245 rows upserted (4 national series, ~20 years). Source: BLS flat file `ln.data.1.AllData`.
- **CES**: 4,655 rows upserted (19 national supersector series). Source: BLS flat file `ce.data.0.AllCESSeries`.
- **QCEW**: 0 rows (see known issue below).
- **OES**: 0 rows (see known issue below).
- `Errors: 0` — pipeline runs clean.

Full invoke result:
```json
{"Laus":245,"Ces":4655,"Qcew":0,"Oes":0,
 "MigrationsApplied":0,"MigrationsSkipped":3,
 "TableChecks":{"laborforce":true,"ces":true,"industry":true,"iowage":true,
                "projectionsmatrix":true,"geographies":true,"periodyears":true,"ingestlog":true},
 "Errors":0}
```

### 4d. Build + Tests
- `dotnet build` — all projects succeed, no warnings treated as errors.
- `dotnet test tests/NationalWid.Ingestion.Tests/` — 4/4 pass (migration helpers + flat-file parse).
- `dotnet test tests/NationalWid.Api.Tests/` — passes (placeholder tests; see gap below).

### 4e. BLS flat-file access from Lambda
- Fixed: BLS was returning 403 when Lambda used generic User-Agent.
- Solution: `BlsFlatFileService.cs` now uses ULMITA-branded headers:
  ```
  User-Agent: ULMITA/1.0 (ulmita.org; mattsteadman@utah.gov)
  Cache-Control: no-cache
  Accept: */*
  Accept-Encoding: gzip, deflate, br
  ```
- Manual content-encoding decompression is applied via `CreateDecodedStream()` (GZip / Deflate / Brotli).

---

## 5. Known Issues / Gaps

### 5a. QCEW — 0 rows (source route returns 404)
**Status:** Non-blocking (logs show "source unavailable", pipeline continues).
**Root cause:** The BLS CEW API URL pattern
`https://data.bls.gov/cew/data/api/{year}/{qtr:D2}/area/US000.json`
returns 404 for every quarter in the range 2021–2026. Likely causes:
- The area code `US000` may need to be `US` or a different key for US national.
- The `qtr:D2` format may be wrong; BLS may expect `1`/`a1` style.
- The route may have changed since the code was written.
**Fix approach:** Inspect an actual BLS QCEW download page (https://www.bls.gov/cew/downloadable-data.htm)
to verify the current API contract, or switch to downloading the annual QCEW flat files.

### 5b. OES — 0 rows (API returns data but BuildRows produces nothing)
**Status:** Non-blocking, but data gap.
**Root cause:** The BLS API returns OES series data, but `BuildRows` in `BlsOesIngestor.cs`
may be extracting the SOC code incorrectly from the series ID, causing every row to be skipped.
The series format is `OEU + 0000000 (7 chars) + 000000 (6 chars) + {occ6} (6 chars) + 04 (2 chars)`.
The occupation code should be extracted at `seriesId[13..19]` (0-indexed).
**Fix approach:** Add a debug log line printing extracted SOC code and value for the first 3 rows
in `BuildRows`, redeploy, check CloudWatch.

### 5c. API controllers not smoke-tested against live data
**Status:** Non-blocking for ingestion; blocks validation of full data pipeline.
The controllers are scaffolded and deployed but have not been tested end-to-end with
real data from Aurora via authenticated API requests.
**Fix approach:** Obtain a ULMITA JWT from the Cognito dev pool and run:
```
GET /ces?stFips=00    (expecting rows after ingestion)
GET /labor-force      (expecting rows after ingestion)
GET /lookups/geographies
```

### 5d. API tests are placeholder only
`tests/NationalWid.Api.Tests/UnitTest1.cs` has no real tests. Integration / smoke tests
for the API controllers would be valuable.

### 5e. Projections (ETA) — not implemented
`ProjectionsController.cs` is scaffolded but the ingestion Lambda has no `BlsProjectionsIngestor`.
The ETA projections flat file source and data mapping are undefined.

### 5f. Licensing data — not implemented
Per AGENTS.md: `License / GET /licensing` has no BLS source. Deferred.

---

## 6. How to Operate

### Deploy to dev
```powershell
./dev-scripts/Deploy.ps1 ./cloud-deployment/dev.deployment-profile.jsonc
```

### Run migrations only (safe to run repeatedly — idempotent)
```powershell
./dev-scripts/Run-Migrations.ps1
```
Or via justfile:
```
just migrate-dev
just verify-schema-dev
```

### Invoke ingestion manually
```powershell
aws lambda invoke `
  --function-name dev-national-wid-ingestion `
  --profile GovDev --region us-gov-west-1 `
  --cli-binary-format raw-in-base64-out `
  --payload '{}' invoke-output.json
Get-Content invoke-output.json
```

### Ingestion request modes (send as payload)
```json
{ "mode": "RunMigrationsOnly" }   // apply + verify schema only, no data
{ "mode": "VerifyOnly" }          // check tables exist, no changes
{ "mode": "SkipMigrations" }      // ingest data only (schema assumed present)
{}                                 // default: run migrations first, then ingest all datasets
```

### Tail ingestion logs
```powershell
aws logs tail /aws/lambda/dev-national-wid-ingestion `
  --profile GovDev --region us-gov-west-1 `
  --since 5m --format short
```
Or: `just tail-ingestion-dev`

### Build and test
```
just build
just test
```

---

## 7. Architecture Overview

### Request flow (data API)
```
State user
  → ULMITA login (Cognito, us-gov-west-1_OGMbjPhYh)
  → JWT with custom:stFips claim
  → API Gateway HTTP API (Cognito authorizer)
  → NationalWid.Api Lambda (ASP.NET Core)
  → EF Core → Aurora PostgreSQL (WID 3.0 schema)
  → JSON / CSV / XLSX response
```

### Ingestion flow
```
EventBridge Scheduler (monthly)
  → NationalWid.Ingestion Lambda
  → MigrationService (idempotent schema check)
  → Per-dataset ingestors:
       BlsLausIngestor  → BlsFlatFileService (ln flat files) OR BLS API v2
       BlsCesIngestor   → BlsFlatFileService (ce flat files) OR BLS API v2
       BlsQcewIngestor  → BLS CEW JSON API (quarters)
       BlsOesIngestor   → BLS API v2
  → Upsert rows into Aurora PostgreSQL
  → IngestLogService → ingestlog table
```

### Response envelope (all list endpoints)
```json
{
  "meta": { "total": 4655, "page": 1, "pageSize": 100, "nextCursor": "..." },
  "data": [ ... ],
  "links": { "self": "/ces?page=1", "next": "/ces?cursor=..." }
}
```

---

## 8. Key Files — What Each Does

| File | Purpose |
|---|---|
| `src/NationalWid.Ingestion/Function.cs` | Lambda entrypoint; orchestrates migrations then ingestors; returns `IngestSummary` JSON |
| `src/NationalWid.Ingestion/Services/MigrationService.cs` | Idempotent migration runner; `wid_migration_history` tracking; `to_regclass` table verification |
| `src/NationalWid.Ingestion/Services/BlsFlatFileService.cs` | Downloads BLS tab-delimited flat files; ULMITA-style headers; explicit gzip/deflate/brotli decode |
| `src/NationalWid.Ingestion/Services/IngestLogService.cs` | Safe write to `ingestlog` table; swallows exceptions to avoid pipeline hard-fail |
| `src/NationalWid.Ingestion/Ingestors/BlsLausIngestor.cs` | LAUS: flat-file first (`ln.data.1.AllData`), API fallback; 4 national series |
| `src/NationalWid.Ingestion/Ingestors/BlsCesIngestor.cs` | CES: flat-file first (`ce.data.0.AllCESSeries`), API fallback; 19+ national supersector series |
| `src/NationalWid.Ingestion/Ingestors/BlsQcewIngestor.cs` | QCEW: BLS CEW JSON API by quarter; returns 0 (source returns 404 — see §5a) |
| `src/NationalWid.Ingestion/Ingestors/BlsOesIngestor.cs` | OES: BLS API v2; 20 SOC major groups; 0 rows currently (see §5b) |
| `src/NationalWid.Api/Program.cs` | ASP.NET Core wiring: Cognito JWT auth, EF Core, controllers, CSV/XLSX support |
| `src/NationalWid.Api/Data/WIDDbContext.cs` | EF Core context for all WID 3.0 tables |
| `cloud-deployment/lambda.template` | SAM template: Aurora cluster, 2 Lambdas, API Gateway, Cognito authorizer, EventBridge scheduler |
| `cloud-deployment/migrations/001_create_schema.sql` | All WID 3.0 table DDL |
| `dev-scripts/Deploy.ps1` | `sam package` + `sam deploy` with correct `;`-joined parameter string |
| `dev-scripts/Run-Migrations.ps1` | Invokes ingestion Lambda in `RunMigrationsOnly` mode and validates output |

---

## 9. Remaining Work (Priority Order)

### P1 — QCEW source fix
Diagnose why the CEW API returns 404 for all quarters.
Likely one-line fix to area code or URL format.
Acceptance: `Qcew > 0` in invocation output.

### P2 — OES rows fix
Add debug logging in `BlsOesIngestor.BuildRows`, redeploy, check SOC extraction.
The BLS API call succeeds; the issue is in the parse step.
Acceptance: `Oes > 0` in invocation output.

### P3 — End-to-end API smoke test
Obtain a ULMITA JWT, run authenticated queries against `GET /ces` and `GET /labor-force`.
Confirm rows come back from Aurora.
Acceptance: 200 response with `data` array containing real records.

### P4 — Projections ingestor
Implement `BlsProjectionsIngestor` using the ETA Employment Projections data source.
Map to `projectionsmatrix` table columns.

### P5 — API integration tests
Replace placeholder `UnitTest1.cs` in `NationalWid.Api.Tests` with real controller tests
using `WebApplicationFactory` or a test DB.

### P6 — Prod deployment
Update `prod.deployment-profile.jsonc` with correct prod Cognito client ID from Parameter Store,
then run `./dev-scripts/Deploy.ps1 ./cloud-deployment/prod.deployment-profile.jsonc`.

---

## 10. Non-Goals / Do Not Change

- Do not redesign the API contract (WID 3.0 table-native endpoints are defined by ETA grant).
- Do not replace Cognito / ULMITA auth.
- Do not switch off .NET 8.
- Do not move off AWS GovCloud profiles (`GovDev` / `GovProd`).
- Do not hardcode secrets — always use Parameter Store / Secrets Manager.

---

## 11. Quick Restart Prompt for Next Agent

> Continue implementation of National WID API in `fed-national-wid`.
> Read `HANDOFF.md` for current state. The core pipeline is working:
> LAUS (245 rows) and CES (4655 rows) ingest successfully from BLS flat files.
> QCEW returns 0 because the BLS CEW JSON API URL returns 404 for all quarters — fix the
> area code or URL format in `BlsQcewIngestor.cs`. OES also returns 0 despite a successful
> API call — add a debug log of extracted SOC code in `BuildRows` to diagnose.
> After fixing both, run `just build`, `just test`, deploy dev, invoke ingestion, and
> confirm non-zero counts for QCEW and OES. Then do an authenticated API smoke test against
> `GET /ces` to confirm data is queryable end-to-end.
> AWS profile: GovDev, region: us-gov-west-1, stack: dev-national-wid-api.
> Do not change auth model, API contract, or .NET version.
