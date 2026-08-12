#!/usr/bin/env python3
"""
generate-license-migration.py

Downloads all per-state WID 2.8 license exports from data.widcenter.org,
extracts flag codes for every state, then combines with the national
COSFlatExport to build a complete PostgreSQL migration file.

Requires: pyodbc, requests, 64-bit Microsoft Access ODBC driver (*.mdb, *.accdb)
Run on Windows where the Access ODBC provider is available.

Usage:
  python generate-license-migration.py [--output <path>] [--cache-dir <path>]

Flags default to 9 (undetermined) only for states that have no per-state file.
"""

import argparse
import html.parser
import io
import os
import re
import sys
import zipfile
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import Optional

try:
    import pyodbc
except ImportError:
    sys.exit("pyodbc is required. Run: pip install pyodbc")

try:
    import requests
except ImportError:
    sys.exit("requests is required. Run: pip install requests")

BASE_URL = "https://data.widcenter.org/wfinfodb/License/"
COS_FLAT_EXPORT_URL = "https://data.widcenter.org/wfinfodb/License/COSFlatExport.zip"
USER_AGENT = "NationalWid-DataLoader/1.0 (utah.gov)"


class _LinkParser(html.parser.HTMLParser):
    def __init__(self):
        super().__init__()
        self.links = []

    def handle_starttag(self, tag, attrs):
        if tag == "a":
            for name, val in attrs:
                if name == "href" and val:
                    self.links.append(val)


def discover_per_state_files():
    """Fetch the directory listing and return stfips -> full URL (most recent per state)."""
    print(f"Fetching directory listing from {BASE_URL} ...")
    resp = requests.get(BASE_URL, headers={"User-Agent": USER_AGENT}, timeout=30)
    resp.raise_for_status()

    parser = _LinkParser()
    parser.feed(resp.text)

    pattern = re.compile(
        r"/wfinfodb/License/(WID28LicenseST(\d{2})Export(\d+)\.(?:mdb|zip|mdb\.zip))$",
        re.IGNORECASE,
    )

    best = {}
    for href in parser.links:
        m = pattern.match(href)
        if not m:
            continue
        stfips   = m.group(2).zfill(2)
        date_int = int(m.group(3))
        full_url = f"https://data.widcenter.org{href}"
        existing = best.get(stfips)
        if existing is None or date_int > existing[0]:
            best[stfips] = (date_int, m.group(1), full_url)

    result = {st: info[2] for st, info in sorted(best.items())}
    print(f"Found {len(result)} per-state files.")
    return result


def _download(url, cache_path):
    if os.path.exists(cache_path):
        with open(cache_path, "rb") as f:
            return f.read()
    resp = requests.get(url, headers={"User-Agent": USER_AGENT}, timeout=300, stream=True)
    resp.raise_for_status()
    data = resp.content
    with open(cache_path, "wb") as f:
        f.write(data)
    return data


def _extract_mdb_from_bytes(data, original_url):
    url_lower = original_url.lower()
    if not (url_lower.endswith(".zip") or url_lower.endswith(".mdb.zip")):
        return data
    with zipfile.ZipFile(io.BytesIO(data)) as zf:
        mdb_names = [n for n in zf.namelist() if n.lower().endswith(".mdb")]
        if not mdb_names:
            raise ValueError(f"No .mdb found in zip from {original_url}")
        with zf.open(mdb_names[0]) as mf:
            return mf.read()


def open_mdb(path):
    conn_str = (
        "DRIVER={Microsoft Access Driver (*.mdb, *.accdb)};"
        f"DBQ={path};"
        "ExtendedAnsiSQL=1;"
    )
    return pyodbc.connect(conn_str)


def iter_rows(conn, sql):
    cursor = conn.cursor()
    cursor.execute(sql)
    col_names = [d[0].lower() for d in cursor.description]
    for row in cursor.fetchall():
        yield dict(zip(col_names, row))


