# UtahVersionDirty

An unmodified snapshot of `fed-national-wid` and `ng-national-wid` exactly as they existed in
Utah's working repos at handoff time — no fixes, no cleanup, no added documentation, no removed
scratch files. This is "how it was actually running," kept for reference alongside the cleaned-up,
documented, audited version in [`../UtahVersion/`](../UtahVersion/).

The only thing intentionally left out is build output (`bin/`, `obj/`, `.vs/`, `publish/`,
`node_modules/`) and `.git` history — everything else, including the ad hoc `ingest-*.json` debug
files and all four overlapping `HANDOFF-*.md` notes, is here untouched. Checked for hardcoded
secrets before copying (AWS keys, private keys, real passwords) — found none; the only credential
present is a local-dev-only placeholder (`postgres`/`postgres` against `localhost`) in
`fed-national-wid/src/NationalWid.Api/appsettings.Development.json`, which grants no access to
anything beyond a developer's own local database.

**Use [`../UtahVersion/`](../UtahVersion/) for the actual handoff** — this folder is a historical
reference, not something to build against.
