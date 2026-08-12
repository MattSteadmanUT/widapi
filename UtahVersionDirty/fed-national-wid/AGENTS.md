# National WID API — Agent Instructions (AGENTS.md)

## Mission
Build the **National WID 3.0 API** — a federally-hosted REST API that provides state workforce
agencies with programmatic access to nationally-available labor market data aligned with the
WID 3.0 database structure. State users authenticate via the ULMITA SSO portal (Cognito).

---

## Project Context

### What is WID?
The Workforce Information Database (WID) is a standardized database structure (governed by the
ARC consortium / ETA grant) that all 50 states use to store and disseminate labor market
information. Version 3.0 is the current version (released 2024, revised May 2025).

### What this API does
- **Serves nationally-available data** (BLS, Census, etc.) pre-loaded into a WID 3.0 compliant
  database, so state agencies can query national benchmarks and pull data into their own
  state WID systems.
- **Auto-populates from BLS** published data series (CES, LAUS, QCEW, OES/OEWS, Projections)
  on a scheduled basis.
- **Supports multiple download formats**: JSON (API), CSV, Excel (XLSX), potentially SDMX.
- **Requires ULMITA authentication** — all endpoints require a valid Cognito JWT from the
  ULMITA user pool. The `custom:stFips` claim identifies which state the user represents.

---

## Authentication — ULMITA Cognito

**Dev user pool:** `us-gov-west-1_OGMbjPhYh`  
**Dev client ID:** `1151pibjvfgecq8d761drjcmu8`  
**Prod user pool:** `us-gov-west-1_RMhTk2TxL`  
**Prod client ID:** (retrieve from Parameter Store `/prod/wid-api/cognito-client-id`)  
**Region:** `us-gov-west-1` (AWS GovCloud)  
**AWS profiles available:** `GovDev`, `GovProd`

### Relevant JWT claims
| Claim | Description |
|---|---|
| `custom:stFips` | State FIPS code (e.g., `"49"` for Utah). Every state user has this. |
| `custom:ulmita_activated` | `"true"` if the account is activated. |
| `custom:ulmita_roles` | Comma-separated list of roles (future use for admin). |
| `sub` | Cognito user UUID |
| `email` | User email |

### Auth requirement
API Gateway should use a **Cognito Authorizer** (not a Lambda authorizer) pointed at the
ULMITA user pool. All resource methods should require the authorizer. Public endpoints:
`GET /health`, `GET /status`.

---

## WID 3.0 Data Model (Core Tables to Expose)

The WID 3.0 spec is in `../widapi/specs/wid-3.0/draft.json` (OpenAPI 3.1.0).
The structure document is in `../widapi/specs/wid-3.0/WID-3.0-Structure-20251120.md`.

### Core data tables (mandatory per ETA grant)
| WID Table | API Path | BLS Source |
|---|---|---|
| `LaborForce` | `GET /labor-force` | BLS LAUS (series prefix: `LA`) |
| `CES` | `GET /ces` | BLS CES (series prefix: `CE`) |
| `Industry` | `GET /industry` | BLS QCEW (CEW data) |
| `IOWage` | `GET /wages` | BLS OES/OEWS |
| `ProjectionsMatrix` | `GET /projections` | ETA/BLS Employment Projections |
| `License` | `GET /licensing` | (future — no BLS source) |

### Lookup tables
| WID Table | API Path |
|---|---|
| `Geographies` | `GET /lookups/geographies` |
| `PeriodYears` | `GET /lookups/period-years` |
| `CEScodes` | `GET /lookups/ces-codes` |
| `IndDirectories` | `GET /lookups/ind-directories` |
| `OccDirectories` | `GET /lookups/occ-directories` |

### Key WID 3.0 design decisions (from `../widapi/docs/decisions/2026-04-07-wid-3-api-design.md`)
1. Table-native endpoints (each WID table = one endpoint)
2. No per-domain `listAreas`/`maxPeriod` sub-endpoints; use shared `/lookups/geographies`
3. `areaTypeVersion` is part of every geography key (new in WID 3.0)
4. CES is a single endpoint (not split employment/hours-earnings as in Oregon 2.8)
5. `SeriesCodeType` is exposed alongside `SeriesCode` on CES
6. Suppress fields are strings `'0'`/`'1'`, not booleans
7. Standard response envelope: `{ meta, data, links }` with cursor+page pagination