def load_flags_from_mdb(mdb_path):
    """Extract WID 2.8 flag codes from a per-state MDB. Returns {(stfips, licenseid): flags}."""
    flags = {}
    conn = open_mdb(mdb_path)
    try:
        for row in iter_rows(conn, "SELECT * FROM [license]"):
            stfips    = (row.get("stfips") or "").strip().zfill(2)
            licenseid = (row.get("licenseid") or "").strip()[:10]
            if not stfips or not licenseid:
                continue
            flags[(stfips, licenseid)] = {
                "licensetype":   (row.get("licensetype")   or "9").strip()[:1],
                "exam":          (row.get("exam")          or "9").strip()[:1],
                "education":     (row.get("education")     or "9").strip()[:1],
                "continuingedu": (row.get("continuingedu") or "9").strip()[:1],
                "certification": (row.get("certification") or "9").strip()[:1],
                "experience":    (row.get("experience")    or "9").strip()[:1],
                "criminal":      (row.get("criminal")      or "9").strip()[:1],
                "physicalreq":   (row.get("physical")      or "9").strip()[:1],
                "veteran":       (row.get("veteran")       or "9").strip()[:1],
                "inactive":      (row.get("inactive")      or "0").strip()[:1],
            }
    finally:
        conn.close()
    return flags


def extract_all(cos_mdb_path, per_state_flags):
    """Extract license, licenseauthorities, and licensexocc from COSFlatExport."""
    conn = open_mdb(cos_mdb_path)
    flat_rows = list(iter_rows(conn, "SELECT * FROM [FlatExport]"))
    conn.close()

    license_seen = {}
    auth_seen    = {}
    licxocc_seen = set()
    licxocc_rows = []

    for row in flat_rows:
        stfips    = (row.get("stfips")    or "").strip().zfill(2)
        licenseid = (row.get("licenseid") or "").strip()[:10]
        licauthid = (row.get("licauthid") or "").strip()[:3]
        occcode   = (row.get("occcode")   or "").strip()[:10]

        if not stfips or not licenseid:
            continue

        lic_key = (stfips, licenseid)
        if lic_key not in license_seen:
            flags = per_state_flags.get(lic_key, {})
            license_seen[lic_key] = {
                "stfips":        stfips,
                "areatype":      "01",
                "licenseid":     licenseid,
                "licauthid":     licauthid or None,
                "licensetitle":  (row.get("lictitle") or "").strip()[:240] or None,
                "licensedesc":   (row.get("licdesc")  or "").strip() or None,
                "licensetype":   flags.get("licensetype",   "9"),
                "exam":          flags.get("exam",          "9"),
                "education":     flags.get("education",     "9"),
                "continuingedu": flags.get("continuingedu", "9"),
                "certification": flags.get("certification", "9"),
                "experience":    flags.get("experience",    "9"),
                "criminal":      flags.get("criminal",      "9"),
                "physicalreq":   flags.get("physicalreq",   "9"),
                "veteran":       flags.get("veteran",       "9"),
                "inactive":      flags.get("inactive",      "0"),
                "licenseurl":    (row.get("licenseurl")    or "").strip()[:500] or None,
                "licenseupdated":(row.get("licenseupdated") or "").strip()[:8] or None,
            }

        if licauthid:
            auth_key = (stfips, licauthid)
            if auth_key not in auth_seen:
                auth_seen[auth_key] = {
                    "stfips":          stfips,
                    "areatype":        "01",
                    "areatypeversion": "0",
                    "area":            "000000",
                    "licauthid":       licauthid,
                    "department":      (row.get("department") or "").strip()[:255] or None,
                    "division":        (row.get("division")   or "").strip()[:255] or None,
                    "board":           (row.get("board")      or "").strip()[:255] or None,
                    "address1":        (row.get("address1")   or "").strip()[:75]  or None,
                    "city":            (row.get("city")       or "").strip()[:30]  or None,
                    "state":           (row.get("st")         or "").strip()[:2]   or None,
                    "zipcode":         (row.get("zip")        or "").strip()[:5]   or None,
                    "telephone":       (row.get("telephone")  or "").strip()[:10]  or None,
                    "email":           None,
                    "url":             (row.get("licauthurl") or "").strip()[:200] or None,
                }

        if occcode:
            xocc_key = (stfips, licenseid, "21", occcode)
            if xocc_key not in licxocc_seen:
                licxocc_seen.add(xocc_key)
                licxocc_rows.append({
                    "stfips":      stfips,
                    "licenseid":   licenseid,
                    "occcodetype": "21",
                    "occcode":     occcode,
                })

    return list(license_seen.values()), list(auth_seen.values()), licxocc_rows


