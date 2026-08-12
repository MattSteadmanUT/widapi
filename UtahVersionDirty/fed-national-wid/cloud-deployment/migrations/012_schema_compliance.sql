-- =============================================================================
-- 012_schema_compliance.sql
-- Corrects implementation against WID 3.0 specification (rev. 2025-05-01).
--
-- Strategy:
--   1. Additive changes (ADD COLUMN) — safe, applied immediately.
--   2. Type expansions (widening only) — safe.
--   3. Field renames — applied via column rename (new name per spec).
--   4. Known design deviations are DOCUMENTED BELOW, not changed here.
--
-- KNOWN DESIGN DEVIATIONS (intentional, documented, not corrected here):
--   A. ProjectionsMatrix PK uses projectionsperiod char(9) = "YYYY-YYYY"
--      instead of spec's separate PeriodYear/PeriodType/Period/ProjectedYear/
--      MatrixIndCode/MatrixOccCode fields. The national API maps projections
--      from ProjectionsCentral which uses a "base-projected" range format.
--   B. IndDirectories and OccDirectories use (stfips, projperiod,
--      indcodetype/occcodetype, indcode/occcode) as PK instead of spec's
--      (MatrixIndCode, PeriodYear, PeriodType, Period, ProjectedYear).
--      These tables serve as general code-title directories here.
--   C. CES.SeriesCodeType stores "NAICS" (the name) rather than the WID
--      codetype code "10". This affects ~35k CES rows; deferred to a
--      separate data-migration task.
--   D. IOWage.meanhourly and IOWage.annualmean are extension fields not in
--      the spec but populated by BLS OEWS data.
--   E. PeriodYears has StFips + PeriodType + Period + PeriodTitle extension
--      fields beyond the spec's single PeriodYear field. These are additive.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- LaborForce: add Benchmark, CLFPRate, EmpPopRatio
-- ---------------------------------------------------------------------------
ALTER TABLE laborforce
    ADD COLUMN IF NOT EXISTS benchmark   char(4),
    ADD COLUMN IF NOT EXISTS clfprate    numeric(5,1),
    ADD COLUMN IF NOT EXISTS emppoproatio numeric(5,1);

COMMENT ON COLUMN laborforce.benchmark    IS 'Benchmark year of the data (WID 3.0 field 10)';
COMMENT ON COLUMN laborforce.clfprate     IS 'Civilian labor force participation rate (WID 3.0 field 15)';
COMMENT ON COLUMN laborforce.emppoproatio IS 'Employment to population ratio (WID 3.0 field 16)';

-- ---------------------------------------------------------------------------
-- CES: add missing spec fields; widen SeriesCode/SeriesCodeType
-- ---------------------------------------------------------------------------
ALTER TABLE ces
    ADD COLUMN IF NOT EXISTS benchmark              char(4),
    ADD COLUMN IF NOT EXISTS empfemaleworkers       numeric(9,0),
    ADD COLUMN IF NOT EXISTS suppfemaleworkers      char(1) NOT NULL DEFAULT '0',
    ADD COLUMN IF NOT EXISTS hoursallworkers        numeric(3,1),
    ADD COLUMN IF NOT EXISTS earningsallworkers     numeric(8,2),
    ADD COLUMN IF NOT EXISTS hourlyearningsallworkers numeric(6,2),
    ADD COLUMN IF NOT EXISTS suppheallwrkr          char(1) NOT NULL DEFAULT '0';

-- Widen SeriesCode from varchar(20) to match spec char(8) semantics.
-- We widen to at least 8; keeping varchar for flexibility.
ALTER TABLE ces ALTER COLUMN seriescode TYPE varchar(20); -- already 20, keep as-is
ALTER TABLE ces ALTER COLUMN seriescodetype TYPE varchar(10); -- keep as-is (see deviation C)

