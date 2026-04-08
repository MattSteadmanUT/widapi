# Oregon WID API — Design Overview

> **Purpose:** Reference document for the National WID API discussion.  
> **Source:** Oregon WID API specification (WID 2.8 · OpenAPI 3.0), Design Goals document (April 2019), and ARC presentation (November 2023).  
> **License:** Apache 2.0  
> **Note:** The Oregon API will need to be updated to align with WID 3.0 naming conventions and schema structure before adoption or adaptation for national use.

---

## Table of Contents

1. [Background & Purpose](#1-background--purpose)
2. [Design Philosophy](#2-design-philosophy)
3. [Formal Design Goals](#3-formal-design-goals)
4. [Data Scope](#4-data-scope)
5. [URL & Endpoint Structure](#5-url--endpoint-structure)
6. [Core API Functions](#6-core-api-functions)
7. [Response Format](#7-response-format)
8. [Validation & Error Handling](#8-validation--error-handling)
9. [Naming Conventions](#9-naming-conventions)
10. [Parameter Reference](#10-parameter-reference)
11. [Open Questions for National Adoption](#11-open-questions-for-national-adoption)
12. [Adaptation Notes for National WID 3.0](#12-adaptation-notes-for-national-wid-30)

---

## 1. Background & Purpose

Oregon developed a REST API specification for version 2.8 of the Workforce Information Database (WID) approximately eight years ago. The specification was written in OpenAPI 3.0 format, is licensed under Apache 2.0, and was formally presented to the ARC consortium in November 2023 as a candidate foundation for a national WID API.

The core motivation: just as the WID database provides a **standard structure** for storing labor market information across states, a WID API would provide a **standard interface** for accessing that information in a technology-neutral way. Because implementations sharing a common interface use the same endpoints, parameter names, and response schemas, LMI applications built on top of the API become significantly more portable — both across technology platforms and between states.

The OpenAPI specification is published at:  
[https://app.swaggerhub.com/apis/arc-wid/Workforce-Information-Database/1.0.0-oas3](https://app.swaggerhub.com/apis/arc-wid/Workforce-Information-Database/1.0.0-oas3)

---

## 2. Design Philosophy

### Data-Driven vs. Application-Driven

The WID API is intentionally **data-driven** rather than application-driven.

| Approach | Description | Example |
|---|---|---|
| **Application-driven** | API is tightly bound to a specific tool; minimal development effort to replicate that tool, but few uses outside it | CareerOneStop Skill Matcher API |
| **Data-driven** | Maximum flexibility in accessing underlying data; supports a wide range of applications, but the developer assembles what they need | BLS API, Census API |

The WID API follows the data-driven model: it provides flexible, fine-grained access to WID data without presupposing any particular application. This is a deliberate trade-off — a more versatile API may require several calls to assemble a complete dataset, but it can be tailored to the exact needs of each developer. The API may eventually include application-driven features, but the initial specification focuses on data access.

### Data and Lookups Are Kept Separate

The Oregon API **specification** states that lookup values (names, titles, codes) are not automatically included in data responses — lookup operations are kept separate from data operations, mirroring the WID database's own separation of data tables from lookup tables.

**Rationale from the spec:** Different WID data tables have varying numbers of foreign-key lookup relationships. Rather than making ad-hoc decisions per table about which lookups to include inline, the API is consistent: data returns codes, and a separate lookup call returns the titles. This is often actually more convenient for developers, who typically store lookup values in a client-side hash table and access them as needed. To ease this pattern, every data response includes **links** pointing to the relevant lookup endpoints pre-populated with the same query parameters.

**However — spec vs. implementation divergence:** The Oregon API implementation diverged from this principle in practice. For example, the `cesEmployment` endpoint returns `industrySeriesTitle` inline alongside the series code, even though the spec calls for separation. This suggests the "data only" approach created enough friction for real users that it was quietly overridden. **This is an open design question for the national API** — see Section 11.

---

## 3. Formal Design Goals

These five goals were established in the April 2019 Design Goals document and guide all specification decisions.

### Leverage the WID Structure
The API should leverage the significant existing work in the WID database structure. Table names, field names, and overall organization serve as the primary model for the API interface — making it familiar to WID users. However, the API need not adhere absolutely to the database structure where it creates friction. For example, naming conventions that would require breaking changes in the database can be improved in the API layer instead, without disturbing the underlying database contract.

### Versatility
The API provides fine-grained access that gives developers the flexibility to build a wide range of applications. This means individual atomic data access operations rather than bundled compound responses. There is a recognized trade-off between versatility and efficiency, and the specification accepts that trade-off in favor of flexibility.

### Consistency
Patterns observed in one part of the API should appear throughout the API. URL paths, parameter names, and available methods should be uniform across all resources. **Functional consistency** is equally important: if one time-series table offers a `maxPeriod` operation, all time-series tables should offer the same operation.

### Clarity
LMI is a technical domain, and some jargon is unavoidable. However, the API should not introduce additional non-intuitive language. Paths, operations, parameters, and response payloads should be comprehensible to anyone familiar with labor market information — including analysts and members of the public, not just DBAs.

### Stability
Once an API is released publicly, breaking changes cause significant downstream disruption. Path structure and naming conventions are the hardest things to change after release. These decisions demand the most careful attention during design, even if they seem less important than functional features. The API must be designed to be **extended without breaking existing operations**.

---

## 4. Data Scope

### Core Data Tables

| WID Table | API Entity | Description |
|---|---|---|
| `labforce` | `laborForce` | LAUS labor force & unemployment statistics |
| `ces` | `cesEmployment` / `cesHoursEarnings` | Current Employment Statistics — split into two endpoints (employment; hours & earnings are logically distinct entities sharing a table) |
| `iomatrix` | `ioMatrix` | Industry & occupational employment projections |
| `iowage` | `wages` | Occupational wage data (OES/OEWS) |
| `industry` | `industry` | QCEW industry employment & payroll data |

### Lookup Tables

| WID Table | API Method | Description |
|---|---|---|
| `geog` | `listAreas` | Geographic area lookup |
| `occdir` | `listOccupations` | IOMatrix occupational code lookup |
| `inddir` | `listIndustries` | IOMatrix industry code lookup |
| `cescode` | `listIndustrySeries` | CES industry series code lookup |
| `indcodes` | `listIndustries` | QCEW NAICS industry code lookup |
| `period` | `listYears` / `minPeriod` / `maxPeriod` | Period reference |

### Out of Scope (current spec)
- Security and authentication — requirements vary by implementation (internal service vs. public web service); intentionally deferred to implementers
- Non-primary-key field filtering (e.g., filtering by benchmark year) — reserved for future versions
- Substate geography (county, MSA) is supported in the Oregon implementation; **Phase 1 of national WID defers substate to a later phase**

---

## 5. URL & Endpoint Structure

### Path Pattern

```
https://{server}/{app-name}/{services}/wid/{category}/{resource}/{method}?{params}
```

| Segment | Role |
|---|---|
| `wid` | Top-level namespace. Separates WID endpoints from other service types (e.g., a future `wid-app` application tier). |
| `{category}` | One of: `data` (primary data tables), `lookup` (reference tables), `crosswalk` (future cross-table mappings) |
| `{resource}` | The WID entity being queried — maps to a WID table |
| `{method}` | Optional sub-action: `listAreas`, `listIndustrySeries`, `maxPeriod`, `minPeriod`, etc. |

> **Note on versioning:** The April 2019 design document included an explicit version segment (`/wid/v1.0/`). This was dropped from the OpenAPI spec. Versioning strategy should be revisited for the national API.

### Example Endpoints

```
GET /wid/data/cesEmployment
GET /wid/data/cesEmployment/listAreas
GET /wid/data/cesEmployment/listIndustrySeries
GET /wid/data/cesEmployment/maxPeriod
GET /wid/data/cesEmployment/minPeriod
GET /wid/data/laborForce
GET /wid/data/laborForce/listAreas
GET /wid/data/ioMatrix
GET /wid/data/ioMatrix/listIndustries
GET /wid/data/ioMatrix/listOccupations
GET /wid/data/industry
GET /wid/data/industry/listAreas
GET /wid/data/industry/listIndustries
GET /wid/data/wages
GET /wid/data/wages/listAreas
GET /wid/data/wages/listOccupations
```

All requests use **HTTP GET**. Responses are available as `application/json` or `text/csv`.

---

## 6. Core API Functions

### 6.1 Primary Key Filtering

Every WID data entity can be queried by any combination of its primary key fields. No parameters are required — omitting a field means no filter is applied for it, mirroring database query semantics:

| Parameters supplied | Result |
|---|---|
| None | Entire table (subject to pagination) |
| Some | Filtered result set |
| All | At most one record |

**Examples (LaborForce):**
```
# One specific record
/wid/data/laborForce?stateFips=41&areaType=21&area=038900&year=2019&periodType=03&period=01&seasonallyAdjusted=true

# All areas in Oregon, January 2019, seasonally adjusted
/wid/data/laborForce?stateFips=41&year=2019&periodType=03&period=01&seasonallyAdjusted=true

# Everything for Oregon (all years, all areas)
/wid/data/laborForce?stateFips=41
```

### 6.2 Year Range Filtering

For time-series tables, `minYear` and `maxYear` parameters are available alongside the exact-match `year` parameter. This allows a range of years to be returned in a single request without pulling the full table.

```
/wid/data/cesEmployment?stateFips=41&minYear=2019&maxYear=2023
```

### 6.3 Lookup Sub-Endpoints

Each data endpoint has corresponding `list*` sub-endpoints that return the valid lookup values (areas, series codes, industry codes, etc.) applicable to the data after filters are applied. These sub-endpoints accept the same parameters as the parent endpoint and return only values present in the filtered dataset.

**Naming convention:** `list` + lookup entity name.

```
# CES series codes active in the Portland MSA
/wid/data/cesEmployment/listIndustrySeries?stateFips=41&areaType=21&area=0038900

# Areas for which QCEW data exists
/wid/data/industry/listAreas?stateFips=41
```

When a data response does not include lookup values inline, the `links` array in the response provides pre-populated URLs to the relevant lookup sub-endpoints.

### 6.4 Min/Max Period Sub-Endpoints

All time-series endpoints offer `minPeriod` and `maxPeriod` sub-endpoints. These return the earliest and most recent periods for which data exists after filters are applied. The response is a **Period object** (not the data itself), making it easy to determine data currency before fetching a full dataset.

```
# Most recent period for CES data in any Oregon MSA
/wid/data/cesEmployment/maxPeriod?stateFips=41&areaType=21

# Oldest available period for statewide labor force data
/wid/data/laborForce/minPeriod?stateFips=41&areaType=01
```

---

## 7. Response Format

### 7.1 JSON Structure

Every successful JSON response has three top-level fields:

```json
{
  "meta": {},
  "data": [],
  "links": []
}
```

#### `meta` — Metadata Object

| Field | Type | Description |
|---|---|---|
| `widVersion` | string | Version of the WID database (e.g., `"2.8"`) |
| `apiVersion` | string | Version of the API specification |
| `totalCount` | integer | Total records matching the request (may exceed current page) |
| `pageNum` | integer | Current page number |
| `pageSize` | integer | Maximum records returned in this response |
| `hasMore` | boolean | Whether additional pages exist |
| `dataSource` | string | Identifies the data source |

#### `data` — Data Array

Each element is a typed record object:

```json
{
  "type": "cesEmployment",
  "id": "41-04-000001-2019-03-01-00000000-0",
  "attributes": {
    "stateFips": "41",
    "areaType": "01",
    "area": "000001",
    "areaId": "41-01-000001",
    "year": "2018",
    "periodType": "03",
    "period": "01",
    "industrySeriesCode": "00000000",
    "seasonallyAdjusted": true,
    "benchmarkYear": "2017",
    "preliminary": "0",
    "employment": 123456
  }
}
```

- **`type`** — the resource name from the URL path (e.g., `"cesEmployment"`)
- **`id`** — the complete compound primary key, dash-delimited
- **`attributes`** — all data fields, plus **concatenated lookup ID fields** (e.g., `areaId`) where the underlying lookup table has a compound primary key; these composite IDs allow efficient client-side lookup without additional API calls

#### `links` — Link Array

Each link object has:

| Field | Description |
|---|---|
| `href` | The endpoint URL |
| `rel` | Link relationship: `next`, `prev`, or a lookup relationship |
| `type` | HTTP method — always `GET` in the Oregon spec. Since the WID API is read-only and every link will always be `GET`, this field carries no information and **should be dropped from the national API spec**. It is inherited from general hypermedia conventions (HAL, JSON:API) where links can reference mutations; it has no practical value in a data-access-only API.

Links are included when:
- Additional pages exist → `rel: "next"` link with next page URL
- Previous pages are available → `rel: "prev"` link
- Related lookup values are available → link to the relevant `list*` sub-endpoint, pre-populated with the same query parameters used in the data request

**Pagination traversal example (Python):**
```python
def load_all_data(url):
    payload = json.load(urlopen(url))
    for item in payload['data']:
        insert_values.append(create_tuple(item['attributes']))
    db_write(insert_values)
    for link in payload['links']:
        if link['rel'] == 'next':
            load_all_data(link['href'])
```

### 7.2 CSV Structure

The CSV format returns **data attributes only** — no metadata, no links, no `type` or `id` wrappers. The first row is a comma-delimited header of field names, followed by one data row per record. All string values are enclosed in double quotes.

Field order is consistent with the `attributes` object in the JSON format, and both formats mirror the field order in the underlying database table.

```csv
"stateFips","areaType","area","year","periodType","period","industrySeriesCode","seasonallyAdjusted","benchmarkYear","preliminary","employment","areaId"
"41","01","000000","2018","01","00","00000000",false,,,1911700,"41-01-000000"
"41","01","000000","2018","03","01","00000000",false,,,1871300,"41-01-000000"
```

### 7.3 Pagination

All result sets are subject to pagination. The `pageNum` (default: `1`) and `pageSize` parameters control paging. A suggested default `pageSize` is `1000`, though the exact default may vary by implementation. The presence of `hasMore: true` in metadata and a `next` link in the links array indicates additional pages.

---

## 8. Validation & Error Handling

### 8.1 Parameter Name Validation — Strict

> **Key design decision:** Unlike many REST APIs that silently ignore unknown query parameters, the WID API **rejects unknown parameter names with HTTP 400**.

**Why this matters:** Most parameters in the WID API act as filters on potentially large datasets. A misspelled parameter name (e.g., `stateFIPS` instead of `stateFips`) would silently return far more data than intended — a non-obvious error that could go undetected and cause data quality issues downstream. Strict parameter validation surfaces these mistakes immediately.

### 8.2 Parameter Value Validation

Validation of parameter values is left to individual implementers. Most parameters have a finite set of allowable values (e.g., valid state FIPS codes). Implementers may choose strict rejection of invalid values, or a lighter approach such as checking format and length only.

### 8.3 Error Response Format

All errors are returned as a JSON object containing an `errors` array:

```json
{
  "errors": [
    {
      "status": "400",
      "detail": "stfps is not a valid parameter name."
    }
  ]
}
```

---

## 9. Naming Conventions

A core principle: **the API uses human-readable names rather than the cryptic, abbreviated field names in the WID database schema.** The WID database's naming conventions reflect constraints on a long-standing database with many existing users — changes to field names there could cause breaking changes for many states. The API layer provides an opportunity to improve naming without disturbing the database contract.

### Rules

1. **Align parameter names with returned field names** — where a query parameter and a JSON response field refer to the same database field, their names must be identical
2. **Avoid abbreviations** — use full, readable names (e.g., `stateFips` not `stfips`, `seasonallyAdjusted` not `adjusted`)
3. **Avoid duplicating the entity name in field names** — a field inside an `area` object should be `name`, not `areaName`
4. **Use camelCase** for all multi-word names

### Field Name Mapping Examples

| WID DB Field | WID API Parameter / Field |
|---|---|
| `stfips` | `stateFips` |
| `areatype` | `areaType` |
| `area` | `area` |
| `periodyear` | `year` |
| `periodtype` | `periodType` |
| `period` | `period` |
| `seriescode` | `industrySeriesCode` |
| `adjusted` | `seasonallyAdjusted` |
| `benchmark` | `benchmarkYear` |
| `prelim` | `preliminary` |
| `empces` | `employment` |

---

## 10. Parameter Reference

Parameters that appear across multiple endpoints:

| Parameter | Type | Description |
|---|---|---|
| `stateFips` | string | 2-digit state FIPS code. `"00"` = national, `"41"` = Oregon. |
| `areaType` | string | `"00"` National · `"01"` State · `"04"` County · `"21"` MSA |
| `area` | string | Specific area code corresponding to `areaType` |
| `year` | string | 4-digit period year |
| `minYear` | string | Earliest year to include (year range filter) |
| `maxYear` | string | Latest year to include (year range filter) |
| `periodType` | string | `"01"` Annual · `"03"` Monthly |
| `period` | string | 2-digit zero-padded period number; `"00"` for annual |
| `periodId` | string | Projection period identifier (IOMatrix) |
| `industrySeriesCode` | string | CES industry series code |
| `industry` | string | NAICS or SIC industry code |
| `industryCodeType` | string | `"05"` SIC · `"10"` NAICS |
| `occupation` | string | SOC or wage occupation code |
| `occupationCodeType` | string | `"19"` SOC 2018 · `"45"` Wage Occupations |
| `wageSource` | string | Code identifying the wage data source |
| `rateType` | string | Code identifying the wage rate type |
| `seasonallyAdjusted` | boolean | Whether to return seasonally adjusted data |
| `pageNum` | integer | Page number. Default: `1` |
| `pageSize` | integer | Records per page. Suggested default: `1000` |

---

## 11. Open Questions for National Adoption

These questions were explicitly raised at the November 2023 ARC presentation:

- **Inline lookup values in data responses?** The Oregon spec calls for strict data/lookup separation, but the Oregon implementation diverges — e.g., `cesEmployment` returns `industrySeriesTitle` inline. Three approaches are on the table: (1) **data fields only**, always requiring a separate lookup call; (2) **always include lookups**, requiring per-table decisions about which ones to embed; (3) **optional embedding via a query parameter** (e.g., `?expand=true`), letting the caller decide. The optional approach avoids per-table judgment calls and serves both bulk data consumers and analysts building quick reports. An audit of which lookups are currently included in the Oregon implementation would help ground this discussion.
- **WID 3.0 naming conventions** — the national API will target WID 3.0 and use WID 3.0 naming conventions for both parameter names and JSON/CSV field names. The Oregon API's human-readable naming convention (see Section 9) should be reconciled with WID 3.0 field names where they differ.
- **XML support?** The 2019 design goals included XML as a potential response format. The 2023 update removed it. Consensus needed.
- **JSON:API conformance?** The 2023 update noted this as a possibility but flagged it as probably unnecessary. To be confirmed.
- **Versioning strategy?** The 2019 design included an explicit version segment (`/wid/v1.0/`). The OpenAPI spec dropped it. A versioning approach needs to be decided before the API is released publicly.
- **API key / authentication?** The 2019 design included `api_key` as a universal parameter on all endpoints (optional depending on implementation). The later spec deferred this entirely. For a national multi-state system, this likely needs a decision.
---

## 12. Adaptation Notes for National WID 3.0

| Area | Oregon API (WID 2.8) | National API (WID 3.0) |
|---|---|---|
| **Schema version** | WID 2.8 | WID 3.0 — decided |
| **Naming conventions** | Oregon human-readable names (e.g., `stateFips`, `seasonallyAdjusted`) | WID 3.0 naming conventions throughout — parameter names and JSON/CSV field names; Oregon's readable names to be reconciled with WID 3.0 spec |
| **`stateFips` in PK** | Not a PK field in Oregon (single-state) | WID 3.0 adds `StateFips` to primary keys; becomes primary filtering dimension across all tables |
| **Multi-state** | `stateFips` enum = `{'00', '41'}` | All state FIPS codes; national (`'00'`) aggregates |
| **Phase 1 tables** | CES, IOMatrix, IOWage, Industry, LaborForce | Same five, per Phase 1 scoping decision |
| **Substate geography** | County + MSA supported | Deferred to later phase; statewide only initially |
| **Authentication** | Deferred to implementers | Needs a decision for national deployment |
| **Versioning** | No version segment in URL | Recommended to add before public release |
| **CES split** | `cesEmployment` / `cesHoursEarnings` | WID 3.0 CES structure to be confirmed |
| **Response formats** | JSON + CSV | Same; XML removed from consideration |
| **Parameter validation** | Strict (400 on unknown params) | Carry forward — especially important for multi-state data volumes |

---

*National WID Project · ARC Consortium · Utah DWS (implementing state)*  
*Reference document compiled from Oregon API materials — April 2026*