def sql_str(val, max_len=0):
    if val is None:
        return "NULL"
    val = str(val).strip()
    if not val:
        return "NULL"
    if max_len and len(val) > max_len:
        val = val[:max_len]
    return "'" + val.replace("'", "''") + "'"


def write_license_sql(rows, out):
    out.write("-- ---------------------------------------------------------------------------\n")
    out.write(f"-- license: {len(rows):,} rows\n")
    out.write("-- ---------------------------------------------------------------------------\n\n")
    out.write("INSERT INTO license (\n")
    out.write("    stfips, areatype, licenseid, licauthid,\n")
    out.write("    licensetitle, licensedesc,\n")
    out.write("    licensetype, exam, education, continuingedu, certification,\n")
    out.write("    experience, criminal, physicalreq, veteran, inactive,\n")
    out.write("    licenseurl, licenseupdated\n")
    out.write(") VALUES\n")
    lines = []
    for r in rows:
        lines.append(
            f"    ({sql_str(r['stfips'])}, {sql_str(r['areatype'])}, {sql_str(r['licenseid'])}, "
            f"{sql_str(r['licauthid'])},\n"
            f"     {sql_str(r['licensetitle'], 240)}, {sql_str(r['licensedesc'])},\n"
            f"     {sql_str(r['licensetype'])}, {sql_str(r['exam'])}, {sql_str(r['education'])}, "
            f"{sql_str(r['continuingedu'])}, {sql_str(r['certification'])},\n"
            f"     {sql_str(r['experience'])}, {sql_str(r['criminal'])}, "
            f"{sql_str(r['physicalreq'])}, {sql_str(r['veteran'])}, {sql_str(r['inactive'])},\n"
            f"     {sql_str(r['licenseurl'], 500)}, {sql_str(r['licenseupdated'])})"
        )
    out.write(",\n".join(lines))
    out.write("\nON CONFLICT (stfips, licenseid) DO UPDATE SET\n")
    out.write("    licauthid       = EXCLUDED.licauthid,\n")
    out.write("    licensetitle    = EXCLUDED.licensetitle,\n")
    out.write("    licensedesc     = EXCLUDED.licensedesc,\n")
    out.write("    licensetype     = EXCLUDED.licensetype,\n")
    out.write("    exam            = EXCLUDED.exam,\n")
    out.write("    education       = EXCLUDED.education,\n")
    out.write("    continuingedu   = EXCLUDED.continuingedu,\n")
    out.write("    certification   = EXCLUDED.certification,\n")
    out.write("    experience      = EXCLUDED.experience,\n")
    out.write("    criminal        = EXCLUDED.criminal,\n")
    out.write("    physicalreq     = EXCLUDED.physicalreq,\n")
    out.write("    veteran         = EXCLUDED.veteran,\n")
    out.write("    inactive        = EXCLUDED.inactive,\n")
    out.write("    licenseurl      = EXCLUDED.licenseurl,\n")
    out.write("    licenseupdated  = EXCLUDED.licenseupdated;\n\n")