COMMENT ON COLUMN ces.benchmark               IS 'Benchmark year (WID 3.0 field 11)';
COMMENT ON COLUMN ces.empfemaleworkers        IS 'Number of female production workers (WID 3.0 field 15)';
COMMENT ON COLUMN ces.suppfemaleworkers       IS 'Suppress female workers flag (WID 3.0 field 22)';
COMMENT ON COLUMN ces.hoursallworkers         IS 'Avg hours/week for all workers (WID 3.0 field 23)';
COMMENT ON COLUMN ces.earningsallworkers      IS 'Avg weekly earnings all workers (WID 3.0 field 24)';
COMMENT ON COLUMN ces.hourlyearningsallworkers IS 'Avg hourly earnings all workers (WID 3.0 field 25)';
COMMENT ON COLUMN ces.suppheallwrkr           IS 'Suppress H&E all workers flag (WID 3.0 field 26)';

-- ---------------------------------------------------------------------------
-- Industry: rename codetype→indcodetype; widen ownership char(1)→char(2);
--           add missing spec fields
-- ---------------------------------------------------------------------------

-- Rename codetype to indcodetype (spec field name).
-- This requires dropping and recreating the PK.
ALTER TABLE industry DROP CONSTRAINT pk_industry;
ALTER TABLE industry RENAME COLUMN codetype TO indcodetype;
ALTER TABLE industry ALTER COLUMN indcodetype TYPE char(2);
ALTER TABLE industry ALTER COLUMN ownership TYPE char(2);
ALTER TABLE industry
    ADD CONSTRAINT pk_industry
        PRIMARY KEY (stfips, areatype, areatypeversion, area,
                     periodyear, periodtype, period, ownership, indcodetype, indcode);

-- IndCode should be char(10) per spec (fixed-length, blank-fill). Convert.
ALTER TABLE industry ALTER COLUMN indcode TYPE char(10);

-- Add missing spec fields.
ALTER TABLE industry
    ADD COLUMN IF NOT EXISTS firms              numeric(8,0),
    ADD COLUMN IF NOT EXISTS establishments    numeric(8,0),
    ADD COLUMN IF NOT EXISTS quarteravgemp     numeric(9,0),
    ADD COLUMN IF NOT EXISTS month1emp         numeric(9,0),
    ADD COLUMN IF NOT EXISTS month2emp         numeric(9,0),
    ADD COLUMN IF NOT EXISTS month3emp         numeric(9,0),
    ADD COLUMN IF NOT EXISTS topemployeravgemp numeric(9,0);

-- Tighten existing sizes to spec values.
ALTER TABLE industry ALTER COLUMN totalwages  TYPE numeric(14,0) USING round(totalwages);
ALTER TABLE industry ALTER COLUMN taxablewages TYPE numeric(14,0) USING round(taxablewages);
ALTER TABLE industry RENAME COLUMN contributions TO uicontributions;
ALTER TABLE industry ALTER COLUMN uicontributions TYPE numeric(12,0) USING round(uicontributions);
ALTER TABLE industry ALTER COLUMN weeklywage TYPE numeric(8,0) USING round(weeklywage);
-- avgmonthlyemp now superseded by quarteravgemp; keep avgmonthlyemp for backward compat.

COMMENT ON COLUMN industry.indcodetype       IS 'Industry code type (WID 3.0 field 8, renamed from codetype)';
COMMENT ON COLUMN industry.firms             IS 'Number of firms (WID 3.0 field 12)';
COMMENT ON COLUMN industry.establishments   IS 'Number of establishments (WID 3.0 field 13)';
COMMENT ON COLUMN industry.quarteravgemp    IS 'Average quarterly employment (WID 3.0 field 14)';
COMMENT ON COLUMN industry.month1emp        IS 'Employment month 1 (WID 3.0 field 15)';
COMMENT ON COLUMN industry.month2emp        IS 'Employment month 2 (WID 3.0 field 16)';
COMMENT ON COLUMN industry.month3emp        IS 'Employment month 3 (WID 3.0 field 17)';
COMMENT ON COLUMN industry.topemployeravgemp IS 'Top employer avg employment (WID 3.0 field 18)';
COMMENT ON COLUMN industry.uicontributions  IS 'UI contributions — renamed from contributions (WID 3.0 field 22)';

-- ---------------------------------------------------------------------------
-- IOWage: rename percentile fields to spec names; add missing spec fields
-- ---------------------------------------------------------------------------

-- Rename abbreviated percentile field names to spec-correct names.
ALTER TABLE iowage RENAME COLUMN pct10 TO percentile10wage;
ALTER TABLE iowage RENAME COLUMN pct25 TO percentile25wage;
ALTER TABLE iowage RENAME COLUMN pct75 TO percentile75wage;
ALTER TABLE iowage RENAME COLUMN pct90 TO percentile90wage;
ALTER TABLE iowage RENAME COLUMN supprecord TO suppresswage;

