# Handoff: Utah DWS → North Carolina

**Status as of 2026-08-12.** Utah DWS is not continuing development on the National WID 3.0 API,
its ingestion pipeline, or its Angular data-explorer library. This document is the single
authoritative status report for North Carolina picking the work up. It supersedes every prior
per-session handoff note Utah wrote along the way — those notes are reconciled into this document,
not carried forward as separate files.

If you read one document before writing any code, read this one, then
[README.md](./README.md) for where everything else lives.

---

## What this project is

The **Workforce Information Database (WID)** is a standardized database structure, governed by the
ARC consortium under an ETA grant, that state workforce agencies use to store and disseminate labor
market information. WID 3.0 (released 2024, revised May 2025) is the current version. ARC member
states — including Utah and North Carolina — collaborated on the WID 3.0 API specification that
lives in this repository's `specs/wid-3.0/draft.yaml`.

Utah DWS built on top of that spec: a **nationally-hosted REST API** (`fed-national-wid/`, this
folder) that pre-loads BLS-published labor market data into a WID 3.0-compliant Aurora PostgreSQL
database, so any state can query national benchmarks or pull data into their own state WID system
without re-implementing BLS ingestion themselves. Utah paired it with an **Angular component
library** (`ng-national-wid/`) — a drop-in data explorer UI any state's portal can embed. Both were
built and operated as part of Utah's ULMITA platform, hosted in AWS GovCloud.

## Why this handoff exists

Utah is stepping back from further development. North Carolina has strong in-house .NET and
Angular expertise, so this handoff is deliberately **not** a "here's how ASP.NET Core works"
document — it's a "here's what we built, why we made the decisions we made, what actually works,
and what's still broken or unfinished" document, so NC can pick up from Utah's actual state of
progress instead of re-discovering it.

---

## Timeline

