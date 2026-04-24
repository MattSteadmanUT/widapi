# WID Reference Documents

This directory holds official WID documentation used as reference material when
developing the WID 3.0 API specification.

## Documents

### WID 3.0 Structure — November 2025

**File:** `WID-3.0-Structure-20251120.pdf`  
**Source:** https://www.widcenter.org/wp-content/uploads/Reports/WID-3.0-Structure-20251120.pdf  
**Description:** The official WID 3.0 database structure specification as of November 2025.
Defines all core tables, lookup tables, crosswalk tables, and administrative tables required
of states receiving ETA funding. Includes full column definitions, data types, and key
relationships for each table.

### WID v3.0 Addenda

**File:** `WID-v30-Addenda.pdf`  
**Source:** https://www.widcenter.org/wp-content/uploads/Reports/WID-v30-Addenda.pdf  
**Description:** Addenda to the WID 3.0 base specification documenting changes, additions,
and clarifications since initial release. Includes the May 2025 update that added `stFips`
to the `CESCodes` table to handle definitional differences between state-level and
national-level CES series codes.

### Oregon WID API - Design Overview

**File:** `Oregon_WID_API_Design_Overview.md`  
**Source:** Oregon team (reference document prepared by Gary Sincick)  
**Description:** Summary of Oregon's WID API design philosophy, endpoint patterns,
response structure, and adaptation considerations for National WID 3.0.

## Location

Both PDF files are stored in `specs/wid-3.0/` alongside the draft API specification:

- `specs/wid-3.0/WID-3.0-Structure-20251120.pdf`
- `specs/wid-3.0/WID-v30-Addenda.pdf`

## Forum Discussion Sync Artifacts

Generated artifacts from external forum discussions are written to:

- `docs/reference-documents/forum/nationalwid-forum-sync.md`
- `specs/wid-3.0/supporting/forum/nationalwid-forum-normalized.json`

These are produced by running:

```bash
npm run sync:forum
```

The source forum lives in a private S3 location. Contributors without access do
not need to run this sync. If they have installed the optional pre-push hook, it
checks for AWS/S3 access first and exits without blocking the push when the
private source is unavailable.

The Markdown format is thread-oriented and shows which posts reply to which
parent posts.
