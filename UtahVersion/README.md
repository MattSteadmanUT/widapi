# UtahVersion

Everything Utah DWS built toward a national WID 3.0 implementation, handed off to North Carolina:
the API + BLS ingestion Lambda, and the Angular data-explorer component library. Both were built
against the WID 3.0 API contract defined in this repository's `specs/wid-3.0/`.

**Start with [HANDOFF.md](./HANDOFF.md)** — it's the single authoritative status report: what's
done, what's known-broken, what infrastructure/access NC needs to stand up, and recommended next
steps. Everything else in this folder is reference material HANDOFF.md points into as needed.

## What's here

```
UtahVersion/
├── HANDOFF.md                  ← read this first
├── fed-national-wid/           The .NET 8 API + BLS ingestion Lambda (AWS SAM/Lambda/Aurora)
│   ├── README.md                 API reference, quick start, endpoint catalog
│   └── docs/
│       ├── architecture.md               How the API's query/filter/sort/pagination/auth engine works, and why
│       ├── ingestion-pipeline.md         Per-ingestor deep dive: sources, quirks, scheduling, idempotency
│       ├── database-schema.md            Full table inventory + migration-by-migration history
│       ├── deployment-and-operations.md  AWS resources, justfile tasks, runbook
│       ├── testing.md                    What's tested, what isn't
│       ├── known-issues-and-gaps.md      The honest "what's not done" list
│       └── (dated session-note docs Utah wrote along the way, kept for historical context)
└── ng-national-wid/            The Angular 17+ data-explorer component library
    ├── README.md                 Component architecture — what it renders and how
    ├── INTEGRATION.md            How to install and configure it in a host Angular app
    └── docs/
        └── standalone-build-guide.md   How to build it outside Utah's original workspace
```

## Suggested reading order

1. [HANDOFF.md](./HANDOFF.md) — status, known issues, infra checklist, next steps.
2. [fed-national-wid/README.md](./fed-national-wid/README.md) — the API's own quick start and full
   endpoint reference. Skim the whole thing before diving into any one doc below.
3. [fed-national-wid/docs/architecture.md](./fed-national-wid/docs/architecture.md) — read this
   before changing any filtering, sorting, pagination, or auth code. It explains the shared engine
   all 11 controllers sit on top of.
4. [fed-national-wid/docs/ingestion-pipeline.md](./fed-national-wid/docs/ingestion-pipeline.md) —
   read this before touching any ingestor. Every BLS/WID Center source has quirks Utah already hit;
   this saves you from rediscovering them.
5. [fed-national-wid/docs/database-schema.md](./fed-national-wid/docs/database-schema.md) — the
   full table inventory, and the history of a few migrations that fixed earlier migrations'
   mistakes (worth knowing why, not just what).
6. [fed-national-wid/docs/deployment-and-operations.md](./fed-national-wid/docs/deployment-and-operations.md) —
   the runbook, once you're ready to actually deploy.
7. [ng-national-wid/README.md](./ng-national-wid/README.md) — the Angular library, if/when NC wires
   it into a portal.

## What isn't here

This folder is a clean copy of the working source and its documentation, not a git-history mirror
of the original repositories. The originals remain on GitHub for anyone who needs deeper commit
history:

- API + ingestion: `github.com/utahdws/fed-national-wid`
- Angular library (lives inside a larger workspace): `github.com/utahdws/fed-ulmita-ng`,
  at `projects/ng-national-wid/`

Debug/scratch artifacts (ad hoc Lambda invoke payloads, IDE cache files) and four overlapping
per-session handoff notes from the original repo were intentionally left out of this copy — their
substantive content is reconciled into [HANDOFF.md](./HANDOFF.md) and
[known-issues-and-gaps.md](./fed-national-wid/docs/known-issues-and-gaps.md) instead of carried
forward as separate, increasingly stale files.