| Date | Milestone |
|---|---|
| 2026-04-07 | Project start; cost-planning doc written (`docs/phase-1-aws-hosting-cost-proposal.md`, in this repo's root `docs/`) |
| 2026-06-11 | First working CloudFormation deploy; schema not yet migrated, QCEW broken |
| 2026-06-12 | LAUS + CES working with state-level coverage; QCEW reworked to CSV slices; OEWS/Projections still broken or incomplete |
| 2026-06-30 | Deployment hardening: least-privilege IAM role split, Aurora snapshot policy, environment-scoped SSM paths |
| 2026-07-28 | All 5 original ingestors report success (264,688 rows); OEWS error-isolation added; Projections redesigned; auth policy fixed and verified end-to-end; SOC-hyphen code normalization added |
| 2026-07-29 | Stakeholder feedback plan written: API keys, filters/sort, CPI, non-core table expansion prioritized |
| 2026-08-03/04 | "Pre-handoff quality push": LAUS/CES/QCEW ingestor audit actually completed (state-level fallback, county/MSA geography resolution, source-hash dedup added to all three); API keys shipped; CPI added and populated; license data populated (all 56/57 states); parallel per-dataset-group Lambda scheduling; XML docs and README rewrite |
| 2026-08-12 | This handoff prepared; repository content copied and documented into `widapi/UtahVersion/` |

**A documentation-hygiene note worth internalizing early**: Utah's own per-session handoff notes
went stale relative to each other — the earliest ones describe bugs that were fixed weeks later.
Don't trust any single historical note in isolation; this document and
[known-issues-and-gaps.md](./fed-national-wid/docs/known-issues-and-gaps.md) reconcile all of them
against current source.

---

## What's done and verified

- **All 7 ingestors operational**: LAUS, CES, QCEW, OEWS, Projections, CPI, and WID Center
  Lookups. See [ingestion-pipeline.md](./fed-national-wid/docs/ingestion-pipeline.md) for how each
  one actually works and the source-format quirks Utah discovered.
- **33 WID 3.0 tables** tracked; 32 have data as of Utah's last verified ingestion run. Only
  `LicenseHistory` is empty, and that's a genuine upstream data-availability gap, not a bug — see
  known-issues-and-gaps.md.
- **Dual authentication**: Cognito JWT (ULMITA user pool) and a self-service, long-lived API key
  system with SHA-256-hashed storage. See [architecture.md](./fed-national-wid/docs/architecture.md#auth-dual-scheme-jwt-by-default-api-key-as-a-fallback).
- **A generic, table-driven query engine** — CSV multi-value filters, wildcard string matching,
  dynamic sort, page- and cursor-based pagination, and CSV/TSV/PSV/XLSX/JSON export — shared across
  all 11 controllers rather than duplicated per endpoint. This is the most architecturally
  important part of the codebase; read architecture.md before extending it.
- **17 sequential, idempotent SQL migrations** building the full WID 3.0 schema plus deliberate,
  documented deviations from spec where the real data didn't fit it cleanly. See
  [database-schema.md](./fed-national-wid/docs/database-schema.md).
- **A full API-contract parity pass** against `widapi/specs/wid-3.0/draft.yaml` — no known
  endpoint-level gaps as of the last parity-closure review.
- **Real integration tests** exist for the API's core query/pagination/auth behavior (104 tests
  passing across both test projects as of Utah's last local run — re-verify this yourself as part
  of your first build, don't take the number on faith). See
  [testing.md](./fed-national-wid/docs/testing.md) for what's covered and, more importantly, what
  isn't.
- **The Angular data-explorer library** exercises effectively every endpoint and query parameter
  the API exposes — see [ng-national-wid/README.md](./ng-national-wid/README.md).

---

## Known issues — the short version

Full detail, including which historical "known issues" turned out to already be fixed, lives in
[known-issues-and-gaps.md](./fed-national-wid/docs/known-issues-and-gaps.md). The items most worth
NC's early attention:

1. **Production was never deployed.** `prod.deployment-profile.jsonc` has real, unfilled
   `TODO_SET_PROD_*` placeholders for the Cognito client ID, VPC ID, subnet IDs, security group
   IDs, and deployment S3 bucket. There is no working Utah prod environment to model NC's against
   — only dev was ever actually stood up and exercised.
2. **The database password is stored in a plaintext SSM `String` parameter**, not `SecureString` —
   confirmed in `cloud-deployment/lambda.template`. Small, well-scoped fix; do it before NC's first
   deploy.
3. **`LicenseHistory` is empty** and can't be fixed in code — no upstream source currently
   publishes it.
4. **Six of the seven ingestors have zero dedicated unit tests** at the ingestor-class level (only
   CPI does). If NC's first priority is "make this pipeline trustworthy," this is the highest-value
   place to start.
5. Several smaller, concretely-scoped items (a hardcoded Utah contact in outbound BLS request
   headers, one upsert with a possible column-name mismatch, an env-var leak risk in the
   force-refresh flag) are listed with exact file locations in known-issues-and-gaps.md.

---

## Infrastructure & access transition checklist

Everything below is provisioned under **Utah DWS's AWS GovCloud account** and cannot be reused by
NC — none of it is a secret NC needs to be handed, but every one of these needs a North
Carolina-owned equivalent stood up before deployment. Full inventory (exact VPC/subnet/Cognito
IDs, SSM parameter paths, historical Aurora endpoints) is in
[deployment-and-operations.md](./fed-national-wid/docs/deployment-and-operations.md) and
[known-issues-and-gaps.md](./fed-national-wid/docs/known-issues-and-gaps.md) — this is the
checklist form:

- [ ] **AWS account** — NC needs its own AWS GovCloud (or commercial, if GovCloud isn't actually a
      requirement for NC's hosting — confirm rather than assume it carries over) account, with its
      own CLI profiles replacing Utah's `GovDev`/`GovProd`.
- [ ] **VPC/networking** — Utah's dev environment reused ULMITA's shared VPC. NC needs its own VPC,
      private subnets, and security group for the Aurora cluster and VPC-attached Lambdas.
- [ ] **Aurora PostgreSQL cluster** — stood up fresh; none of Utah's data transfers automatically
      (nor should it — NC will run ingestion fresh against BLS/WID Center sources).
- [ ] **Cognito user pool — decide, don't assume.** Utah's API authenticates against **ULMITA's
      existing Cognito user pool**; this project never stood up its own identity provider. Two
      genuinely viable paths, and **Matt Steadman/Utah DWS has offered to keep hosting ULMITA login
      for NC's analysts** rather than requiring NC to stand up a separate identity system — many of
      the same analysts already have ULMITA accounts for other systems, and the incremental cost of
      a small number of additional users is low. Confirm which path NC wants:
      1. **Continue using Utah's ULMITA Cognito pool** — NC's analysts log in through ULMITA the
         same way Utah's do; no new identity infrastructure for NC to build or maintain. Lowest
         effort, and the option Utah is actively offering.
      2. **Stand up an NC-owned Cognito pool** (or other identity provider) as a fully independent
         auth source, if NC prefers full control over its own user base long-term.

      **The code does not need to change either way** — `Program.cs`'s JWT validation reads
      `Cognito:UserPoolId`/`ClientId`/`Region`/`ClientIds` entirely from configuration (see
      [architecture.md](./fed-national-wid/docs/architecture.md#auth-dual-scheme-jwt-by-default-api-key-as-a-fallback)),
      with zero hardcoded pool identifiers anywhere in the C# source — those only ever appear in the
      per-environment deployment profiles (`cloud-deployment/*.deployment-profile.jsonc`), which are
      meant to be edited per deployment regardless. Pointing at Utah's pool, an NC pool, or both
      (e.g. `ClientIds` already accepts a list of allowed app client IDs) is purely a config
      decision. The one genuinely ULMITA-specific thing in the codebase —
      `Auth/CognitoClaimsExtensions.cs`'s `custom:ulmita_activated`/`custom:ulmita_roles` claim-name
      constants — is defined but **not called anywhere** in the current code, so it isn't a
      dependency either way; it's just available if NC wants to read those claims later.
- [ ] **Parameter Store secrets** — NC's own BLS API key registration (`/${env}/wid-api/bls-api-key`)
      and DB connection string, provisioned in NC's account. Recommend fixing the plaintext-SSM
      issue above at the same time.
- [ ] **BLS API key** — Utah's registration key doesn't transfer; register a new one under NC's own
      contact for higher rate limits.
- [ ] **Outbound request identity** — `BlsFlatFileService`'s HTTP requests carry a hardcoded
      `User-Agent: "ULMITA/1.0 (ulmita.org; mattsteadman@utah.gov)"` header. Update this to identify
      NC's project and contact before BLS traffic is attributed to a Utah contact who's no longer
      involved.
- [ ] **Tags** — `tagElcid`, `tagDept`, `tagDivision`, `tagContact` in the deployment profiles are
      Utah's internal cost-center conventions; replace with NC's equivalents.
- [ ] **`sam validate --lint`/`cfn-lint`** — never run against `lambda.template` in Utah's
      environment (tooling wasn't installed where the template was last edited). Worth running once
      before NC's first deploy.

---

## Cost notes

Full detail in `docs/phase-1-aws-hosting-cost-proposal.md` (this repo's root `docs/`, not the
`fed-national-wid/docs/` folder — it predates the implementation and was written during initial
ARC-wide planning). **The single most important caveat**: that proposal's bottom-line numbers
assume reuse of Utah's existing ULMITA GovCloud platform (shared VPC, NAT gateway,
logging/alerting, IAM patterns, CI/CD, secrets governance) and applies a 10-25% "shared
infrastructure credit" to reflect that reuse. **NC will not have that credit** — starting from a
greenfield AWS account means planning around the gross, non-discounted figures in that document,
not the ULMITA-adjusted ones.

Rough envelopes from that proposal (GovCloud, 20% planning uplift already applied, prod+dev
combined):

| Phase | Traffic profile | Monthly | Annual |
|---|---|---|---|
| Phase 1 (staff-only access) | <150–1,500 users, 1–20M req/mo | $185–$706 | $2.2K–$8.5K |
| Phase 2 (public state-website access) | 50–150M req/mo | $1,546–$3,940 | $18.6K–$47.3K |
| Phase 3 (multi-state live backend) | 250M–1B req/mo | $4,156–$16,560 | $49.9K–$198.7K |

Add 20-40% contingency on Phase 2/3 figures if NC wants per-state schema customization — the
proposal flags that as unmodeled since no real per-state extension pattern existed yet when it was
written.

---

## Recommended next steps, in priority order

1. **Stand up NC's own AWS account/VPC** and get a dev deploy working end to end — this is
   prerequisite to everything else.
2. **Confirm the Cognito/identity path with Utah** — continuing to authenticate through Utah's
   ULMITA Cognito pool (the option Utah has offered) vs. NC standing up its own pool. Either way
   this is a configuration decision, not a code change (see the checklist above) — nail it down
   early so `provideNationalWid()`'s `getToken()` contract in the Angular library, and the
   `Cognito:*` values in NC's deployment profile, are set correctly from the start.
3. **Fix the plaintext-SSM password issue** while you're already touching the deployment template
   for step 1.
4. **Write ingestor-level tests** for LAUS, CES, QCEW, OEWS, Projections, and WID Center Lookups
   before making further changes to them — right now regressions in the trickiest parts of the
   pipeline (multi-tier fallback logic, area-code resolution) would go undetected.
5. **Complete a real prod deployment** — Utah never did this; it's genuinely new ground, not a
   repeat of dev.
6. Everything else in [known-issues-and-gaps.md](./fed-national-wid/docs/known-issues-and-gaps.md).

---

## Where everything else lives

- [README.md](./README.md) — orientation and reading order for the rest of this folder.
- [fed-national-wid/](./fed-national-wid/) — the API + ingestion Lambda solution, with its own
  README and a `docs/` folder covering architecture, the ingestion pipeline, database schema,
  deployment/operations, testing, and known issues in depth.
- [ng-national-wid/](./ng-national-wid/) — the Angular data-explorer library, with its own README
  covering component architecture and a build guide.
- This repository's root `specs/wid-3.0/` and `docs/decisions/` — the WID 3.0 API contract Utah
  and NC (and other ARC states) collaborated on, which `fed-national-wid` implements. Keep these in
  sync per the cross-repo contract rules described in `fed-national-wid/AGENTS.md`.