---

## Technical Architecture

### Tech Stack
- **Language/Runtime:** .NET 8, C# — ASP.NET Core Web API
  (matches existing ULMITA identity API at `../fed-ulmita-id`)
- **Hosting:** AWS Lambda (function URL or API Gateway HTTP API)
- **IaC:** AWS SAM + CloudFormation — matching the deployment pattern in `../fed-ulmita-id`
- **Database:** Amazon Aurora Serverless v2 (PostgreSQL 16) — WID 3.0 is a relational schema
- **Scheduler:** Amazon EventBridge Scheduler → Lambda for BLS data ingestion
- **Downloads:** S3 (pre-generated CSV/XLSX) or streaming from Lambda
- **Secrets:** AWS Systems Manager Parameter Store

### Project structure to create
```
fed-national-wid/
├── AGENTS.md                     (this file)
├── README.md
├── justfile                      (task runner — matches fed-ulmita-id pattern)
├── nuget.config
├── NationalWid.Api.slnx
├── src/
│   ├── NationalWid.Api/          (ASP.NET Core Lambda project)
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── Controllers/
│   │   │   ├── CesController.cs
│   │   │   ├── LaborForceController.cs
│   │   │   ├── IndustryController.cs
│   │   │   ├── WagesController.cs
│   │   │   ├── ProjectionsController.cs
│   │   │   ├── LookupController.cs
│   │   │   └── HealthController.cs
│   │   ├── Models/               (WID 3.0 entity models)
│   │   ├── Services/             (data access, BLS ingestion)
│   │   └── Auth/                 (JWT/Cognito helpers)
│   └── NationalWid.Ingestion/    (separate Lambda for BLS data ingestion)
│       ├── Function.cs
│       └── Ingestors/
│           ├── BlsLausIngestor.cs
│           ├── BlsCesIngestor.cs
│           ├── BlsQcewIngestor.cs
│           └── BlsOesIngestor.cs
├── cloud-deployment/
│   ├── lambda.template           (SAM template: API + Ingestion Lambdas + Aurora)
│   ├── dev.deployment-profile.jsonc
│   ├── prod.deployment-profile.jsonc
│   └── migrations/               (SQL migration scripts for Aurora)
├── dev-scripts/
│   ├── Deploy.ps1
│   └── Tail-Logs.ps1
└── tests/
    ├── NationalWid.Api.Tests/
    └── NationalWid.Ingestion.Tests/
```

---

## BLS Data Ingestion Details

### BLS Public API v2
- Base URL: `https://api.bls.gov/publicAPI/v2/timeseries/data/`
- Registration key preferred for higher rate limits (store in Parameter Store)
- Supports up to 50 series per request, 20 years of history

### Series to ingest (national-level stFips = '00' or '00000')
| Dataset | Series example | WID table |
|---|---|---|
| LAUS (national) | `LNS14000000` (unemployment rate) | `LaborForce` |
| CES (national) | `CES0000000001` (total nonfarm) | `CES` |
| QCEW (quarterly) | CEW API: `https://data.bls.gov/cew/data/api/` | `Industry` |
| OES/OEWS | BLS OES flat files (annual) | `IOWage` |
| Employment Projections | ETA Projections data | `ProjectionsMatrix` |

### Ingestion Lambda behavior
- Triggered by EventBridge Scheduler (monthly for most datasets, quarterly for QCEW)
- Idempotent upsert into Aurora PostgreSQL
- Writes ingestion log to `IngestLog` table (admin/operational table)
- Respects BLS rate limits (10 requests/second, 500/day without key)
- Stores BLS API registration key in Parameter Store: `/wid-api/bls-api-key`

---

## Download Formats

All data endpoints support an `Accept` header or `?format=` query param:
- `application/json` (default) — standard envelope
- `text/csv` — flat CSV download
- `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` — XLSX download
  (use ClosedXML or EPPlus NuGet package)

For large datasets, streaming response is preferred over buffering.

---

## CloudFormation / SAM Template Requirements

