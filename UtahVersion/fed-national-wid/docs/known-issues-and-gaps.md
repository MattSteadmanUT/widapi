# Known Issues & Gaps

The honest "what's not done" doc. Every item below was checked against current source or a
CloudFormation template, not just carried forward from an old status note — where a historical
handoff claimed something was "not yet investigated," this doc says whether that turned out to be
true as of this handoff (2026-08-12, repo HEAD `6b47131`).

## Confirmed resolved (don't re-investigate these)

| Historical issue | Resolution |
|---|---|
| DB schema not present at first deploy (`42P01` errors) | Idempotent `MigrationService.cs` runner added |
| QCEW returning 0 rows | Reworked from BLS timeseries API to per-quarter CSV slices; later hardened with county/MSA geography resolution |
| OEWS returning 0 rows / crashing on corrupted files | Switched to downloadable-ZIP/XLSX ingestion + `SafeFetchWorkbookAsync` per-task error isolation |
| Authorization policy blocking valid users | `FallbackPolicy` simplified to `RequireAuthenticatedUser()`; verified end-to-end with a real Cognito JWT |
| SOC-hyphen code format handling | `WidCodes.NormalizeCode()` added, applied across all controllers |
| Status screen showing stale/inconsistent dataset names | `StatusService.CanonicalizeDataSet`/`GetAliases` map every historical label to the current canonical name |
| Projections ingestor incomplete | Fully redesigned (v3→v4) after discovering the source API doesn't support per-SOC-group queries |
| **"Audit LAUS/CES/QCEW for the same code-format issues found in OEWS/Projections"** — the headline open item in the 2026-07-28 handoff | Actually done in a later commit: state-level API fallback added to LAUS/CES, real county/MSA geography resolution added to QCEW, source-hash dedup added to all three |
| API `sort`/multi-value/wildcard filters (stakeholder feedback P0) | Implemented (`QuerySorting.cs`, `QueryFiltering.cs`) |
| API key lifecycle (stakeholder feedback P1) | Implemented (create/list/rotate/revoke, SHA-256 hash storage) |
| CPI schema + ingestion (stakeholder feedback P2) | Implemented and populated |
| Licensing data ("no BLS source, deferred") | Superseded — sourced from WID Center `COSFlatExport` + per-state WID 2.8 exports instead, all 56/57 states |
| API integration tests were placeholder-only | Real `WebApplicationFactory`-based tests now exist in `tests/NationalWid.Api.Tests/UnitTest1.cs` (misleading filename, real content) covering `/health`, `/status`, and auth-required 401 checks |
| CPI ingestion Lambda timeout | Resolved by giving each dataset group its own 900-second Lambda invocation instead of sharing one window |

## Still genuinely open — pick these up first

