# WID 3.0 SQL Scripts — For State Agencies

These scripts help state agencies create and populate their own WID 3.0 database from nationally-available public data.

---

## Database setup

### 1. Create schema

| Engine              | Script                          |
|---------------------|---------------------------------|
| PostgreSQL 14+      | `wid-30-schema-postgres.sql`    |
| MySQL 8 / MariaDB   | `wid-30-schema-mysql.sql`       |
| SQL Server 2016+    | `wid-30-schema-mssql.sql`       |

All scripts are idempotent and safe to re-run.

### 2. Seed national reference data

Run `wid-30-seed-national.sql` (PostgreSQL syntax, adapt for other engines) to populate:
- `geographies` — national record
- `statefips` — all states + DC
- `areatypes` — WID 3.0 standard codes
- `periodyears` — annual/monthly/quarterly rows 2000–present

---

## Pulling data from the National WID API

The National WID 3.0 API at `[REDACTED-LIVE-API-URL]` serves nationally-available BLS data. You can query it and insert the results into your state database.

### Available endpoints

| Table             | API endpoint              | Filter              |
|-------------------|---------------------------|---------------------|
| LaborForce        | `GET /labor-force`        | `stFips=<your FIPS>`|
| CES               | `GET /ces`                | `stFips=<your FIPS>`|
| Industry (QCEW)   | `GET /industry`           | `stFips=<your FIPS>`|
| IOWage (OES)      | `GET /wages`              | `stFips=<your FIPS>`|
| ProjectionsMatrix | `GET /projections`        | `stFips=<your FIPS>`|
| Geographies       | `GET /lookups/geographies`| `stFips=<your FIPS>`|

### Download full dataset as CSV

```
GET /labor-force?stFips=49&format=csv
GET /wages?stFips=49&format=xlsx
GET /projections?stFips=49&format=csv
```

Supported formats: `json`, `csv`, `tsv`, `psv`, `xlsx`

All endpoints require a valid ULMITA JWT token in the `Authorization: Bearer <token>` header.

---

## Area code conventions (WID 3.0)

The WID 3.0 area code is 7 characters:

| Level    | Area Type | Area Code Pattern         | Example              |
|----------|-----------|---------------------------|----------------------|
| National | `00`      | `0000000`                 | United States        |
| State    | `01`      | `{stFips}00000`           | `4900000` = Utah     |
| County   | `04`      | `{stFips}{county3}00`     | `4901100` = Davis UT |
| MSA/CBSA | `31`      | 6-digit CBSA padded to 7  | `0419900` = Ogden MSA|

---

## Data refresh schedule

BLS publishes data on the following approximate schedule:

| Dataset     | WID table         | BLS release cadence          |
|-------------|-------------------|------------------------------|
| LAUS        | `laborforce`      | 3rd/4th Friday of each month |
| CES         | `ces`             | 1st Friday of each month     |
| QCEW        | `industry`        | ~5 months after quarter end  |
| OEWS        | `iowage`          | May (annual)                 |
| Projections | `projectionsmatrix`| Annual                      |

The National WID API checks for updated data daily (Mon–Fri) and re-processes sources when the BLS file hash changes. Your state data refresh should be coordinated with these release cycles.

---

## Populating state-specific data

For data NOT available from the national API (sub-state QCEW detail, state-specific projections, licensing), your state BLS data extracts should be loaded directly using the table schemas above.

Use the `ON CONFLICT ... DO UPDATE SET` pattern for idempotent upserts:

```sql
INSERT INTO laborforce (stfips, areatype, areatypeversion, area, ...)
VALUES ('49', '01', '0', '4900000', ...)
ON CONFLICT (stfips, areatype, areatypeversion, area, periodyear, periodtype, period, adjusted)
DO UPDATE SET
    laborforce = EXCLUDED.laborforce,
    employed   = EXCLUDED.employed,
    unemployed = EXCLUDED.unemployed,
    unemprate  = EXCLUDED.unemprate;
```