def write_auth_sql(rows, out):
    out.write("-- ---------------------------------------------------------------------------\n")
    out.write(f"-- licenseauthorities: {len(rows):,} rows\n")
    out.write("-- ---------------------------------------------------------------------------\n\n")
    out.write("INSERT INTO licenseauthorities (\n")
    out.write("    stfips, areatype, areatypeversion, area, licauthid,\n")
    out.write("    department, division, board, address1, city,\n")
    out.write("    state, zipcode, telephone, email, url\n")
    out.write(") VALUES\n")
    lines = []
    for r in rows:
        lines.append(
            f"    ({sql_str(r['stfips'])}, {sql_str(r['areatype'])}, {sql_str(r['areatypeversion'])}, "
            f"{sql_str(r['area'])}, {sql_str(r['licauthid'])},\n"
            f"     {sql_str(r['department'], 255)}, {sql_str(r['division'], 255)}, "
            f"{sql_str(r['board'], 255)}, {sql_str(r['address1'], 75)}, {sql_str(r['city'], 30)},\n"
            f"     {sql_str(r['state'])}, {sql_str(r['zipcode'])}, {sql_str(r['telephone'])}, "
            f"{sql_str(r['email'])}, {sql_str(r['url'], 200)})"
        )
    out.write(",\n".join(lines))
    out.write("\nON CONFLICT (stfips, areatype, areatypeversion, area, licauthid) DO UPDATE SET\n")
    out.write("    department = EXCLUDED.department,\n")
    out.write("    division   = EXCLUDED.division,\n")
    out.write("    board      = EXCLUDED.board,\n")
    out.write("    address1   = EXCLUDED.address1,\n")
    out.write("    city       = EXCLUDED.city,\n")
    out.write("    state      = EXCLUDED.state,\n")
    out.write("    zipcode    = EXCLUDED.zipcode,\n")
    out.write("    telephone  = EXCLUDED.telephone,\n")
    out.write("    email      = EXCLUDED.email,\n")
    out.write("    url        = EXCLUDED.url;\n\n")


