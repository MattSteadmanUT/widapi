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

## Platform portability — what's AWS-specific vs. portable

Utah built and ran this on AWS, but **NC is not locked into AWS, or Cognito specifically, to run
this application.** That wasn't true by accident — most of the coupling that existed was either
never real (a hardcoded issuer URL shape, two unused SDK package references) or is isolated to a
small, identifiable surface rather than spread through the business logic. Here's the accurate
breakdown, verified against the actual code rather than assumed:

### Already portable today — no code changes needed

- **The API itself.** `NationalWid.Api` is a plain ASP.NET Core Kestrel application.
  `AddAWSLambdaHosting(...)` in `Program.cs` is additive: it only engages Lambda-specific hosting
  when actually running inside the Lambda runtime, and is a no-op otherwise — this is exactly how
  `just run`/`dotnet watch run` already runs it locally as an ordinary web server, no Lambda
  runtime present. It will run unmodified on any container host, PaaS, VM, or on-prem server that
  can host a .NET 8 web app.
- **Authentication — at the application layer only.** Standard ASP.NET Core `JwtBearer`/OIDC
  middleware — Cognito is simply the identity provider Utah configured, not a requirement baked
  into the C# code. `Program.cs` builds its OIDC issuer URL from
  `Cognito:Region`/`Cognito:UserPoolId` by default (matching Cognito's URL shape), but an explicit
  `Oidc:Authority` configuration value — added as part of this handoff — overrides that
  construction entirely, with **zero code changes**, as long as the provider is OIDC-compliant and
  issues standard JWTs. The one Cognito-specific quirk (`AudienceValidator`'s `client_id`-claim
  fallback, since Cognito access tokens don't populate the standard `aud` claim) is additive and
  harmless against any provider that doesn't set that claim.

  **This does not mean switching identity providers is template-free, though** — as currently
  deployed on AWS, API Gateway's own native JWT authorizer (`lambda.template`'s `HttpApi` resource,
  the `UlmitaCognito` authorizer block) sits in front of the Lambda and validates the token
  *before* `Program.cs` ever sees it. That authorizer's issuer URL is built from
  `appCognitoUserPoolId` in Cognito's specific URL shape, with no `Oidc:Authority`-equivalent
  parameter — it has no way to point at a non-Cognito provider without editing the SAM template's
  `Auth.Authorizers` block directly (or replacing it with a Lambda authorizer, a larger change).
  So: swapping to a different Cognito pool (Utah's or NC's own) is template-free: same URL shape,
  different pool ID, all via the deployment profile. Swapping to a genuinely different OIDC
  provider (Auth0, Okta, Azure AD B2C, Keycloak) requires editing `lambda.template`'s authorizer
  block, even though `Program.cs` itself needs no change either way.
- **The database.** Plain PostgreSQL 16 via Npgsql/EF Core (`ConnectionStrings:WidDb`). Aurora
  Serverless v2 is Utah's hosting choice, not a dependency — any managed or self-hosted Postgres
  instance works.
- **The ingestion business logic.** All 7 ingestors and their shared services
  (`BlsFlatFileService`, `SourceHashService`, etc. — see
  [ingestion-pipeline.md](./ingestion-pipeline.md)) take a plain connection string and an
  `HttpClient`; none of them reference AWS types. Only the *bootstrap* around them (below) is
  AWS-coupled.
- **The Angular library.** Already fully platform-agnostic by design — see
  [ng-national-wid/README.md](../../ng-national-wid/README.md#configuration-nationalwidconfig);
  `getToken()` accepts whatever bearer/API-key string the host app's own auth system produces.

As part of this handoff, two AWS SDK package references (`AWSSDK.SimpleSystemsManagement`,
`AWSSDK.SSO`) were removed from `NationalWid.Api.csproj` — they were never actually called
anywhere in the API's code and only overstated its AWS coupling.

### Genuinely AWS-specific — would need rework to leave AWS entirely

- **The ingestion Lambda's entry point** (`Function.cs`). Its handler signature
  (`FunctionHandler(IngestRequest? request, ILambdaContext context)`) and its per-group time-budget
  logic (`context.RemainingTime`, used throughout `RunSafeAsync` — see
  [ingestion-pipeline.md](./ingestion-pipeline.md)) are genuinely AWS Lambda-specific. Moving to a
  different platform's scheduled-job/function primitive (Azure Functions timer trigger, GCP Cloud
  Run Jobs + Cloud Scheduler, a container + cron, a Kubernetes CronJob) means writing a new
  entry point and an equivalent time-budget mechanism — bounded to this one file's bootstrap and
  dispatch logic, not the ingestors it calls into.
- **Secrets retrieval** (`ParameterStoreService.cs`). Constructed directly with
  `new AmazonSimpleSystemsManagementClient()` in `Function.cs`'s constructor — not behind an
  interface, so it's a hard dependency on AWS Systems Manager Parameter Store today. Swapping to
  another platform's secrets mechanism (Azure Key Vault, GCP Secret Manager, HashiCorp Vault, or
  plain environment variables) means replacing this one class and its instantiation point; nothing
  downstream (the ingestors) knows or cares where the connection string / BLS API key came from —
  they receive plain strings.
