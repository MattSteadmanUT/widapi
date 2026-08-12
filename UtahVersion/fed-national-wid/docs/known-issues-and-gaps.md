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

1. **`LicenseHistory` is empty.** Not a bug — every currently published per-state license-history
   export from WID Center has zero rows, and the CareerOneStop `COSFlatExport` doesn't carry
   history either. No alternate machine-readable source was identified. This can't be "fixed" in
   code; it needs a new external data source if NC wants it populated.

2. **Prod deployment was never completed.** `cloud-deployment/prod.deployment-profile.jsonc`
   still has literal placeholders — `TODO_SET_PROD_COGNITO_CLIENT_ID`, `TODO_SET_PROD_VPC_ID`,
   `TODO_SET_PROD_SUBNET_IDS`, `TODO_SET_PROD_SECURITY_GROUP_IDS`,
   `TODO_SET_PROD_DEPLOYMENTS_BUCKET` — confirmed still present as of this handoff. Only the prod
   Cognito **user pool ID** (`us-gov-west-1_RMhTk2TxL`) is filled in. This isn't NC-specific
   cleanup — it means **the project itself never had a working production environment**, in Utah's
   account or otherwise. Budget for a full prod stand-up, not just a config swap.

3. **The database password is stored in a plaintext SSM `String` parameter, not `SecureString`.**
   Confirmed in `cloud-deployment/lambda.template`: `DbConnectionStringParameter` is
   `"Type": "String"` whose `Value` embeds
   `Password={{resolve:secretsmanager:${Secret}:SecretString:password}}` — i.e. the actual
   resolved password ends up as plaintext inside a non-encrypted SSM parameter, then gets injected
   directly into the API Lambda's environment variable. The password itself originates in Secrets
   Manager (properly generated/rotated there), but this SSM hop re-exposes it in a weaker-at-rest
   form. Switching `DbConnectionStringParameter` to `Type: SecureString` (and updating the
   `ParameterStoreService` read call to pass `WithDecryption: true`, which it already does for
   other params) is a small, well-scoped fix worth doing before NC's first deploy.

4. **API keys don't auto-disable when the underlying Cognito user is disabled.** Only the base
   lifecycle (create/rotate/revoke/expire) exists — there's no event-driven sync from Cognito
   admin-disable actions to `apikeys.status`. If a user is deactivated in ULMITA/Cognito, any API
   keys they created stay active until manually revoked.

5. **Gateway-level throttling was never decided.** `docs/stakeholder-feedback-plan-2026-07-29.md`
   leaves this as an open decision (REST API usage-plans vs. HTTP-API app-level throttling) — it
   was never implemented either way. Current rate limiting, if any, is whatever HTTP API's defaults
   provide plus the unused `rateProfile` field on API keys (`standard`/`elevated` policies exist in
   the `apikeyratepolicies` table but nothing in `ApiKeyService`/`ApiKeyAuthHandler` currently reads
   or enforces them).

6. **Broader non-core WID table families beyond what's implemented are still out of scope.**
   `docs/wid30-structure-validation-2026-07-31.md` explicitly says optional WID table families
   beyond the current 33 tables aren't migrated or exposed, and there's no automated
   field-by-field validator comparing the live schema against the WID 3.0 structure document —
   conformance has been checked manually so far, not continuously.

7. **`sam validate --lint`/`cfn-lint` has never been run** against `lambda.template` — the 2026-06-30
   hardening changes (IAM role split, Aurora snapshot policy, env-scoped SSM paths — all confirmed
   present in the current template) were authored in an environment without those tools installed,
   so the template has only ever been JSON-parsed, not linted. Worth running once before NC's first
   deploy just to catch anything that slipped through.

8. **`ForceRefreshDatasets` sets a process-wide environment variable with no reset path** — see
   [ingestion-pipeline.md](./ingestion-pipeline.md#payload-options-ingestrequest). Possible
   warm-Lambda-container leak between invocations; not confirmed to have caused a real incident,
   but not proven safe either.

9. **QCEW's upsert `ON CONFLICT` column list references `codetype` while its `INSERT` column list
   uses `indcodetype`** — see [database-schema.md](./database-schema.md). Needs verification
   against the live `industry` table constraint definition.

10. **Zero ingestor-class-level tests for six of the seven ingestors** (LAUS, CES, QCEW, OEWS,
    Projections, WID Center Lookups — only CPI has one). See [testing.md](./testing.md) for detail
    and a suggested starting approach (fixture-based tests like `BlsFlatFileServiceTests.cs`
    already uses).

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