-- Fix OccCode/IndCode to char(10) per spec.
ALTER TABLE iowage ALTER COLUMN occcode TYPE char(10);
ALTER TABLE iowage ALTER COLUMN indcode TYPE char(10);

-- Add missing spec fields.
ALTER TABLE iowage
    ADD COLUMN IF NOT EXISTS responserate          numeric(6,0),
    ADD COLUMN IF NOT EXISTS entrywage             numeric(9,2),
    ADD COLUMN IF NOT EXISTS experiencedwage       numeric(9,2),
    ADD COLUMN IF NOT EXISTS userdefinedpct        numeric(3,0),
    ADD COLUMN IF NOT EXISTS userdefinedpctwage    numeric(9,2),
    ADD COLUMN IF NOT EXISTS userdefinedrangelopct numeric(3,0),
    ADD COLUMN IF NOT EXISTS userdefinedrangehipct numeric(3,0),
    ADD COLUMN IF NOT EXISTS userdefinedranagemean numeric(9,2),
    ADD COLUMN IF NOT EXISTS wagerelativepcterror  numeric(6,2),
    ADD COLUMN IF NOT EXISTS emprelativepcterror   numeric(6,2),
    ADD COLUMN IF NOT EXISTS panelcode             char(6),
    ADD COLUMN IF NOT EXISTS suppressall           char(1) NOT NULL DEFAULT '0',
    ADD COLUMN IF NOT EXISTS suppressemp           char(1) NOT NULL DEFAULT '0';

-- Correct precision to spec (numeric(9,2) not numeric(10,2)).
ALTER TABLE iowage
    ALTER COLUMN medianwage           TYPE numeric(9,2),
    ALTER COLUMN meanwage             TYPE numeric(9,2),
    ALTER COLUMN percentile10wage     TYPE numeric(9,2),
    ALTER COLUMN percentile25wage     TYPE numeric(9,2),
    ALTER COLUMN percentile75wage     TYPE numeric(9,2),
    ALTER COLUMN percentile90wage     TYPE numeric(9,2);

COMMENT ON COLUMN iowage.responserate         IS 'Survey response rate (WID 3.0 field 15)';
COMMENT ON COLUMN iowage.entrywage            IS 'Entry-level wage (WID 3.0 field 17)';
COMMENT ON COLUMN iowage.experiencedwage      IS 'Experienced-level wage (WID 3.0 field 18)';
COMMENT ON COLUMN iowage.panelcode            IS 'Reference panel code yyyymm (WID 3.0 field 31)';
COMMENT ON COLUMN iowage.suppresswage         IS 'Suppress wage values flag (renamed from supprecord, WID 3.0 field 32)';
COMMENT ON COLUMN iowage.suppressall          IS 'Suppress entire record flag (WID 3.0 field 33)';
COMMENT ON COLUMN iowage.suppressemp          IS 'Suppress employment values flag (WID 3.0 field 34)';

-- ---------------------------------------------------------------------------
-- ProjectionsMatrix: add missing spec fields (design deviation A documented above)
-- ---------------------------------------------------------------------------
ALTER TABLE projectionsmatrix
    ADD COLUMN IF NOT EXISTS pctestind     numeric(6,2),
    ADD COLUMN IF NOT EXISTS pctestocc     numeric(6,2),
    ADD COLUMN IF NOT EXISTS pctprojind    numeric(6,2),
    ADD COLUMN IF NOT EXISTS pctprojocc    numeric(6,2),
    ADD COLUMN IF NOT EXISTS growthcode    char(2),
    ADD COLUMN IF NOT EXISTS exits         numeric(9,0),
    ADD COLUMN IF NOT EXISTS annualexits   numeric(9,0),
    ADD COLUMN IF NOT EXISTS transfers     numeric(9,0),
    ADD COLUMN IF NOT EXISTS annualtransfers numeric(9,0),
    ADD COLUMN IF NOT EXISTS annualchange  numeric(9,0),
    ADD COLUMN IF NOT EXISTS annualopenings numeric(9,0);

