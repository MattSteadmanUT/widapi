# Deployment hardening note — 2026-06-30

This note records the deployment-hardening work visible in the current diff. It documents intent, operational impact, and deployment/rollback requirements. It does **not** imply SAM validation, CloudFormation validation, or deployment occurred.

## Threats and risks addressed

1. **Cross-environment parameter bleed:** ingestion previously defaulted to global Parameter Store paths (`/wid-api/...`), which made it too easy for dev and prod to read or overwrite the same values.
2. **Over-privileged Lambdas:** API and ingestion did not need the same AWS permissions. Shared or broader access increases blast radius if one function is compromised.
3. **Unused secret-extension surface area:** wiring the AWS Parameters and Secrets Lambda extension layer without using it adds code, permissions, and maintenance burden for no runtime benefit.
4. **Aurora replacement data-loss risk:** a stack update that replaces the Aurora cluster should retain a recovery point instead of deleting the old cluster outright.

## What changed

### Environment-scoped SSM paths

- `cloud-deployment\lambda.template` now writes the database connection string to:
  - `/${appEnvironment}/wid-api/db-connection-string`
- The ingestion Lambda now receives environment-scoped parameter names through environment variables:
  - `DB_CONNECTION_STRING_PARAMETER=/${appEnvironment}/wid-api/db-connection-string`
  - `BLS_API_KEY_PARAMETER=/${appEnvironment}/wid-api/bls-api-key`
- `src\NationalWid.Ingestion\Function.cs` resolves parameter paths from those environment variables and trims whitespace before use. Local fallback defaults remain `/wid-api/db-connection-string` and `/wid-api/bls-api-key`, which preserves non-deployed/local behavior.

### Required BLS-key migration

The stack does **not** create or migrate the BLS API key automatically. Before deploying this change to an environment, copy the existing key into the new environment-specific path for that environment:

- dev: `/dev/wid-api/bls-api-key`
- prod: `/prod/wid-api/bls-api-key`

Until that migration happens, ingestion in the updated stack will look for the new path and can fail to read the BLS key even if `/wid-api/bls-api-key` still exists.

### Split least-privilege Lambda roles

- The API Lambda now uses a dedicated `ApiExecutionRole`.
- The ingestion Lambda now uses a dedicated `IngestionExecutionRole`.
- `IngestionExecutionRole` keeps only read access needed for Parameter Store under:
  - `arn:${AWS::Partition}:ssm:${AWS::Region}:${AWS::AccountId}:parameter/${appEnvironment}/wid-api/*`
- The API role is reduced to the standard Lambda execution + VPC access managed policies and does not receive SSM read access from this template.

### Removed unused extension layer and related permissions

- The AWS Parameters and Secrets Lambda extension layer is no longer part of the function configuration in this diff.
- Corresponding template wiring and permissions that were only needed for that extension were removed.
- Runtime parameter access is now performed directly by the ingestion code through `AmazonSimpleSystemsManagementClient`, with the minimal SSM read scope above.

### Aurora replacement behavior

- `AWS::RDS::DBCluster` now has:
  - `DeletionPolicy: Snapshot`
  - `UpdateReplacePolicy: Snapshot`

This means a CloudFormation-driven **replacement** of the Aurora cluster should snapshot the old cluster before removal, which materially improves recoverability during destructive updates. This does **not** eliminate the need for pre-deploy database backups or restore rehearsals.

## Tests added

- `tests\NationalWid.Ingestion.Tests\ParameterPathTests.cs`
  - verifies fallback behavior when the env var is missing
  - verifies fallback behavior when the env var is whitespace
  - verifies trimming and use of an environment-scoped configured path

These tests cover the new parameter-path resolution behavior that makes the SSM lookup environment-aware.

## Local verification performed

Local verification completed for this change set:

- `dotnet test NationalWid.Api.slnx --no-restore`: 87 passed (52 API, 35 ingestion)
- the CloudFormation/SAM template parses as valid JSON

- verified template wiring for environment-scoped parameter names
- verified dedicated API vs ingestion IAM roles and the remaining SSM scope
- verified Aurora `UpdateReplacePolicy` / `DeletionPolicy` snapshot settings
- verified ingestion path-resolution logic and its unit tests

`sam` and `cfn-lint` are **not installed** in this environment, so no SAM transform/validation or CloudFormation linting was run here.

## Deployment checklist

1. Copy the existing BLS API key into the environment-specific SSM path for each target environment (`/dev/wid-api/bls-api-key`, `/prod/wid-api/bls-api-key`).
2. Confirm the target deployment profile sets the intended `appEnvironment`.
3. Run `sam validate --lint` and `cfn-lint` in an environment where those tools are installed.
4. Review the generated CloudFormation change set, paying particular attention to IAM and Aurora replacement actions.
5. Deploy the stack change.
6. Confirm the ingestion Lambda environment contains the expected `DB_CONNECTION_STRING_PARAMETER` and `BLS_API_KEY_PARAMETER` values.
7. Confirm the API Lambda is using the dedicated API role and the ingestion Lambda is using the dedicated ingestion role.
8. Smoke-test `/health`, `/status`, an authenticated data route, and a manual ingestion invocation before relying on scheduled runs.
9. Keep the legacy `/wid-api/bls-api-key` value in place until the new environment-specific path has been proven in the target environment.

## Rollback checklist

1. Revert the stack/code to the prior revision if the new parameter-path or IAM split causes operational issues.
2. If needed, repoint ingestion to the legacy global SSM paths or restore the prior Lambda environment configuration.
3. Re-deploy the reverted stack.
4. If a stack update replaced Aurora resources, use the retained cluster snapshot created by the replacement policy if database recovery is required.

## Important remaining risk

The highest unresolved secret-handling risk remains unchanged: database credentials are still materialized into a **plaintext SSM `String` parameter** (`/${appEnvironment}/wid-api/db-connection-string`) and into Lambda environment configuration (`ConnectionStrings__WidDb` for the API). Even though the password originates in Secrets Manager, this design still expands the number of places where the assembled credential exists in plaintext.

This should be treated as a follow-up hardening item. A stronger design would avoid storing the fully assembled connection string as plaintext in SSM and avoid injecting it directly into Lambda environment variables.
