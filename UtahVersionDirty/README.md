# UtahVersionDirty

An unmodified snapshot of `fed-national-wid` and `ng-national-wid` exactly as they existed in
Utah's working repos at handoff time — no fixes, no cleanup, no added documentation, no removed
scratch files. This is "how it was actually running," kept for reference alongside the cleaned-up,
documented, audited version in [`../UtahVersion/`](../UtahVersion/).

The only things intentionally left out are build output (`bin/`, `obj/`, `.vs/`, `publish/`,
`node_modules/`), `.git` history, and — added after an initial pass — a handful of **live AWS
resource identifiers** for Utah's still-running dev environment: the VPC ID, subnet IDs, security
group ID, S3 deployment bucket name, and Aurora cluster endpoint hostnames, each replaced in place
with a `[REDACTED-LIVE-*]` marker (in `AGENTS.md`, `cloud-deployment/dev.deployment-profile.jsonc`,
`dev-client.html`, `README.md`, `scripts/sql/README.md`, and the `HANDOFF-*.md` files). These
aren't credentials — none grant access without separate IAM authentication — but since that
environment is confirmed still live, publishing exact coordinates for it in a permanently-public
repo serves no purpose NC needs and only adds reconnaissance value for anyone looking. Everything
else, including the ad hoc `ingest-*.json` debug files and the four overlapping `HANDOFF-*.md`
notes themselves, is untouched. Checked for hardcoded secrets before copying (AWS keys, private
keys, real passwords, account IDs) — found none; the only credential anywhere is a local-dev-only
placeholder (`postgres`/`postgres` against `localhost`) in
`fed-national-wid/src/NationalWid.Api/appsettings.Development.json`, which grants no access to
anything beyond a developer's own local database. Cognito user pool IDs and app client IDs were
*not* redacted — those aren't secrets (public OAuth/OIDC clients are designed to embed them) and
NC needs them if continuing to use Utah's shared ULMITA login.

**Use [`../UtahVersion/`](../UtahVersion/) for the actual handoff** — this folder is a historical
reference, not something to build against.