0. **The WID 3.0 spec (`specs/wid-3.0/draft.yaml`, this repo's root) has real drift from this
   implementation, and Utah's own "no remaining gaps" parity claim is wrong.** This is significant
   enough to get its own document — see
   [spec-contract-drift.md](./spec-contract-drift.md) before building anything against the spec.
   Short version: CPI and API keys are entirely undocumented in the spec, the projections
   directory endpoints live at a different URL than the spec says, and `ProjectionsMatrix`'s key
   structure is architecturally different from what the spec describes.

1. **`LicenseHistory` is empty.** Not a bug — every currently published per-state license-history
   export from WID Center has zero rows, and the CareerOneStop `COSFlatExport` doesn't carry
   history either. No alternate machine-readable source was identified. This can't be "fixed" in
   code; it needs a new external data source if NC wants it populated.

2. **CPI endpoints require no authentication at all — confirm this was intentional.** All four
   `/cpi*` endpoints carry `[AllowAnonymous]` at the method level (`CpiController.cs`), which wins
   over the `[Authorize]` attribute the controller also declares and over `Program.cs`'s global
   `RequireAuthenticatedUser()` fallback policy. Every other dataset in this API requires
   authentication; CPI is the sole, undocumented exception. Nothing in the code, commit history, or
   prior handoff notes explains why — it could be a deliberate choice (price/inflation data judged
   more appropriate for fully public access than labor-market microdata) or simply an oversight
   carried over from whoever added the CPI feature. Confirm intent before NC either relies on CPI
   staying public or "fixes" it to require auth like everything else — either is a one-line change
   (add/remove the attribute) but the decision itself needs a real answer, not a guess.

3. **Prod deployment was never completed.** `cloud-deployment/prod.deployment-profile.jsonc`
   still has literal placeholders — `TODO_SET_PROD_COGNITO_CLIENT_ID`, `TODO_SET_PROD_VPC_ID`,
   `TODO_SET_PROD_SUBNET_IDS`, `TODO_SET_PROD_SECURITY_GROUP_IDS`,
   `TODO_SET_PROD_DEPLOYMENTS_BUCKET` — confirmed still present as of this handoff. Only the prod
   Cognito **user pool ID** (`us-gov-west-1_RMhTk2TxL`) is filled in. This isn't NC-specific
   cleanup — it means **the project itself never had a working production environment**, in Utah's
   account or otherwise. Budget for a full prod stand-up, not just a config swap.

4. **The database password is stored in a plaintext SSM `String` parameter, not `SecureString`.**
   Confirmed in `cloud-deployment/lambda.template`: `DbConnectionStringParameter` is
   `"Type": "String"` whose `Value` embeds
   `Password={{resolve:secretsmanager:${Secret}:SecretString:password}}` — i.e. the actual
   resolved password ends up as plaintext inside a non-encrypted SSM parameter, then gets injected
   directly into the API Lambda's environment variable. The password itself originates in Secrets
   Manager (properly generated/rotated there), but this SSM hop re-exposes it in a weaker-at-rest
   form. Switching `DbConnectionStringParameter` to `Type: SecureString` (and updating the
   `ParameterStoreService` read call to pass `WithDecryption: true`, which it already does for
   other params) is a small, well-scoped fix worth doing before NC's first deploy.

5. **API keys don't auto-disable when the underlying Cognito user is disabled.** Only the base
   lifecycle (create/rotate/revoke/expire) exists — there's no event-driven sync from Cognito
   admin-disable actions to `apikeys.status`. If a user is deactivated in ULMITA/Cognito, any API
   keys they created stay active until manually revoked.

6. **Gateway-level throttling was never decided.** `docs/stakeholder-feedback-plan-2026-07-29.md`
   leaves this as an open decision (REST API usage-plans vs. HTTP-API app-level throttling) — it
   was never implemented either way. Current rate limiting, if any, is whatever HTTP API's defaults
   provide plus the unused `rateProfile` field on API keys (`standard`/`elevated` policies exist in
   the `apikeyratepolicies` table but nothing in `ApiKeyService`/`ApiKeyAuthHandler` currently reads
   or enforces them).

7. **Broader non-core WID table families beyond what's implemented are still out of scope.**
   `docs/wid30-structure-validation-2026-07-31.md` explicitly says optional WID table families
   beyond the current 33 tables aren't migrated or exposed, and there's no automated
   field-by-field validator comparing the live schema against the WID 3.0 structure document —
   conformance has been checked manually so far, not continuously.

8. **`sam validate --lint`/`cfn-lint` has never been run** against `lambda.template` — the 2026-06-30
   hardening changes (IAM role split, Aurora snapshot policy, env-scoped SSM paths — all confirmed
   present in the current template) were authored in an environment without those tools installed,
   so the template has only ever been JSON-parsed, not linted. Worth running once before NC's first
   deploy just to catch anything that slipped through.

9. **`ForceRefreshDatasets` sets a process-wide environment variable with no reset path** — see
   [ingestion-pipeline.md](./ingestion-pipeline.md#payload-options-ingestrequest). Possible
   warm-Lambda-container leak between invocations; not confirmed to have caused a real incident,
   but not proven safe either.

10. **QCEW's upsert `ON CONFLICT` column list references `codetype` while its `INSERT` column list
    uses `indcodetype`** — see [database-schema.md](./database-schema.md). Needs verification
    against the live `industry` table constraint definition.

11. **Zero ingestor-class-level tests for six of the seven ingestors** (LAUS, CES, QCEW, OEWS,
    Projections, WID Center Lookups — only CPI has one). See [testing.md](./testing.md) for detail
    and a suggested starting approach (fixture-based tests like `BlsFlatFileServiceTests.cs`
    already uses).

12. **No OpenAPI/Swagger generation exists in the API project** — confirmed by checking
    `NationalWid.Api.csproj` and `Program.cs` for Swashbuckle/NSwag: neither is registered. This
    means the "generated OpenAPI from this API should match `specs/wid-3.0/draft.yaml`" governance
    rule in `AGENTS.md` is currently aspirational, not enforced — any drift between the spec and the
    real implementation has to be found by manual comparison. Adding real OpenAPI generation would
    make that governance rule actually checkable.

13. **A CI workflow from the original repository was deliberately not carried into this copy**:
    `.github/workflows/restrict-prod-prs.yml`, which blocked any PR into a `prod` branch unless it
    came from `dev`. It wasn't copied because it hardcodes Utah's exact two-branch (`dev`/`prod`)
    workflow, which may not match whatever branching strategy NC adopts for wherever this ends up
    hosted on GitHub. If NC wants an equivalent branch-protection rule, it's a small, easy workflow
    to recreate — the original is on GitHub at `utahdws/fed-national-wid` if a reference is useful.

14. **The `tagElcid`/`tagDept`/`tagDivision`/`tagContact`/`tagEnv` template parameters are never
    actually applied to any AWS resource.** Confirmed by grepping `lambda.template`: these
    parameters exist and flow in from both deployment profiles, but zero `"Tags"` properties
    reference them anywhere in `Resources`. If NC's account has cost-allocation-tag or tag-policy
    enforcement, this template currently doesn't tag anything despite looking like it has a
    tagging convention built in. Either wire these into the actual resource definitions or drop
    the unused parameters — as shipped, they're dead configuration.

15. **`--capabilities CAPABILITY_NAMED_IAM` is not passed to `dotnet lambda deploy-serverless`**
    in `dev-scripts/Deploy.ps1`, and the template names its IAM roles explicitly
    (`${appEnvironment}-national-wid-api-role`, etc.), which CloudFormation normally requires that
    capability acknowledgment for. **This was deliberately left unfixed rather than guessed at** —
    `dotnet lambda deploy-serverless` (Amazon.Lambda.Tools) may handle this differently than the
    raw AWS CLI's `cloudformation deploy`, and asserting the wrong flag syntax risks breaking the
    script worse than leaving it. Before NC's first `just deploy dev`, run
    `dotnet lambda deploy-serverless --help` to check the actual current syntax, or be prepared to
    add whatever capability-acknowledgment flag it requires if the deploy fails or prompts
    interactively (which would hang in a non-interactive/CI run).

16. **Aurora's cluster identifier is deterministic** (`${appEnvironment}-national-wid`, not
    unique per deploy attempt), and the cluster has `DeletionPolicy`/`UpdateReplacePolicy:
    Snapshot`. If a first deploy attempt fails and the stack gets rolled back or deleted, or if NC
    tears down and redeploys dev while iterating, CloudFormation will attempt to snapshot the
    cluster under a system-generated identifier tied to that deterministic cluster ID. It's
    untested whether a second `CREATE_COMPLETE` attempt at the same `appEnvironment` value could
    collide with a leftover snapshot from a prior failed attempt, requiring manual snapshot
    cleanup. Watch for this if an early deploy attempt fails and a retry behaves unexpectedly.

17. **Unregistered-BLS-key rate limits are undocumented against the actual recurring schedule.**
    `ParameterStoreService.GetParameterOrDefaultAsync` correctly doesn't throw if no BLS API key is
    configured (see [first-deployment-playbook.md](./first-deployment-playbook.md)), but nothing
    documents what BLS's unregistered-key rate limits actually are, or whether the 7 scheduled
    EventBridge invocations — two of them (`laus`, `ces`) clustered at the identical time on every
    weekday — would exceed an anonymous quota on an ongoing basis, as opposed to the one-time
    manual backfill the deployment playbook walks through. Register a real BLS API key before
    relying on the schedules long-term, not just for the initial bootstrap.

18. **Minor script robustness gaps**, not urgent: `dev-scripts/Tail-Logs.ps1`'s `-Function all`
    mode starts two background jobs and polls `Receive-Job` without checking for job failures — if
    one job fails to start (e.g. tailing a log group that doesn't exist yet because nothing's been
    invoked), the failure won't surface clearly. `dev-scripts/Run-Migrations.ps1` throws if the
    Lambda response lacks a `tableChecks` field, which only happens if an *older* ingestion Lambda
    build (predating that field) is deployed — not a risk for a true first deploy, but worth
    knowing if a future rollback ever mixes an old Lambda build with this script.

19. **Two independent CORS layers exist with different policies, which can be confusing to debug.**
    API Gateway's own CORS configuration (`lambda.template`'s `HttpApi` resource) allows all
    origins (`AllowOrigins: ["*"]`); the actual restriction to specific frontend origins happens one
    layer deeper, in the ASP.NET Core app itself via the `Cors__AllowedOrigins__N` environment
    variables (now template-parameterized as `appCorsOrigin1`-`4` — see
    [deployment-and-operations.md](./deployment-and-operations.md)). This isn't a bug — app-level
    CORS is the real gatekeeper and API Gateway's wide-open setting is harmless on its own — but if
    NC ever debugs a CORS rejection, check the app-level config first; the API Gateway layer will
    never be the thing blocking a request.

