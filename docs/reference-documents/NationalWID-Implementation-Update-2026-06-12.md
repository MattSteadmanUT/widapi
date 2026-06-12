# National WID Implementation Status Update (Technical) - 2026-06-12

## Purpose
This is a self-contained stakeholder update for the National WID implementation.
It focuses on three questions:
1. What API and database capabilities are currently available?
2. What data has been propagated so far, and from which sources?
3. What remains before Phase-1 ingestion can be considered fully complete?

---

## Current Runtime and Access Status

Environment:
- AWS GovCloud dev environment is deployed and operational.
- API stack status is healthy (latest deploy completed successfully).
- API base URL (dev): https://uk6y6uz1il.execute-api.us-gov-west-1.amazonaws.com

API access model:
- Authentication uses ULMITA Cognito (JWT bearer token).
- `GET /health` is intentionally unauthenticated.
- Data endpoints require a valid token issued from the ULMITA user pool.
- State identity is represented in token claims (notably state FIPS) for access context.

API endpoint coverage currently implemented:
- `GET /labor-force`
- `GET /ces`
- `GET /industry`
- `GET /wages`
- `GET /projections`
- `GET /lookups/*`

Response model:
- Standardized envelope with metadata, records, and pagination links.
- Table-native endpoint design aligned to WID 3.0 table structure decisions.

---

## Database Status

Platform:
- Aurora PostgreSQL-backed WID database is running in dev.

Schema/migrations:
- Core schema and index migrations are applied.
- Lookup seed process is in place.
- Ingestion run logging is implemented (dataset-level counts and status tracking).

Core table intent in this phase:
- LaborForce
- CES
- Industry
- IOWage
- ProjectionsMatrix

---

## Data Propagation Status by Dataset and Source

## 1) LaborForce (`/labor-force`)
Source:
- BLS LAUS

Current state:
- National + state-level ingestion implemented and validated.

Latest validated evidence in this implementation workstream:
- 26,554 rows upserted.

## 2) CES (`/ces`)
Source:
- BLS CES

Current state:
- National + state-level ingestion implemented and validated.

Latest validated evidence in this implementation workstream:
- 31,579 rows upserted.

## 3) Industry (`/industry`, QCEW)
Source:
- BLS QCEW (CEW downloadable CSV slices)

Current state:
- Ingestion implementation was changed from a failing endpoint strategy to CEW CSV slices.
- National/state filtering and batched upsert are active.

Latest observed evidence:
- Repeated run logs show `QCEW: completed - 6480 rows upserted`.

## 4) IOWage (`/wages`, OEWS/OES)
Source:
- BLS OEWS downloadable ZIP/XLSX workbooks (`nat` and `st` files)

Current state:
- Source strategy migrated away from invalid time-series IDs to workbook-based ingestion.
- Parser and mapping are implemented and deployed.

Latest observed evidence:
- Recent runs still report `OES: completed - 0 rows upserted`.

Interpretation:
- Source integration is now in place, but final normalization logic (especially code/format
  handling from workbook fields) still needs one stabilization pass.

## 5) ProjectionsMatrix (`/projections`)
Source:
- Projections Central REST JSON feed

Current state:
- Projections ingestor is implemented and wired into orchestration.
- During stabilization, scope was constrained to all-occupations (`all/00-0000`) to keep runtime bounded.

Direct source probe result (endpoint behavior check):
- rows = 55
- total_pages = 1
- total_items = 55

Latest observed issue:
- Some validation windows included overlapping long-running invokes, making
  final single-run completion proof noisy.

Interpretation:
- Data path is implemented; clean authoritative final run evidence is pending.

---

## What Stakeholders Can Consider "Done" vs "In Progress"

Done (implemented and evidence-backed):
- API runtime deployed with auth model and table-native endpoints.
- Database schema/migrations operational in dev.
- LAUS and CES propagation at state+national scale.
- QCEW propagation producing non-zero upserts from BLS CEW CSV source.

In progress (last-mile stabilization):
- OEWS/OES row production (currently zero in latest observed runs).
- One clean, non-overlapping projections validation run with final count evidence.

---

## Source Lineage Summary (What data comes from where)

- LaborForce: BLS LAUS
- CES: BLS CES
- Industry: BLS QCEW CEW data (CSV slices)
- IOWage: BLS OEWS downloadable workbooks (ZIP/XLSX)
- ProjectionsMatrix: Projections Central REST JSON

This lineage is now directly represented in ingestion code paths for all five Phase-1 core domains.

---

## Immediate Next Technical Milestones
1. Run one controlled async ingestion execution (single invocation only).
2. Capture one authoritative completion summary for LAUS/CES/QCEW/OES/Projections.
3. Close OEWS normalization issue and verify non-zero `IOWage` inserts.
4. Execute endpoint smoke validation on `/industry`, `/wages`, and `/projections`.
5. Publish final Phase-1 ingestion-close update with stable counts.

---

## Planning Alignment Notes
- This implementation continues to align with WID 3.0 table-native endpoint design.
- It also aligns with earlier project discussion goals: centrally populated national/state data,
  API access for state systems, and support for CSV/structured downstream usage.