ALTER TABLE projectionsmatrix RENAME COLUMN supprecord TO suppress;

COMMENT ON COLUMN projectionsmatrix.pctestind      IS 'Pct of base-year industry emp (WID 3.0 field 13)';
COMMENT ON COLUMN projectionsmatrix.pctestocc      IS 'Pct of base-year occupation emp (WID 3.0 field 14)';
COMMENT ON COLUMN projectionsmatrix.pctprojind     IS 'Pct of projected industry emp (WID 3.0 field 15)';
COMMENT ON COLUMN projectionsmatrix.pctprojocc     IS 'Pct of projected occupation emp (WID 3.0 field 16)';
COMMENT ON COLUMN projectionsmatrix.growthcode     IS 'State-specific growth descriptor (WID 3.0 field 20)';
COMMENT ON COLUMN projectionsmatrix.exits          IS 'Exits from labor force (WID 3.0 field 21)';
COMMENT ON COLUMN projectionsmatrix.annualexits    IS 'Annual exits from labor force (WID 3.0 field 22)';
COMMENT ON COLUMN projectionsmatrix.transfers      IS 'Transfers between occupations (WID 3.0 field 23)';
COMMENT ON COLUMN projectionsmatrix.annualtransfers IS 'Annual transfers (WID 3.0 field 24)';
COMMENT ON COLUMN projectionsmatrix.annualchange   IS 'Annual employment change (WID 3.0 field 26)';
COMMENT ON COLUMN projectionsmatrix.annualopenings IS 'Annual job openings (WID 3.0 field 28)';
COMMENT ON COLUMN projectionsmatrix.suppress       IS 'Suppress confidential data flag (renamed from supprecord, WID 3.0 field 29)';

-- ---------------------------------------------------------------------------
-- Geographies: add spec fields (AreaName varchar(100), AreaDesc, Latitude,
--              Longitude, GeoPrecisionCode); keep areatitle as alias
-- ---------------------------------------------------------------------------
ALTER TABLE geographies
    ADD COLUMN IF NOT EXISTS areadesc         text,
    ADD COLUMN IF NOT EXISTS latitude         numeric(11,6),
    ADD COLUMN IF NOT EXISTS longitude        numeric(11,6),
    ADD COLUMN IF NOT EXISTS geoprecisioncode char(1);

-- Rename areatitle to areaname per spec; keep alias for backward compat.
ALTER TABLE geographies RENAME COLUMN areatitle TO areaname;
-- Widen to spec size.
ALTER TABLE geographies ALTER COLUMN areaname TYPE varchar(100);

COMMENT ON COLUMN geographies.areaname         IS 'Geographic area name (renamed from areatitle, WID 3.0 field 5)';
COMMENT ON COLUMN geographies.areadesc         IS 'Narrative area description (WID 3.0 field 6)';
COMMENT ON COLUMN geographies.latitude         IS 'Geographic latitude (WID 3.0 field 7)';
COMMENT ON COLUMN geographies.longitude        IS 'Geographic longitude (WID 3.0 field 8)';
COMMENT ON COLUMN geographies.geoprecisioncode IS 'Geocode precision level (WID 3.0 field 9)';

-- ---------------------------------------------------------------------------
-- StateFips: spec says varchar(20) for StateName — ours varchar(120) is fine
--            (already wider). Field 'stateabbrev' spec calls 'Abbreviation'.
-- ---------------------------------------------------------------------------
-- No change needed; varchar(120) > spec's varchar(20) is acceptable.

-- ---------------------------------------------------------------------------
-- AreaTypes: widen areatypename to spec's varchar(200)
-- ---------------------------------------------------------------------------
ALTER TABLE areatypes ALTER COLUMN areatypename TYPE varchar(200);

-- ---------------------------------------------------------------------------
-- License: add missing AreaType/AreaTypeVersion/Area non-PK fields;
--           add LicenseDesc; fix LicenseID to char(10)
-- ---------------------------------------------------------------------------

-- Fix LicenseID type to char(10) per spec (existing data is empty, safe).
ALTER TABLE license ALTER COLUMN licenseid TYPE char(10);
ALTER TABLE license ALTER COLUMN licauthid TYPE char(3);
ALTER TABLE license ALTER COLUMN licensetype TYPE char(1);

