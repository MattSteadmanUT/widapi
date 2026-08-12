# National WID API - Fable 5 Handoff (2026-06-11)

## Purpose
This document captures the current implementation state, verified runtime behavior, and the next high-value execution plan for Claude Fable 5.

## Executive Summary
- Core .NET 8 API + ingestion Lambdas are scaffolded, deployed, and running in GovDev.
- CloudFormation stack exists and is healthy (`CREATE_COMPLETE`).
- API baseline checks passed (`/health` works; protected routes challenge without token).
- The original DB connection-string parsing issue has been fixed.
- Current ingestion failures are now primarily schema/data readiness issues (missing relations), plus unresolved QCEW source mapping.

## Live Environment Snapshot
- AWS profile: `GovDev`
- Region: `us-gov-west-1`
- Stack: `dev-national-wid-api`
- Stack status note: a recent deploy attempt briefly showed `ROLLBACK_IN_PROGRESS`, then the stack returned to `CREATE_COMPLETE`.
- API URL: `https://uk6y6uz1il.execute-api.us-gov-west-1.amazonaws.com`
- Aurora endpoint: `dev-national-wid.cluster-cxveh3zajems.us-gov-west-1.rds.amazonaws.com`
- Ingestion function: `dev-national-wid-ingestion`

## Deployment State Caveat
- Current stack state is healthy: `CREATE_COMPLETE`.
- If an update fails, prefer these checks instead of relying on a single waiter:
   1. `aws cloudformation describe-stacks ... --query "Stacks[0].StackStatus"`
   2. `aws cloudformation describe-stack-events ...` to identify the failing logical resource and reason.
- Note: `stack-rollback-complete` waiters can be misleading depending on whether the operation is create rollback vs update rollback; always confirm final status explicitly with `describe-stacks`.

## What Is Confirmed Working
1. Deployment target is functional:
   - `aws cloudformation describe-stacks` reports `CREATE_COMPLETE`.
2. Lambda runtime and networking basics are functional:
   - Ingestion function executes and reaches BLS calls.
3. Password parsing regression is fixed:
   - Errors changed from Npgsql parse/connect-string issues to downstream DB relation/data issues.
4. Security group ingress for PostgreSQL was added for the shared SG.
5. VPC DNS hostnames were enabled.

## What Is Currently Broken
1. Database schema is not fully present at runtime:
   - Ingestion logs show `42P01: relation "laborforce" does not exist`.
   - Ingestion logs show `42P01: relation "ces" does not exist`.
2. QCEW ingestion path does not currently yield data:
   - Repeated `NotFound` across year/quarter requests in current logic.
3. No reliable evidence yet that migrations were applied to Aurora in dev.

## Most Likely Root Cause Chain
1. Infrastructure stack deployment succeeded.
2. Aurora connectivity now succeeds enough to execute SQL.
3. SQL fails because required tables/indexes/seeds were never applied (or applied to a different DB/cluster).
4. QCEW endpoint logic likely mismatches current BLS API contract for the selected years/route pattern.

## Artifacts and Files That Matter Most
- Infrastructure template:
  - `cloud-deployment/lambda.template`
- Schema and seed scripts:
  - `cloud-deployment/migrations/001_create_schema.sql`
  - `cloud-deployment/migrations/002_create_indexes.sql`
  - `cloud-deployment/migrations/003_seed_lookups.sql`
- Ingestion orchestration:
  - `src/NationalWid.Ingestion/Function.cs`
- Ingestors:
  - `src/NationalWid.Ingestion/Ingestors/BlsLausIngestor.cs`
  - `src/NationalWid.Ingestion/Ingestors/BlsCesIngestor.cs`
  - `src/NationalWid.Ingestion/Ingestors/BlsQcewIngestor.cs`
  - `src/NationalWid.Ingestion/Ingestors/BlsOesIngestor.cs`

## What Fable 5 Should Do Next (Priority Order)

### Phase 1 - Make DB State Deterministic (Highest Priority)
1. Implement an idempotent migration runner and execute it in dev:
   - Apply `001_create_schema.sql`, then `002_create_indexes.sql`, then `003_seed_lookups.sql`.
   - Record migration runs in a migration history table (for repeat safety and auditability).
2. Add a deployment hook or explicit script target so schema application is part of normal deploy workflow.
3. Add a post-migration verification step:
   - Assert existence of `laborforce`, `ces`, `industry`, `iowage`, `projectionsmatrix`, lookup tables, and `ingestlog`.

Acceptance criteria:
- `SELECT to_regclass('public.laborforce')` and `SELECT to_regclass('public.ces')` return table names.
- Ingestion no longer throws `42P01` relation-not-found errors.

### Phase 2 - Stabilize Ingestion Contract and Data Flow
1. LAUS/CES/OES:
   - Confirm series mapping, period parsing, and upsert key alignment with WID PK definitions.
   - Ensure ingestion result counts are accurate and non-negative for successful writes.
2. QCEW:
   - Rework source URL and parsing logic against the current BLS QCEW API contract.
   - Add graceful handling for unavailable quarter/year datasets without marking the entire dataset as failed.
3. Improve observability:
   - Add structured logging for source URL, request status, row counts, and SQL write counts.
   - Insert per-dataset status rows into `ingestlog`.