The `cloud-deployment/lambda.template` must define:
1. **Aurora Serverless v2 cluster** (PostgreSQL 16) — private subnets, no public access
2. **NationalWidApi Lambda** — .NET 8, 1024MB, 30s timeout, VPC-attached
3. **NationalWidIngestion Lambda** — .NET 8, 2048MB, 15min timeout, VPC-attached
4. **API Gateway HTTP API** — with Cognito authorizer (ULMITA user pool)
5. **EventBridge Scheduler** — monthly trigger for ingestion Lambda
6. **Parameter Store parameters** for secrets (DB connection string, BLS key)
7. **VPC config** — reuse existing VPC from deployment profile

### Deployment profile parameters to include
```json
{
  "appCognitoUserPoolId": "us-gov-west-1_OGMbjPhYh",
  "appCognitoClientId": "1151pibjvfgecq8d761drjcmu8",
  "vpcId": "[REDACTED-LIVE-VPC-ID]",
  "vpcSubnetIds": "[REDACTED-LIVE-SUBNET-IDS]",
  "vpcSecurityGroupIds": "[REDACTED-LIVE-SECURITY-GROUP-ID]",
  "appAuroraDbName": "nationalwid",
  "appAuroraMinCapacity": 0.5,
  "appAuroraMaxCapacity": 8
}
```

---

## Key Conventions (match fed-ulmita-id)

1. **justfile** for all tasks: `just build`, `just run`, `just test`, `just deploy dev`
2. **Deployment** via `dotnet lambda deploy-serverless` or `sam deploy`
3. **PowerShell scripts** in `dev-scripts/`
4. **Tags**: `tagElcid: wsitapro`, `tagDept: dws`, `tagDivision: wra`, 
   `tagContact: MattSteadman@utah.gov`
5. **No hardcoded secrets** — always Parameter Store
6. **Health endpoint** `GET /health` returns `200 OK { "status": "healthy" }` — unauthenticated

---

## API Response Envelope

All list responses use:
```json
{
  "meta": {
    "total": 1500,
    "page": 1,
    "pageSize": 100,
    "nextCursor": "eyJzdEZpcHMiOi..."
  },
  "data": [...],
  "links": {
    "self": "/ces?stFips=00&page=1",
    "next": "/ces?stFips=00&cursor=eyJzdEZpcHMiOi...",
    "geographies": "/lookups/geographies?stFips=00"
  }
}
```

---

## Immediate Build Goals (Priority Order)

1. **Project scaffold** — solution, projects, justfile, nuget.config
2. **WID 3.0 data models** — C# records for all core tables
3. **Database schema** — PostgreSQL migration SQL matching WID 3.0 structure
4. **API project** — controllers, Cognito JWT auth, response envelope
5. **Cloud deployment template** — Aurora + Lambda + API Gateway + Cognito authorizer
6. **BLS ingestion Lambda** — at minimum LAUS and CES national series
7. **Download formats** — CSV and XLSX export
8. **Deployment profiles** — dev and prod

Build as much of this as possible in a single coherent pass. 
Prioritize correctness and completeness over perfection.

---

## Cross-Repo API Contract Governance (fed-national-wid <-> widapi)

The API implementation repository (`fed-national-wid`) and the specification repository (`widapi`) must remain synchronized.

### Source-of-truth hierarchy
1. `widapi` is the source of truth for API contract behavior and documentation.
2. `fed-national-wid` must implement the contract defined in `widapi/specs/wid-3.0/draft.yaml`.
3. If there is any conflict between implementation and spec/docs, resolve the conflict in favor of `widapi` and then update implementation.

### Canonical contract rule
1. Any behavioral or contract change in `fed-national-wid` API routes, query parameters, filtering, sorting, pagination, auth model, response envelope, or field names/types MUST be documented in `widapi`.
2. In the ideal state, generated OpenAPI/Swagger from `fed-national-wid` is identical in API contract semantics to `widapi/specs/wid-3.0/draft.yaml` (and therefore `draft.json`).

### Required workflow for API changes
1. Implement API change in `fed-national-wid`.
2. Update `widapi/specs/wid-3.0/draft.yaml` in the same workstream.
3. Add/update supporting rationale in `widapi/docs/decisions/` when the change is non-trivial.
4. Ensure examples and parameter descriptions in `widapi` reflect actual implementation behavior.
5. Do not treat API work as complete until spec/docs parity is addressed.

### Drift policy
1. If implementation temporarily diverges from `widapi`, record the drift explicitly in both repos and create a follow-up task to close the gap.
2. Prefer spec-first or same-PR synchronization over deferred backfill.

