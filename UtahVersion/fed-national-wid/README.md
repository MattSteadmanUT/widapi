# National WID 3.0 API

A nationally-hosted REST API providing state workforce agencies with standardized, programmatic access to Bureau of Labor Statistics (BLS) labor market data aligned with the **WID 3.0** database specification.

Built and operated by the Utah Department of Workforce Services (DWS) as part of the ULMITA platform. State users authenticate via the ULMITA Cognito user pool or with long-lived [API keys](#api-keys).

> **Handed off from Utah DWS to North Carolina.** Start with
> [`../HANDOFF.md`](../HANDOFF.md) for current status, known issues, and an infrastructure/access
> transition checklist. This README is the API's own reference documentation and is still
> accurate; the `docs/` folder below has deep dives into architecture, the ingestion pipeline,
> the database schema, deployment/operations, testing, and known gaps.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Quick Start (Developer)](#quick-start-developer)
3. [Live Environments](#live-environments)
4. [Authentication](#authentication)
5. [API Key Authentication](#api-keys)
6. [Data Coverage](#data-coverage)
7. [API Reference](#api-reference)
8. [Response Format](#response-format)
9. [Filtering & Sorting](#filtering--sorting)
10. [Download / Export](#download--export)
11. [Ingestion Pipeline](#ingestion-pipeline)
12. [Running Migrations](#running-migrations)
13. [Angular Component Library](#angular-component-library)
14. [Contributing](#contributing)

---

## Architecture Overview

```
API Gateway (HTTP API, payload v2)
    │
    ├─ JWT Bearer (Cognito)  ─┐
    └─ ApiKey header          ├─ Auth
                              │
    Lambda (NationalWid.Api)  ─── EF Core ─── Aurora PostgreSQL
                              │
    EventBridge Scheduler ────┘
    └─ Lambda (NationalWid.Ingestion)
           ├─ BLS LAUS (flat files)
           ├─ BLS CES (flat files + API)
           ├─ BLS QCEW (ZIP → CSV)
           ├─ BLS OEWS (Excel workbooks)
           ├─ BLS Employment Projections (flat files)
           ├─ BLS CPI-U (flat files)  ← new
           └─ WID Center Lookups (HTTP)
```

**Runtime:** .NET 8 on AWS Lambda (GovCloud `us-gov-west-1`)  
**Database:** Amazon Aurora PostgreSQL Serverless v2  
**Auth:** AWS Cognito (JWT) + application-layer API keys  
**Migrations:** Sequential SQL scripts in `cloud-deployment/migrations/`  

---

## Quick Start (Developer)

### Prerequisites

| Tool | Notes |
|---|---|
| .NET 8 SDK | `dotnet --version` |
| AWS CLI | Profiles `GovDev` / `GovProd` configured |
| AWS SAM CLI | For local Lambda emulation |
| PowerShell 7+ | `pwsh --version` |
| [just](https://github.com/casey/just) | Task runner |

### Common Tasks

| Task | Command |
|---|---|
| Build | `just build` |
| Run tests | `just test` |
| Deploy to dev | `just deploy-dev` |
| Run migrations | `just migrate-dev` |
| Verify schema | `just verify-schema-dev` |
| Trigger ingestion | `just ingest-dev` |
| Tail ingestion logs | `just tail-ingestion-dev` |

Or invoke the scripts directly:

```powershell
# Deploy
./dev-scripts/Deploy.ps1 ./cloud-deployment/dev.deployment-profile.jsonc

# Run SQL migrations (idempotent)
./dev-scripts/Run-Migrations.ps1

# Tail Lambda logs
./dev-scripts/Tail-Logs.ps1 -FunctionName dev-national-wid-ingestion
```

---

## Live Environments

| | Dev |
|---|---|
| **API base URL** | `https://uk6y6uz1il.execute-api.us-gov-west-1.amazonaws.com/` |
| **Stack** | `dev-national-wid-api` |
| **Region** | `us-gov-west-1` |
| **AWS Profile** | `GovDev` |

`GET /health` is unauthenticated. All other endpoints require authentication — either a Cognito JWT or an API key (see below).

---

## Authentication

### JWT bearer token (interactive users)

All endpoints require a valid OIDC access token, **except** `/health`, `/status` (and its
`/status/history`, `/status/coverage` variants), and — worth calling out explicitly since it's easy
to miss — all four `/cpi*` endpoints (`GET /cpi`, `/cpi/metadata`, `/cpi/items`, `/cpi/areas`),
which carry `[AllowAnonymous]` at the method level despite the controller also declaring
`[Authorize]`. `[AllowAnonymous]` wins, so **CPI data is fully public with no authentication
required at all**, unlike every other dataset in this API. Nothing in the code or Utah's prior
docs explains whether this was a deliberate call to expose price data more broadly than labor
market data, or an oversight — worth confirming intent with the original author before assuming
either way (see [known-issues-and-gaps.md](docs/known-issues-and-gaps.md)).

```http
Authorization: Bearer <access-token>
```

Deployed against Utah's ULMITA Cognito user pool today (dev settings below), but the API isn't
tied to Cognito specifically — it's standard ASP.NET Core JWT/OIDC validation. Set `Oidc:Authority`
in configuration to point at any other OIDC-compliant identity provider instead (Auth0, Okta,
Azure AD B2C, Keycloak, a different Cognito pool, etc.) with no code change. See
[docs/architecture.md](docs/architecture.md#auth-dual-scheme-jwt-by-default-api-key-as-a-fallback)
and [docs/deployment-and-operations.md](docs/deployment-and-operations.md#platform-portability--whats-aws-specific-vs-portable)
for the full picture.

| Setting | Dev |
|---|---|
| User Pool ID | `us-gov-west-1_OGMbjPhYh` |
| Client ID | `1151pibjvfgecq8d761drjcmu8` |
| Region | `us-gov-west-1` |

Any authenticated caller — JWT or API key — can query any state's data; this is a nationally-hosted
public-data API, so there's no per-state access restriction by design.

---

## API Keys

For external systems and automation (CI/CD pipelines, data integrations, scheduled jobs), users can create long-lived API keys that don't require the interactive Cognito flow.

### Using an API Key

```http
GET /labor-force?stFips=49
Authorization: ApiKey nwid_abc12345...
```

### Managing Keys

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api-keys` | Create a new key (plaintext returned once) |
| `GET` | `/api-keys` | List all keys for the authenticated user |
| `GET` | `/api-keys/{keyId}` | Get metadata for a specific key |
| `PATCH` | `/api-keys/{keyId}` | Update description, status, or expiry |
| `DELETE` | `/api-keys/{keyId}` | Permanently revoke a key |

**Create a key:**
```json
POST /api-keys
{
  "description": "Data pipeline integration",
  "expiresAt": "2027-01-01T00:00:00Z",
  "rateProfile": "standard"
}
```

**Response (shown once — store the `plaintextKey` securely):**
```json
{
  "keyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "plaintextKey": "nwid_abc12345xyz...",
  "keyPrefix": "nwid_abc",
  "status": "active",
  "createdAt": "2026-01-01T00:00:00Z",
  "expiresAt": "2027-01-01T00:00:00Z",
  "rateProfile": "standard"
}
```

Key security properties:
- Only the SHA-256 hash is stored — the plaintext key is returned **once** and never recoverable
- Revoking a key (`DELETE`) is permanent; re-activate with a new key if needed
- Disabled keys (`PATCH` status → `disabled`) can be re-enabled (`disabled → active`)

---

## Data Coverage

See [docs/table-data-audit.md](docs/table-data-audit.md) for full per-table coverage details.

### Core Tables

| Table | Endpoint | Source | Status |
|---|---|---|---|
| LaborForce | `GET /labor-force` | BLS LAUS flat files | ✅ Ingested |
| CES | `GET /ces` | BLS CES flat files | ✅ Ingested |
| Industry | `GET /industry` | BLS QCEW CSV | ✅ Ingested |
| IOWage | `GET /wages` | BLS OEWS Excel | ✅ Ingested |
| ProjectionsMatrix | `GET /projections` | BLS Projections flat files | ✅ Ingested |
| License | `GET /licensing/licenses` | WID Center `COSFlatExport` + per-state WID 2.8 license MDB exports | ✅ Ingested (`licenseUpdatedDate` parsed from source stamp) |
| LicenseAuthorities | `GET /licensing/authorities` | Same as License | ✅ Ingested |
| LicenseHistory | `GET /licensing/history` | Same as License | ⚠ Empty in current source feeds |
| LicenseXOcc | `GET /licensing/occupationCrosswalks` | Same as License | ✅ Ingested |

Licensing history remains source-constrained; see [docs/table-data-audit.md](docs/table-data-audit.md#why-licensing-tables-are-empty).

The `License` payload also includes a parsed `licenseUpdatedDate` field derived from the source `licenseUpdated` stamp when it is present and valid.

### CPI Tables (new)

| Table | Endpoint | Source | Status |
|---|---|---|---|
| CPI | `GET /cpi` | BLS CPI-U flat files | ✅ Ingestion implemented |
| CpiSeries | `GET /cpi/metadata` | BLS `cu.series` | ✅ Ingestion implemented |
| CpiItems | `GET /cpi/items` | BLS `cu.item` | ✅ Ingestion implemented |
| CpiAreas | `GET /cpi/areas` | BLS `cu.area` | ✅ Ingestion implemented |

### Non-Core Lookup & View Tables

All 14 lookup tables and 6 view tables are fully implemented and populated from ingested data. See `GET /status` for live record counts.

---

## API Reference

### Base URLs

All endpoints below are relative to the API base URL. Trailing slashes are optional.

### `/health`

```http
GET /health
```
Returns `{ "status": "ok" }`. No authentication required.

### `/status`

```http
GET /status                  # Table freshness and record counts
GET /status?report=history   # Ingestion run history
GET /status?report=coverage  # Core table geographic coverage
```

### Core Endpoints

Each core endpoint accepts the following common query parameters (all optional):

| Parameter | Description |
|---|---|
| `stFips` | 2-digit state FIPS code; CSV multi-value (e.g., `49,36`) |
| `areaType` | Area type code (e.g., `ST`, `MSA`) |
| `area` | Area code (supports wildcards: `49*`) |
| `periodYear` | 4-digit year; CSV multi-value |
| `sort` | `sort=field:asc` or `sort=f1:desc,f2:asc` |
| `page` / `pageSize` | Offset pagination |
| `cursor` | Cursor-based pagination token |
| `format` | `csv`, `tsv`, `psv`, `xlsx`, `json` |

```http
GET /labor-force          # BLS LAUS: civilian labor force, employed, unemployed, unemp rate
GET /ces                  # BLS CES: establishment employment by series code
GET /industry             # BLS QCEW: employment and wages by industry
GET /wages                # BLS OEWS: occupational employment and wage statistics
GET /projections          # BLS Employment Projections: 10-year occupation × industry matrix
GET /projections/matrixXInd   # Industry crosswalk for projection matrix
GET /projections/matrixXOcc   # Occupation crosswalk for projection matrix
```

Each core endpoint also has a `/metadata` sub-endpoint returning available filter values:
```http
GET /labor-force/metadata
GET /ces/metadata
# etc.
```

### CPI

```http
GET /cpi                  # CPI observation values
GET /cpi/metadata         # Series definitions with area/item names
GET /cpi/items            # Item code reference (price basket components)
GET /cpi/areas            # Geographic area codes
```

| Parameter | Description |
|---|---|
| `seriesId` | BLS series ID or pattern (e.g., `CUSR*`) |
| `year` | 4-digit year |
| `period` | BLS period code (`M01`–`M13`, `S01`/`S02`) |
| `areaCode` | BLS area code (e.g., `0000` = U.S. city average) |
| `itemCode` | BLS item code (e.g., `SA0` = All items) |
| `seasonalCode` | `S` = seasonally adjusted, `U` = unadjusted |

### Lookups

```http
GET /lookups/geographies
GET /lookups/areaTypes
GET /lookups/stateFips
GET /lookups/periodTypes
GET /lookups/periodYears
GET /lookups/periods
GET /lookups/industryCodes
GET /lookups/occupationCodes
GET /lookups/cesCodes
GET /lookups/ownerships
GET /lookups/wageSources
GET /lookups/wageRateTypes
GET /lookups/benchmarks
GET /lookups/growthCodes
GET /lookups/ind-directories
GET /lookups/occ-directories
```

### Views (Joined / Enriched)

```http
GET /views/cesWithGeography            # CES + area names
GET /views/laborForceWithGeography     # LAUS + area names
GET /views/industryWithGeography       # Industry + area names
GET /views/wagesWithDescriptions       # OEWS + industry and occupation titles
GET /views/projectionsWithTitles       # Projections + titles and growth code labels
GET /views/licensingByOccupation       # License crosswalk (includes licenseUpdatedDate where present)
```

### Licensing

```http
GET /licensing/authorities
GET /licensing/licenses
GET /licensing/history
GET /licensing/occupationCrosswalks
```

---

## Response Format

All paginated endpoints return a standard envelope:

```json
{
  "meta": {
    "total": 12483,
    "page": 1,
    "pageSize": 100,
    "nextCursor": "eyJzdEZpcHMiOiI0OSJ9"
  },
  "data": [ ... ],
  "links": {
    "self": "https://.../labor-force?stFips=49&page=1&pageSize=100",
    "next": "https://.../labor-force?cursor=eyJzdEZpcHMiOiI0OSJ9"
  }
}
```

---

## Filtering & Sorting

### String Filters

All string filter parameters support three modes:

| Pattern | Matches |
|---|---|
| `49` (exact) | Only `"49"` |
| `49,36,06` (CSV) | Any of the listed values |
| `11*` (wildcard) | Starts with `"11"` |
| `*0000` (wildcard) | Ends with `"0000"` |
| `*total*` (wildcard) | Contains `"total"` |

### Sorting

```
?sort=periodYear:desc,stFips:asc
```

Multiple fields separated by commas; each field followed by `:asc` or `:desc`.

---

## Download / Export

Add `?format=<type>` to any data endpoint to download:

| Format | Description |
|---|---|
| `csv` | Comma-separated values |
| `tsv` | Tab-separated values |
| `psv` | Pipe-separated values |
| `xlsx` | Excel workbook |
| `json` | JSON file download |

Example:
```http
GET /labor-force?stFips=49&periodYear=2024&format=xlsx
```

---

## Ingestion Pipeline

The ingestion Lambda runs monthly via EventBridge Scheduler. It downloads and upserts:

| Dataset | Source | Schedule |
|---|---|---|
| LAUS | BLS flat files (`la.data.*`) | Monthly |
| CES | BLS flat files (`ce.data.*`, `sm.data.55`) | Monthly |
| Industry (QCEW) | BLS CEW ZIP files | Monthly |
| OEWS | BLS OEWS Excel workbooks | Annual (May release) |
| Employment Projections | BLS flat files | Biennial |
| CPI-U | BLS flat files (`cu.data.*`) | Monthly |
| WID Center Lookups | WID Center HTTP | Monthly |

### Forcing a Re-run

Invoke the ingestion Lambda with a payload:

```json
{
  "forceRefreshDatasets": "LAUS,CES"
}
```

Available options:
- `RunMigrationsOnly: true` — run schema migrations only, no data ingestion
- `SkipMigrations: true` — skip schema migrations
- `SkipHeavyDatasets: true` — skip QCEW, OEWS, Projections (for quick LAUS/CES updates)
- `VerifyOnly: true` — verify schema without running migrations or ingestion

---

## Running Migrations

Migrations are sequential SQL scripts in `cloud-deployment/migrations/`. They are **idempotent** — each is applied only once, tracked by filename in the `migrations` table.

```powershell
./dev-scripts/Run-Migrations.ps1                          # dev
./dev-scripts/Run-Migrations.ps1 -Profile GovProd         # prod
```

Current migrations (017 total):

| # | File | Description |
|---|---|---|
| 001 | `001_create_schema.sql` | Core WID 3.0 tables |
| 002 | `002_create_indexes.sql` | Composite indexes |
| 003 | `003_seed_lookups.sql` | Reference data seeds |
| 004 | `004_extend_wid30_schema.sql` | Extended WID 3.0 fields |
| 005 | `005_add_lookup_reference_tables.sql` | Lookup reference tables |
| 006 | `006_ingestlog_activity_fields.sql` | Ingest log activity tracking |
| 007 | `007_fix_geography_area_codes.sql` | Area code normalization |
| 008 | `008_seed_state_periodyears.sql` | State period years seed |
| 009 | `009_fix_national_area_codes.sql` | National area code fix |
| 010 | `010_revert_to_char6_area.sql` | Area code type fix |
| 011 | `011_cleanup_area_codes.sql` | Area code cleanup |
| 012 | `012_schema_compliance.sql` | WID 3.0 schema compliance |
| 013 | `013_add_api_keys.sql` | User-managed API keys |
| 014 | `014_add_cpi_tables.sql` | CPI data tables |
| 015 | `015_license_data.sql` | Bulk license data seed (all 57 states/territories) |
| 016 | `016_license_flags_national.sql` | License flag data correction (all 56 states) |
| 017 | `017_widen_cpi_baseyear.sql` | Widen `cpiseries.baseyear` to fit real BLS base-period strings |

009 through 011 are a three-migration sequence fixing two different area-code bugs in succession
— see [docs/database-schema.md](docs/database-schema.md) for why, and for migration 012's
explicitly-documented deviations from the WID 3.0 spec.

---

## Angular Component Library

The National WID data explorer is packaged as a reusable Angular library: **`@ulmita/ng-national-wid`**.

The built library is at `dist/ng-national-wid/` in the `fed-ulmita-ng` repository. To publish it to a private npm registry:

```bash
cd dist/ng-national-wid
npm publish --registry https://your-registry.example.gov/
```

### Integration

1. **Install** (after publishing to your registry):
   ```bash
   npm install @ulmita/ng-national-wid
   ```

2. **Add to your AppModule:**
   ```typescript
   import { NationalWidModule, provideNationalWid } from '@ulmita/ng-national-wid';

   @NgModule({
     imports: [NationalWidModule],
     providers: [
       provideNationalWid({
         apiBaseUrl: 'https://your-wid-api.example.gov/',
         getToken: () => yourAuthService.getAccessToken()
       })
     ]
   })
   export class AppModule {}
   ```

3. **Use the component:**
   ```html
   <nwid-national-wid (navigateBack)="router.navigate(['/dashboard'])">
   </nwid-national-wid>
   ```

4. **CSS dependencies** (add to `index.html`):
   ```html
   <link rel="stylesheet" href="https://www.w3schools.com/w3css/4/w3.css">
   <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.0/css/all.min.css">
   ```

### Using an API Key Instead of a Bearer Token

```typescript
provideNationalWid({
  apiBaseUrl: 'https://your-wid-api.example.gov/',
  getToken: async () => `ApiKey ${environment.nationalWidApiKey}`
})
```

---

## Contributing

1. Branch from `dev` (e.g., `feature/my-change`)
2. Make changes in `src/NationalWid.Api/` or `src/NationalWid.Ingestion/`
3. Add a migration in `cloud-deployment/migrations/` if schema changes are needed
4. Run `dotnet build` and `dotnet test` before pushing
5. Open a PR from your branch to `dev`

### Project Structure

```
src/
  NationalWid.Api/
    Controllers/          # HTTP endpoints
    Models/               # EF entities + request/response records
    Data/WIDDbContext.cs  # EF Core context
    Services/             # Business logic (WidCodes, ApiKeyService, etc.)
    Auth/                 # API key auth handler
    Program.cs            # DI registration and middleware
  NationalWid.Ingestion/
    Ingestors/            # One class per data source
    Services/             # Shared utilities (BlsFlatFileService, etc.)
    Function.cs           # Lambda entry point
cloud-deployment/
  migrations/             # SQL migration scripts (001–017)
  *.jsonc                 # SAM deployment profiles
dev-scripts/              # PowerShell helpers
tests/                    # xUnit tests
docs/                     # Architecture, ingestion pipeline, schema, deployment, testing, and known-issues deep dives — see below
```

### Documentation

| Doc | Covers |
|---|---|
| [docs/architecture.md](docs/architecture.md) | The shared query/filter/sort/pagination/download engine and dual auth model — read before touching controllers or the query pipeline |
| [docs/ingestion-pipeline.md](docs/ingestion-pipeline.md) | Every ingestor's source, quirks, scheduling, and idempotency mechanism |
| [docs/database-schema.md](docs/database-schema.md) | Full table inventory and migration-by-migration history, including corrective migrations |
| [docs/deployment-and-operations.md](docs/deployment-and-operations.md) | AWS resources, `justfile` tasks, deployment profile contents, runbook |
| [docs/first-deployment-playbook.md](docs/first-deployment-playbook.md) | The ordered, concrete runbook from an empty AWS account to a populated dev environment |
| [docs/iam-deployer-policy.md](docs/iam-deployer-policy.md) | A starting-point least-privilege IAM policy for whoever runs `just deploy` |
| [docs/spec-contract-drift.md](docs/spec-contract-drift.md) | Where this API and the WID 3.0 spec disagree — read before trusting the spec as ground truth |
| [docs/testing.md](docs/testing.md) | What's covered by automated tests and what isn't |
| [docs/known-issues-and-gaps.md](docs/known-issues-and-gaps.md) | The honest "what's not done" list |
| [docs/table-data-audit.md](docs/table-data-audit.md) | Per-table data coverage audit |
| [docs/table-classification.md](docs/table-classification.md) | Core vs. non-core table classification rationale |

For overall handoff status, start at [`../HANDOFF.md`](../HANDOFF.md) instead.