Acceptance criteria:
- A manual invocation writes rows to at least LAUS and CES.
- Ingestion payload reports non-negative row counts for successful datasets.
- Logs clearly distinguish "source unavailable" vs "pipeline failure".

### Phase 3 - Validate Data API Surface Against WID 3.0 Shape
1. Validate API controllers against actual migrated schema and lookup dependencies.
2. Ensure endpoints return expected envelope (`meta`, `data`, `links`).
3. Add a minimal smoke test suite for:
   - `/health` unauthenticated
   - one protected endpoint with token
   - one lookup endpoint
   - one data endpoint after ingestion

Acceptance criteria:
- Post-ingestion API queries return real records for national scope (`stfips='00'` path where applicable).
- Smoke tests pass in dev.

### Phase 4 - Guardrails and Regression Prevention
1. Add CI checks:
   - Build + tests
   - migration lint/basic SQL validation
2. Add runbook docs for:
   - credential rotation
   - migration execution
   - ingestion verification
3. Ensure IaC reflects all runtime-critical network/DB prerequisites to avoid manual drift.

Acceptance criteria:
- Fresh environment bootstrap can be repeated without ad hoc manual fixes.

## Suggested Fable 5 Prompt Seed
Use this as a starting instruction to Fable 5:

"Continue implementation of National WID API in `fed-national-wid` using .NET 8 and existing architecture. First make Aurora schema state deterministic by implementing and running idempotent migrations from `cloud-deployment/migrations/001_create_schema.sql`, `002_create_indexes.sql`, and `003_seed_lookups.sql`. Verify required tables exist (`laborforce`, `ces`, etc.), then fix ingestion so LAUS/CES write rows successfully in dev. Keep QCEW logic resilient (no hard fail when source quarter is unavailable), add structured ingest logging, and produce evidence commands/results for each validation step. Do not change auth model or move off GovDev/GovProd profile usage." 

## Non-Goals for This Next Pass
- Do not redesign the overall API contract.
- Do not replace Cognito auth approach.
- Do not switch off .NET 8.
- Do not introduce broad architecture pivots until ingestion + schema baseline is stable.

## Fast Verification Checklist (after Fable 5 changes)
1. Deploy stack/function updates in dev.
2. Execute migrations and verify table existence.
3. Invoke ingestion lambda once manually.
4. Confirm LAUS/CES row writes and no relation-not-found errors.
5. Query one protected endpoint for non-empty data.
6. Capture logs and output payload as proof artifacts.

## Final Status Call
- Password parsing issue: fixed.
- Runtime blocking issue now: schema/migration/data readiness and QCEW mapping.
- Best next use of Fable 5: end-to-end migration runner + ingestion hardening + verifiable data write proof.

---

## Session Update (2026-06-11, later pass)

### What Was Implemented
1. Idempotent migration runner added to ingestion code:
   - `src/NationalWid.Ingestion/Services/MigrationService.cs`
   - Applies `001_create_schema.sql`, `002_create_indexes.sql`, `003_seed_lookups.sql` in order.
   - Tracks applied scripts in `wid_migration_history`.
   - Verifies required tables using `to_regclass` checks.
2. Ingestion Lambda flow updated:
   - `src/NationalWid.Ingestion/Function.cs`
   - Supports migration/verification request modes (`runMigrationsOnly`, `verifyOnly`, `skipMigrations`).
   - Adds per-dataset ingest log writes via `src/NationalWid.Ingestion/Services/IngestLogService.cs`.
   - Fixes summary error propagation (`Errors` now returned correctly).
3. Dev workflow additions:
   - `dev-scripts/Run-Migrations.ps1` for migration/verification invocation.
   - `justfile` targets: `migrate-dev`, `verify-schema-dev`.
4. Ingestion hardening fixes:
   - LAUS row aggregation corrected (one row per period; avoids PK conflicts).
   - QCEW quarter loop fixed for current-year handling.
   - QCEW logs distinguish source unavailable vs request failure.
   - OES SOC extraction corrected from series IDs.
   - CES upsert now refreshes suppression/derived metric fields.
   - Ingest-log writes now use bounded timeout tokens.
5. Build/deploy script fix:
   - `dev-scripts/Deploy.ps1` template parameter serialization changed to `;`-joined key/value string, allowing successful stack update.

### Validation Evidence
1. Local build/tests:
   - `dotnet build` succeeded.
   - `dotnet test` succeeded.
2. Dev deployment:
   - Stack update completed: `UPDATE_COMPLETE`.
   - Updated ingestion package includes migration SQL files.

### New Runtime Blocker
Migration-mode Lambda invocation now fails against Aurora with:
- `PostgresException 28P01: password authentication failed for user "widadmin"`

This indicates a live environment credential mismatch (Parameter Store connection string vs Aurora user password), not a migration-runner code issue.

### Recommended Next Actions
1. Validate `/wid-api/db-connection-string` in Parameter Store against current Aurora credentials.
2. If needed, rotate/reset DB user password and update Parameter Store atomically.
3. Re-run:
   - `just migrate-dev`
   - `just verify-schema-dev`
4. After auth is restored, run ingestion once and confirm LAUS/CES writes and `ingestlog` rows.