## Needs a live check NC will have to do themselves

These can't be resolved by reading source — they require live AWS/database access, and since NC
will not inherit Utah's AWS account, most of this data won't transfer regardless. Listed so NC
knows to check these things fresh rather than assume Utah's last-known numbers still apply:

- **OEWS 2024-2025 data presence** — the ingestor's 20-year lookback would pick this up
  automatically if BLS has published it (`SafeFetchWorkbookAsync` no-ops gracefully on files that
  don't exist yet), but whether it *has* loaded requires `SELECT DISTINCT periodYear FROM iowage
  ORDER BY periodYear DESC` against a live database.
- **Whether the 2026-06-30 deployment-hardening changes were actually deployed** to Utah's live
  stacks (vs. just present in the template) — moot for NC either way, since NC will deploy fresh,
  but explains why some historical docs describe symptoms the current template shouldn't produce.
- **Current row counts / data freshness** — every number in [HANDOFF.md](../../HANDOFF.md) (e.g.
  "264,688 total rows") is a point-in-time snapshot from a specific Utah ingestion run. NC will run
  ingestion fresh into its own database regardless, so treat these as historical proof-of-function,
  not current state.

## Documentation hygiene note

Utah's original working repo carried four overlapping handoff notes written at different points
in the project (2026-06-11, 2026-06-12, 2026-07-28, plus a final `HANDOFF.md`) alongside four more
dated session-note docs. The earliest of these was stale relative to the later ones — it still
described QCEW/OEWS as broken and Projections as unimplemented, both fixed since. Rather than carry
all of that forward as separate files with overlapping and sometimes contradictory content, their
substance was reconciled against current source and folded into [HANDOFF.md](../../HANDOFF.md)
(the consolidated version at the `UtahVersion/` root) and this document. Treat those two as
current status; nothing from the original per-session notes should be assumed still accurate on
its own.