- **The IaC and deployment tooling.** `cloud-deployment/lambda.template` (SAM/CloudFormation),
  `dev-scripts/Deploy.ps1` (`dotnet lambda deploy-serverless`), the 7 EventBridge Scheduler
  entries, and the Secrets Manager-backed DB password are all AWS-specific infrastructure
  definitions. None of this transfers to another cloud — that's expected of any IaC — but the
  resource table above is a reasonably complete checklist of what equivalent infrastructure NC
  would need to stand up (managed Postgres, a scheduled-compute primitive, a secrets store, an API
  gateway/ingress, a scheduler) if choosing a non-AWS platform.

**Bottom line for NC**: choosing to deploy this on AWS (with either Utah's ULMITA Cognito pool or
NC's own) is the lowest-effort path, since it's what's already built and proven. Choosing a
different cloud is a real option, not blocked by the application code — it concentrates the work
in one Lambda entry point, one secrets-fetching class, and rewriting the IaC layer, rather than
touching the API, the ingestion logic, the database layer, or the Angular library.

## Deployment profiles — what's Utah-specific

`cloud-deployment/dev.deployment-profile.jsonc` and `prod.deployment-profile.jsonc` carry the AWS
account/network identifiers the SAM template deploys into. None of these are secrets (no
credentials appear in the files), but **every one of them is a Utah GovCloud resource ID that will
not exist in North Carolina's AWS account** and must be replaced before NC can deploy:

- `vpcId`, `vpcSubnetIds`, `vpcSecurityGroupIds` — Utah's GovCloud VPC network.
- `appCognitoUserPoolId`, `appCognitoClientId`, `appCognitoClientIdSecondary` — Utah's ULMITA
  Cognito user pool (dev: `us-gov-west-1_OGMbjPhYh`). These specific IDs are dev-environment
  values either way, so NC will need its own environment's IDs regardless — the real decision is
  *whose* pool those IDs point at. Utah has offered to keep hosting ULMITA login for NC's
  analysts (low incremental cost, many are already ULMITA users elsewhere), which is a live,
  low-effort option alongside NC standing up its own pool. Nothing in `Program.cs` hardcodes a
  specific pool — it's entirely config-driven — so this is purely a "which IDs go in the
  deployment profile" decision, not an application change. See the Cognito checklist item in
  [../../HANDOFF.md](../../HANDOFF.md#infrastructure--access-transition-checklist) before
  filling these in.
- `awsToolsDefaults.profile` (`GovDev`/`GovProd`) — local AWS CLI profile names Utah configured on
  Matt Steadman's machine; meaningless outside that environment. NC will configure their own.
- `s3-bucket` — Utah's SAM deployment artifact bucket (name redacted in this handoff since that
  environment is still live; not something NC would reuse regardless — create your own).
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
