# Deployment & Operations Runbook

## AWS resources (from `cloud-deployment/lambda.template`, an AWS SAM template)

| Resource | Type | Notes |
|---|---|---|
| `NationalWidVpcAuroraSubnetGroup` | `AWS::RDS::DBSubnetGroup` | Private subnets, reused from the deployment profile's `vpcSubnetIds` |
| Aurora master password | `AWS::SecretsManager::Secret` | Auto-generated |
| `NationalWidAuroraCluster` | `AWS::RDS::DBCluster` | Aurora Serverless v2, PostgreSQL 16 |
| Aurora instance | `AWS::RDS::DBInstance` | `db.serverless` instance class, min/max ACU set per environment (`appAuroraMinCapacity`/`appAuroraMaxCapacity` — dev is 0.5–8) |
| DB connection string | `AWS::SSM::Parameter` | Written by the template so the Lambdas can read it at cold start |
| HTTP API | `AWS::Serverless::HttpApi` | API Gateway HTTP API (payload format v2), Cognito JWT authorizer |
| API Lambda role | `AWS::IAM::Role` | Least-privilege execution role for `NationalWidApiFunction` |
| Ingestion Lambda role | `AWS::IAM::Role` | Execution role for `NationalWidIngestionFunction`, includes SSM/Parameter Store read access |
| `NationalWidApiFunction` | `AWS::Serverless::Function` | .NET 8, VPC-attached, fronts all `/`-rooted endpoints |
| `NationalWidIngestionFunction` | `AWS::Serverless::Function` | .NET 8, VPC-attached, 2048MB / 900s timeout |
| EventBridge Scheduler role | `AWS::IAM::Role` | Grants the 7 schedules `lambda:InvokeFunction` on the ingestion Lambda |
| 7× `AWS::Scheduler::Schedule` | — | One per dataset group — see the schedule table in [ingestion-pipeline.md](./ingestion-pipeline.md#schedule-from-cloud-deploymentlambdatemplate) |

Auth in front of the API is **AWS Cognito** (ULMITA's existing user pool — this API does not own
or provision its own user pool) plus the application-layer API key system
(`apikeys`/`apikeyratepolicies` tables, see [architecture.md](./architecture.md#auth-dual-scheme-jwt-by-default-api-key-as-a-fallback)).

## Deployment profiles — what's Utah-specific

`cloud-deployment/dev.deployment-profile.jsonc` and `prod.deployment-profile.jsonc` carry the AWS
account/network identifiers the SAM template deploys into. None of these are secrets (no
credentials appear in the files), but **every one of them is a Utah GovCloud resource ID that will
not exist in North Carolina's AWS account** and must be replaced before NC can deploy:

- `vpcId`, `vpcSubnetIds`, `vpcSecurityGroupIds` — Utah's GovCloud VPC network.
- `appCognitoUserPoolId`, `appCognitoClientId`, `appCognitoClientIdSecondary` — Utah's ULMITA
  Cognito user pool (dev: `us-gov-west-1_OGMbjPhYh`). **This is the biggest open question for
  NC** — see [known-issues-and-gaps.md](./known-issues-and-gaps.md) for whether NC stands up its
  own Cognito pool (and therefore its own ULMITA-equivalent identity layer) or this API needs a
  different auth model entirely once it's no longer living inside Utah's ULMITA platform.
- `awsToolsDefaults.profile` (`GovDev`/`GovProd`) — local AWS CLI profile names Utah configured on
  Matt Steadman's machine; meaningless outside that environment. NC will configure their own.
- `s3-bucket` (`dev-ulmita-deployments`) — Utah's SAM deployment artifact bucket.
- `tagContact: "MattSteadman@utah.gov"` — update to NC's contact before deploying, and check for
  other `tagElcid`/`tagDept`/`tagDivision` tag values that may follow Utah's internal cost-center
  conventions rather than anything NC needs to preserve.

None of this is hard to redo — it's standard "stand up your own copy of this stack" work — but
budget time for it before the first NC deploy, and don't assume any ID in these two files is
reusable.

## Common tasks

All of these are `just` recipes (`justfile`) that wrap the underlying PowerShell scripts in
`dev-scripts/` — use `just` where possible, drop to the scripts directly only if you need a
parameter `just` doesn't expose.

| Task | Command | What it does |
|---|---|---|
| Build | `just build` | `dotnet build` |
| Run API locally | `just run` | `dotnet watch run` against `NationalWid.Api` — needs local access to a reachable Postgres connection string (see `appsettings.Development.json`) |
| Run ingestion locally | `just ingest` | `dotnet run` against `NationalWid.Ingestion` — needs AWS credentials with Parameter Store read access, since it resolves the DB connection string and BLS API key the same way the deployed Lambda does |
| Test | `just test` | `dotnet test` — see [testing.md](./testing.md) |
| Test with coverage | `just test-coverage` | Same, plus Cobertura output to `./TestResults/` |
| Deploy | `just deploy dev` / `just deploy prod` | Builds Release, then `dotnet lambda deploy-serverless` against the environment's `.deployment-profile.jsonc` — this deploys **both** Lambdas and the full CloudFormation stack (Aurora, API Gateway, schedules) in one shot |
| Validate template only | `just validate-deploy dev` | `sam validate` against the merged template, no actual deploy |
| Watch stack status | `just watch-stack-dev` / `-prod` | Polls CloudFormation stack events with rollback-aware terminal-state detection — use this during a deploy instead of refreshing the AWS Console |
| Run migrations | `just migrate-dev` | Invokes the **ingestion Lambda** with `{"runMigrationsOnly": true}` — migrations are applied by the ingestion Lambda's code, not a separate migration runner. `Run-Migrations.ps1` then reads back `migrationsApplied`/`migrationsSkipped`/`tableChecks` from the Lambda's response and fails the script if any expected table is missing. |
| Verify schema only | `just verify-schema-dev` | Same invocation with `{"verifyOnly": true}` — checks table existence without applying anything |
| Trigger ingestion | `just ingest-dev` | `aws lambda invoke` with an empty payload — runs **all 7 dataset groups sequentially in one invocation** (the "rarely used" mode per `Function.cs`'s own comment — risks the 15-minute timeout; prefer invoking with a specific `datasetGroup` payload for anything but a full from-scratch backfill) |
| Tail ingestion logs | `just tail-ingestion-dev` | `aws logs tail` with `--follow`, 5-minute lookback |
| Tail API logs | `just monitor` | `./dev-scripts/Tail-Logs.ps1` against the API Lambda |

## Forcing a targeted re-ingestion

To re-run one dataset group with a specific payload (bypassing the `justfile`'s all-groups
default), invoke directly:

```powershell
aws lambda invoke --function-name dev-national-wid-ingestion --profile GovDev --region us-gov-west-1 `
  --cli-binary-format raw-in-base64-out `
  --payload '{"datasetGroup":"laus"}' `
  out.json
```

To force reprocessing even when a source's content hash hasn't changed (see
[ingestion-pipeline.md](./ingestion-pipeline.md#idempotency-sourcehashservice)):

```json
{ "forceRefreshDatasets": "BLS-QCEW,BLS-FLAT-CE" }
```

or `"forceRefreshDatasets": "ALL"` to force every source. Remember the known warm-Lambda-reuse
caveat on this flag documented in ingestion-pipeline.md before relying on it repeatedly in quick
succession.

## GovCloud-specific notes

- Everything is deployed to `us-gov-west-1` (AWS GovCloud), not a commercial AWS region — don't
  assume commercial-region service availability/quotas/console URLs apply.
- The two AWS CLI profiles referenced throughout (`GovDev`, `GovProd`) are local machine
  configuration, not something baked into the repo — NC will need to configure equivalent profiles
  pointed at their own GovCloud (or commercial, if NC's WID hosting doesn't require GovCloud —
  confirm this requirement rather than assuming it carries over) account.
- Both Lambdas are VPC-attached (to reach Aurora over a private subnet), which means cold starts
  pay ENI-attachment latency — expected/known, not a bug, but worth knowing if NC investigates API
  latency complaints.

## Migrations reminder

Migrations are applied by the **ingestion Lambda**, not a standalone tool — `just migrate-dev`
works by invoking that Lambda with a special payload. There is no separate "migration Lambda" or
CLI. See [database-schema.md](./database-schema.md) for the full migration history and known
issues in the migration files themselves (including one migration that references a column that
doesn't exist yet as written — worth a clean-database dry run before NC's first from-scratch
deploy).
