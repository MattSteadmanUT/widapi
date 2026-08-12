# First Deployment Playbook

This is the missing piece the rest of the docs assume you already know: the exact, ordered
sequence of steps to take this project from "nothing exists in NC's AWS account" to "a working dev
environment serving real BLS data." Every other doc in this folder explains a *part* of the system;
this one is the runbook that ties them together for the first attempt.

It was written by walking through the codebase as if standing up the system for the first time —
several of the steps below exist because that walkthrough found and fixed real bugs that would
have blocked a from-scratch deploy (see the callouts). Read [known-issues-and-gaps.md](./known-issues-and-gaps.md)
alongside this for anything that's a known limitation rather than a step to perform.

## Before you start: decisions to lock in

Don't start deploying until these are answered — they change what you provision:

1. **Identity provider** — continuing to use Utah's ULMITA Cognito pool, standing up NC's own
   Cognito pool, or using a different OIDC provider entirely. See
   [HANDOFF.md](../../HANDOFF.md#infrastructure--access-transition-checklist). This determines
   whether you need to provision Cognito at all in step 2 below.
2. **Cloud platform** — AWS is the lowest-effort path (it's what's built and proven); a different
   cloud is possible but means writing new IaC and a new ingestion-Lambda-equivalent entry point
   first (see [deployment-and-operations.md](./deployment-and-operations.md#platform-portability--whats-aws-specific-vs-portable)).
   This playbook assumes AWS.
3. **GovCloud vs. commercial AWS** — Utah deployed to `us-gov-west-1` because that's where the rest
   of ULMITA lives, not because anything in this codebase requires GovCloud specifically. Nothing
   in the SAM template, the .NET code, or the AWS services used (Lambda, Aurora, API Gateway,
   EventBridge Scheduler, SSM, Secrets Manager) is GovCloud-exclusive. **This is a decision for
   NC's own security/compliance team to make**, not something this document can answer — if NC's
   state-sponsored AWS account is commercial rather than GovCloud, that should work with no code
   changes, only different `region`/`profile` values in the deployment profile.
4. **Dev before prod, always.** Utah never actually completed a production deployment —
   `prod.deployment-profile.jsonc` still has unfilled `TODO_SET_PROD_*` placeholders. Treat prod as
   genuinely new ground, not a repeat of a proven dev process.

## Step 0: Local prerequisites

- .NET 8 SDK, PowerShell 7+, [`just`](https://github.com/casey/just), AWS CLI, AWS SAM CLI.
- The `Amazon.Lambda.Tools` global dotnet tool, since `Deploy.ps1` calls
  `dotnet lambda deploy-serverless` — install with `dotnet tool install -g Amazon.Lambda.Tools` if
  not already present (this isn't installed by anything in the repo itself).
- An AWS CLI profile with credentials for NC's account, with permissions to create VPCs (or use an
  existing one), Aurora clusters, Lambda functions, IAM roles, API Gateway, EventBridge schedules,
  Secrets Manager secrets, and SSM parameters.
- `dotnet restore && just build && just test` should succeed with zero network dependencies beyond
  `nuget.org` (confirmed — `nuget.config` has no private feed).

## Step 1: Network prerequisites

The SAM template (`cloud-deployment/lambda.template`) takes `vpcId`, `vpcSubnetIds`, and
`vpcSecurityGroupIds` as **input parameters** — it does not create a VPC itself. Utah's dev profile
points at ULMITA's existing shared VPC. NC needs an existing (or newly created) VPC with:
- Private subnets for the Aurora cluster and both VPC-attached Lambdas.
- A security group allowing the Lambdas to reach Aurora on 5432, and outbound HTTPS (443) for the
  ingestion Lambda's calls to `download.bls.gov`, `api.bls.gov`, `data.bls.gov`,
  `public.projectionscentral.org`, and `data.widcenter.org`.

## Step 2: Identity provider

If continuing with Utah's ULMITA pool: get the pool ID and client ID from Utah for NC's target
environment. If standing up NC's own Cognito pool (or another OIDC provider): create it, and note
there is **no companion user-management app included in this handoff** — Utah's `AGENTS.md`
referenced a sibling `fed-ulmita-id` identity API that isn't part of this repository. NC will need
either its own equivalent, or to provision users directly via AWS Console/CLI
(`aws cognito-idp admin-create-user`) if standing up a bare Cognito pool with no self-service
sign-up flow.

## Step 3: SAM deployment artifact bucket

`dev.deployment-profile.jsonc`'s `awsToolsDefaults.s3-bucket` names an S3 bucket
(`dev-ulmita-deployments` for Utah) that `dotnet lambda deploy-serverless` uploads build artifacts
to. Create an equivalent bucket in NC's account (standard S3 bucket, no special configuration
found in the template or scripts beyond it needing to exist and be writable by the deploying
identity) before the first deploy.

## Step 4: Fill in the deployment profile

Copy `cloud-deployment/dev.deployment-profile.jsonc` (or start `prod.deployment-profile.jsonc` from
its still-unfilled `TODO_SET_PROD_*` placeholders) and set every value to NC's own: AWS profile,
region, S3 bucket, Cognito pool/client IDs (or leave `Oidc:Authority` for a non-Cognito provider —
see [architecture.md](./architecture.md#auth-dual-scheme-jwt-by-default-api-key-as-a-fallback)),
VPC/subnet/security-group IDs, and the `tagElcid`/`tagDept`/`tagDivision`/`tagContact` values
(Utah's cost-center tags won't mean anything in NC's account).

## Step 5: Deploy the stack

```powershell
just validate-deploy dev   # sam validate against the merged template first
just deploy dev
```

This single command provisions the Aurora Serverless v2 cluster (with the `nationalwid` — or
whatever `appAuroraDbName` is set to — database created automatically as part of cluster creation),
both Lambdas, the API Gateway HTTP API with its Cognito/OIDC authorizer, the 7 EventBridge
schedules, the IAM roles, the Secrets Manager-generated DB password, and the SSM parameter that
assembles the full connection string from it. **Nothing is populated yet** — the database exists
but is empty (no tables).

## Step 6: BLS API key (optional but recommended)

Ingestion works without one (BLS's unregistered rate limits are enough for occasional/manual runs),
but for reliable scheduled ingestion, register a free BLS Public Data API key (self-service,
instant, no approval process — via BLS's public API registration page) and store it:

```powershell
aws ssm put-parameter --name "/dev/wid-api/bls-api-key" --type SecureString --value "<key>" `
  --profile <nc-profile> --region <nc-region>
```

Use NC's own registration, not Utah's — Utah's key is tied to Utah's contact information and
doesn't transfer. (Separately, the outbound `User-Agent` header the ingestion Lambda sends still
identifies Utah — see [known-issues-and-gaps.md](./known-issues-and-gaps.md) — update that in code
too, it's unrelated to the API key itself.)

## Step 7: Apply migrations

```powershell
just migrate-dev
```

This invokes the ingestion Lambda with `{"runMigrationsOnly": true}`, which creates the
`wid_migration_history` tracking table (if it doesn't exist) and applies all 17 migration files in
filename order inside one transaction each. **Every ingestion invocation re-runs this migration
check automatically** (unless `skipMigrations: true` is passed) — you do not need to manually
re-run `just migrate-dev` after every future migration file lands, as long as regular scheduled
ingestion is running; it self-applies on the next invocation.

> **Fixed as part of this handoff**: migrations `003_seed_lookups.sql` and
> `007_fix_geography_area_codes.sql` had real bugs that would have broken a from-scratch bootstrap
> — 003 inserted into a column (`areaname`) that doesn't exist until migration 012 renames it from
> `areatitle`, and 007 compared a `char(6)` column against a 7-character literal in a way that would
> have deleted the national geography seed row it was supposed to protect. Both are fixed in this
> copy. If you're comparing against Utah's original `fed-national-wid` repository for any reason,
> don't carry those two files over as-is. See [database-schema.md](./database-schema.md) for the
> full trace of what was wrong and why.

Verify with `just verify-schema-dev` before proceeding.

## Step 8: First ingestion run — order matters

**Do not** invoke the ingestion Lambda with an empty payload (`{}`) for the first run. That mode
runs all 7 dataset groups sequentially in one invocation and risks the 15-minute Lambda timeout —
it's explicitly called "rarely used" in the code for that reason.

Instead, invoke each dataset group separately, **and run `lookups` first**:

```powershell
aws lambda invoke --function-name dev-national-wid-ingestion --profile <nc-profile> --region <nc-region> `
  --cli-binary-format raw-in-base64-out --payload '{"datasetGroup":"lookups"}' out-lookups.json
```

then `laus`, `ces`, `qcew`, `oes`, `projections`, `cpi` — each as its own invocation.

**Why `lookups` has to go first**: `BlsQcewIngestor` resolves MSA-level area codes by looking them
up in the `geographies` table (populated by the `lookups` group, from WID Center data). If
`geographies` is still empty when `qcew` runs, every QCEW row for an MSA-level area silently fails
to resolve and gets dropped — see `TryResolveArea`/`_msaMap` in
[ingestion-pipeline.md](./ingestion-pipeline.md#qcew-blsqcewingestorcs--industry). Because QCEW's
change detection is per-source-file (`SourceHashService`, keyed by the quarter's URL), those
dropped rows **won't be picked up automatically on a later run** — the source file's hash hasn't
changed, so it's skipped as "already processed," permanently short-changing MSA-level industry data
for that quarter unless someone notices and force-refreshes it
(`{"forceRefreshDatasets": "BLS-QCEW"}`). Running `lookups` before `qcew` on first bootstrap avoids
ever hitting this. This ordering dependency wasn't previously documented; it's the kind of thing a
first deploy would only discover by losing MSA rows and not knowing why.

Each dataset group's data volume varies significantly — OEWS alone fetches ~20 years × 3
geographies of Excel workbooks, so budget real wall-clock time for a full historical backfill (this
wasn't timed by Utah's own documentation, so don't assume it's quick; watch
`just tail-ingestion-dev` while it runs).

## Step 9: Verify

```http
GET /health          -- unauthenticated, confirms the API Lambda is reachable
GET /status           -- unauthenticated, per-dataset row counts and freshness
GET /labor-force?stFips=37&periodYear=2024   -- 37 = North Carolina's FIPS code, a real query
```

Use `dev-client.html` (open it locally in a browser) for a manual test harness with a token-entry
UI if you don't have the Angular library wired up yet.

## Step 10: Angular library (if applicable)

Once the API is live, follow [ng-national-wid's standalone-build-guide.md](../../ng-national-wid/docs/standalone-build-guide.md)
to stand up the component library in NC's own Angular workspace, then
[INTEGRATION.md](../../ng-national-wid/INTEGRATION.md) to wire it into a host app pointed at NC's
new API base URL.

## What's still genuinely open after following this playbook

- Whether `data.widcenter.org` (the WID Center Lookups source) requires any registration or
  agreement for automated access beyond what Utah already had as an ARC consortium member — this
  is an external-partner question, not something answerable from the code. Confirm directly with
  the WID Center / ARC consortium before relying on this ingestor in production.
- Everything in [known-issues-and-gaps.md](./known-issues-and-gaps.md) that wasn't fixed as part of
  this handoff (the plaintext SSM password, missing prod config, zero test coverage on 6 of 7
  ingestors, etc.).