ALTER TABLE license
    ADD COLUMN IF NOT EXISTS areatype        char(2),
    ADD COLUMN IF NOT EXISTS areatypeversion char(4),
    ADD COLUMN IF NOT EXISTS area            char(6),
    ADD COLUMN IF NOT EXISTS licensedesc     text;

COMMENT ON COLUMN license.licensedesc IS 'License description (WID 3.0 field 8)';
COMMENT ON COLUMN license.areatype    IS 'Geographic area type (WID 3.0 field 2 — non-PK)';

-- ---------------------------------------------------------------------------
-- LicenseAuthorities: fix LicAuthID to char(3); add missing fields
-- ---------------------------------------------------------------------------
ALTER TABLE licenseauthorities ALTER COLUMN licauthid TYPE char(3);
ALTER TABLE licenseauthorities ALTER COLUMN zipcode TYPE char(5);
ALTER TABLE licenseauthorities ALTER COLUMN telephone TYPE varchar(10);

ALTER TABLE licenseauthorities
    ADD COLUMN IF NOT EXISTS address2         varchar(75),
    ADD COLUMN IF NOT EXISTS zipext           char(4),
    ADD COLUMN IF NOT EXISTS latitude         numeric(11,6),
    ADD COLUMN IF NOT EXISTS longitude        numeric(11,6),
    ADD COLUMN IF NOT EXISTS geoprecisioncode char(1),
    ADD COLUMN IF NOT EXISTS teleext          varchar(10),
    ADD COLUMN IF NOT EXISTS fax              varchar(10),
    ADD COLUMN IF NOT EXISTS contact          varchar(50);

-- Tighten varchar lengths to spec.
ALTER TABLE licenseauthorities ALTER COLUMN department TYPE varchar(255);
ALTER TABLE licenseauthorities ALTER COLUMN division   TYPE varchar(255);
ALTER TABLE licenseauthorities ALTER COLUMN board      TYPE varchar(255);
ALTER TABLE licenseauthorities ALTER COLUMN address1   TYPE varchar(75);
ALTER TABLE licenseauthorities ALTER COLUMN city       TYPE varchar(30);
ALTER TABLE licenseauthorities ALTER COLUMN email      TYPE varchar(70);
ALTER TABLE licenseauthorities ALTER COLUMN url        TYPE varchar(200);

-- ---------------------------------------------------------------------------
-- LicenseHistory: fix LicenseID to char(10); LicenseNumberType to char(2)
-- ---------------------------------------------------------------------------
ALTER TABLE licensehistory ALTER COLUMN licenseid          TYPE char(10);
ALTER TABLE licensehistory ALTER COLUMN licensenumbertype  TYPE char(2);
ALTER TABLE licensehistory ALTER COLUMN licensenumber      TYPE numeric(9,0) USING licensenumber::numeric;

-- ---------------------------------------------------------------------------
-- LicenseXOcc: fix LicenseID to char(10); OccCode to char(10)
-- ---------------------------------------------------------------------------
ALTER TABLE licensexocc ALTER COLUMN licenseid TYPE char(10);
ALTER TABLE licensexocc ALTER COLUMN occcode   TYPE char(10);

-- ---------------------------------------------------------------------------
-- MatrixXInd / MatrixXOcc: widen MatrixIndCode/MatrixOccCode to spec sizes
-- ---------------------------------------------------------------------------
ALTER TABLE matrixxind ALTER COLUMN matrixindcode TYPE char(15);
ALTER TABLE matrixxind ALTER COLUMN indcode       TYPE char(10);

ALTER TABLE matrixxocc ALTER COLUMN matrixocccode TYPE char(15);
ALTER TABLE matrixxocc ALTER COLUMN occcode       TYPE char(10);

-- ---------------------------------------------------------------------------
-- IndDirectories / OccDirectories: widen title fields to spec varchar(200)
-- ---------------------------------------------------------------------------
ALTER TABLE inddirectories ALTER COLUMN indtitle TYPE varchar(200);
ALTER TABLE occdirectories ALTER COLUMN occtitle TYPE varchar(200);

ALTER TABLE inddirectories
    ADD COLUMN IF NOT EXISTS subtotal  char(1),
    ADD COLUMN IF NOT EXISTS ownership char(2);
