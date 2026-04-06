# Field Mapping Notes

This directory tracks how fields from existing WID 2.8 implementations map (or don't map)
to the WID 3.0 draft spec.

## Format

Name files by domain area, e.g. `employer.md`, `job-seeker.md`, `placement.md`.

Each file should be a table of the form:

| WID 2.8 Field | State(s) | WID 3.0 Field | Notes / Breaking Change |
|---------------|----------|---------------|-------------------------|
| `employerName` | OR | `employer_name` | Renamed to snake_case |
| `empId`        | OR | TBD | Under discussion — see decisions/2026-04-06-employer-id.md |