def write_licxocc_sql(rows, out):
    out.write("-- ---------------------------------------------------------------------------\n")
    out.write(f"-- licensexocc: {len(rows):,} rows\n")
    out.write("-- ---------------------------------------------------------------------------\n\n")
    out.write("INSERT INTO licensexocc (stfips, licenseid, occcodetype, occcode)\n")
    out.write("VALUES\n")
    lines = []
    for r in rows:
        lines.append(
            f"    ({sql_str(r['stfips'])}, {sql_str(r['licenseid'])}, "
            f"{sql_str(r['occcodetype'])}, {sql_str(r['occcode'])})"
        )
    out.write(",\n".join(lines))
    out.write("\nON CONFLICT (stfips, licenseid, occcodetype, occcode) DO NOTHING;\n\n")


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument(
        "--cache-dir",
        default=r"C:\Users\mattsteadman\AppData\Local\Temp\wid-license",
    )
    parser.add_argument("--output", default=None)
    parser.add_argument("--workers", type=int, default=4)
    args = parser.parse_args()

    os.makedirs(args.cache_dir, exist_ok=True)

    if args.output is None:
        script_dir  = os.path.dirname(os.path.abspath(__file__))
        repo_root   = os.path.dirname(script_dir)
        args.output = os.path.join(
            repo_root, "cloud-deployment", "migrations", "015_license_data.sql"
        )

    # Step 1: COSFlatExport
    cos_mdb_path = os.path.join(args.cache_dir, "COSFlatExport.mdb")
    if not os.path.exists(cos_mdb_path):
        print("Downloading COSFlatExport.zip ...")
        cos_zip = _download(COS_FLAT_EXPORT_URL, os.path.join(args.cache_dir, "COSFlatExport.zip"))
        cos_mdb_bytes = _extract_mdb_from_bytes(cos_zip, COS_FLAT_EXPORT_URL)
        with open(cos_mdb_path, "wb") as f:
            f.write(cos_mdb_bytes)
        print(f"  Extracted COSFlatExport.mdb ({len(cos_mdb_bytes)/1024/1024:.1f} MB)")
    else:
        print("Using cached COSFlatExport.mdb")

    # Step 2: download all per-state files
    state_urls = discover_per_state_files()

    def fetch_state(stfips, url):
        mdb_path = os.path.join(args.cache_dir, f"ST{stfips}.mdb")
        if os.path.exists(mdb_path):
            return stfips, -1
        ext = ".zip" if ".zip" in url.lower() else ".mdb"
        cache_path = os.path.join(args.cache_dir, f"ST{stfips}_raw{ext}")
        raw_bytes  = _download(url, cache_path)
        mdb_bytes  = _extract_mdb_from_bytes(raw_bytes, url)
        with open(mdb_path, "wb") as f:
            f.write(mdb_bytes)
        return stfips, len(mdb_bytes)

    print(f"\nDownloading {len(state_urls)} per-state files with {args.workers} workers...")
    with ThreadPoolExecutor(max_workers=args.workers) as pool:
        futures = {pool.submit(fetch_state, st, url): st for st, url in state_urls.items()}
        for future in as_completed(futures):
            stfips = futures[future]
            try:
                st, size = future.result()
                if size == -1:
                    print(f"  ST{st}: cached")
                else:
                    print(f"  ST{st}: {size/1024/1024:.1f} MB")
            except Exception as exc:
                print(f"  ST{stfips}: ERROR - {exc}", file=sys.stderr)

    # Step 3: load all per-state flags
    print("\nLoading flag codes from all per-state MDB files...")
    all_flags = {}
    states_loaded = 0
    for stfips in sorted(state_urls.keys()):
        mdb_path = os.path.join(args.cache_dir, f"ST{stfips}.mdb")
        if not os.path.exists(mdb_path):
            print(f"  ST{stfips}: not found, flags will be undetermined", file=sys.stderr)
            continue
        try:
            flags = load_flags_from_mdb(mdb_path)
            all_flags.update(flags)
            states_loaded += 1
            print(f"  ST{stfips}: {len(flags)} license flag records")
        except Exception as exc:
            print(f"  ST{stfips}: could not read - {exc}", file=sys.stderr)

    print(f"  Loaded flags for {states_loaded} states ({len(all_flags):,} total flag records)")

    # Step 4: extract from COSFlatExport
    print("\nExtracting from COSFlatExport (all states)...")
    license_rows, auth_rows, licxocc_rows = extract_all(cos_mdb_path, all_flags)
    flagged = sum(1 for r in license_rows if r["exam"] != "9")
    print(f"  license:             {len(license_rows):,} rows")
    print(f"  licenseauthorities:  {len(auth_rows):,} rows")
    print(f"  licensexocc:         {len(licxocc_rows):,} rows")
    print(f"  licenses with precise flags: {flagged:,} / {len(license_rows):,}")

    # Step 5: write migration
    print(f"\nWriting migration to: {args.output}")
    os.makedirs(os.path.dirname(args.output), exist_ok=True)

    with open(args.output, "w", encoding="utf-8") as f:
        f.write("-- =============================================================================\n")
        f.write("-- 015_license_data.sql\n")
        f.write("--\n")
        f.write("-- Seeds occupational license data from WID Center data.widcenter.org.\n")
        f.write("-- Source: COSFlatExport.mdb (CareerOneStop flat export, all 57 states/territories)\n")
        f.write("--         WID28LicenseST*.mdb (per-state WID 2.8 exports for flag code detail)\n")
        f.write("--\n")
        f.write("-- Flag codes use WID 2.8 standard:\n")
        f.write("--   0=not required, 1/2/3=type-specific, 9=undetermined\n")
        f.write(f"-- Per-state files loaded: {states_loaded}\n")
        f.write(f"-- Total rows: {len(license_rows) + len(auth_rows) + len(licxocc_rows):,}\n")
        f.write("-- =============================================================================\n\n")
        f.write("ALTER TABLE license ADD COLUMN IF NOT EXISTS areatype char(2);\n\n")
        write_license_sql(license_rows, f)
        write_auth_sql(auth_rows, f)
        write_licxocc_sql(licxocc_rows, f)

    size_kb = os.path.getsize(args.output) / 1024
    total = len(license_rows) + len(auth_rows) + len(licxocc_rows)
    print(f"Done. {size_kb:,.0f} KB  |  {total:,} total rows")


if __name__ == "__main__":
    main()
