# BLS CES Data Pipeline

This pipeline downloads Current Employment Statistics (CES) data from the Bureau of Labor Statistics (BLS) public flat files and loads it into the `ces` table in a SQL Server WID database.

## Components

| File | Purpose |
|---|---|
| `bls_get_flat_file.ps1` | Downloads a BLS flat file and bulk-loads it into the `blsseries` staging table |
| `powershell_loadCes.sql` | Stored procedure that transforms staged data into the `ces` table |
| `schema/blsseries.sql` | DDL for the staging table |
| `schema/import_history.sql` | DDL for the download history tracking table |

## Prerequisites

The following tables must exist before running the pipeline:

| Table | Source |
|---|---|
| `blsseries` | Create from `schema/blsseries.sql` (provided) |
| `import_history` | Create from `schema/import_history.sql` (provided) |
| `ces` | Standard WID table (not provided here) |
| `cescode` | Standard WID reference table (not provided here) |

The account running the pipeline must have Windows Integrated Security access to the WID database with permissions to truncate/insert on `blsseries`, insert/delete on `ces`, and read on `cescode`.

## How It Works

### Step 1 — Download (`bls_get_flat_file.ps1`)

The script accepts two parameters:

| Parameter | Default | Description |
|---|---|---|
| `-server` | `127.0.0.1` | SQL Server hostname or IP |
| `-url` | LAUS NC file | BLS flat file URL to download |

On each run, the script:
1. Connects to the WID database
2. Truncates `blsseries`
3. Checks `import_history` to find when this URL was last downloaded
4. Fetches the file's last-modified date from the BLS server
5. If the file is newer than the last recorded download, downloads it and bulk-loads it into `blsseries` in batches of 100,000 rows, converting BLS `"-"` (not available) values to NULL
6. Records the successful download in `import_history`

If the BLS file has not changed since the last run, the download is skipped entirely.

### Step 2 — Load (`powershell_loadCes` stored procedure)

The stored procedure:
1. Reads the first two characters of `series_id` from `blsseries` to determine whether the data is national CES (`CE`) or state/metro CES (`SM`)
2. Deletes existing rows in `ces` for the relevant geography and seasonal-adjustment type
3. Pivots the long-format BLS data (one row per series/period/statistic) into the wide-format `ces` table (one row per series/period with all statistics as columns)
4. Validates that every loaded series code exists in `cescode` — if any are missing, the procedure raises an error with the message `cescode records need to be added`

## BLS Data URLs

The CES pipeline requires two separate downloads run sequentially:

**National CES (CE series):**
```
https://download.bls.gov/pub/time.series/ce/ce.data.0.AllCESSeries
```

**State/Metro CES (SM series):**
```
https://download.bls.gov/pub/time.series/sm/sm.data.{fips}.{StateName}
```

Replace `{fips}` with your state's BLS FIPS code and `{StateName}` with the state name as it appears on the BLS server. Browse available files at:
```
https://download.bls.gov/pub/time.series/sm/
```

## SQL Server Agent Setup

The pipeline runs as three sequential steps in a SQL Server Agent job:

**Step 1 — Download national CES:**
```
powershell -file C:\path\to\bls_get_flat_file.ps1 -server YourServer -url "https://download.bls.gov/pub/time.series/ce/ce.data.0.AllCESSeries"
```

**Step 2 — Load national CES to database:**
```sql
EXEC powershell_loadCes
```

**Step 3 — Download state/metro CES:**
```
powershell -file C:\path\to\bls_get_flat_file.ps1 -server YourServer -url "https://download.bls.gov/pub/time.series/sm/sm.data.{fips}.{StateName}"
```

**Step 4 — Load state/metro CES to database:**
```sql
EXEC powershell_loadCes
```

Because `blsseries` is truncated on each run, the stored procedure must be executed after each individual download before the next download overwrites the staging table.

## Notes

- **BLS contact email:** The `-UserAgent` string in `bls_get_flat_file.ps1` must be updated to a valid contact email address. BLS requests this in the user agent for all automated downloads.
- **`cescode` validation:** If new industry series codes appear in a BLS release that are not yet in your `cescode` table, the stored procedure will raise an error after loading. The data is in `ces` at that point; you simply need to add the missing codes to `cescode` before the data is queryable through normal application paths.
- **Annual averages:** BLS period `M13` represents an annual average. The stored procedure maps this to `periodtype = '01'` and `period = '00'` in the `ces` table.
- **Employment units:** Employment values (`empces`, `empprodwrk`, `empfemale`) are stored in BLS flat files in thousands. The stored procedure multiplies these by 1,000 on load.
- **Preliminary data:** Rows where `footnote_codes = 'P'` are flagged as preliminary (`prelim = 1`) in the `ces` table and will be updated in a subsequent BLS release.
