-- =============================================================================
-- 014_add_cpi_tables.sql
-- Consumer Price Index (CPI) tables for the National WID.
--
-- Data source: BLS Time Series database (download.bls.gov/pub/time.series/cu/)
--   cu.item         -> cpitems      (item codes and names)
--   cu.area         -> cpiareas     (geographic areas)
--   cu.periodicity  -> cpiperiodicities  (M=monthly, S=semi-annual, A=annual)
--   cu.seasonal     -> cpiseasonaladjustments  (S=seasonally adjusted, U=not adj)
--   cu.series       -> cpiseriesindex  (one row per time series)
--   cu.data.X.xxx   -> cpi          (actual CPI values)
--
-- All BLS codes are preserved verbatim; normalization is done in the API layer.
-- =============================================================================

-- Reference table: CPI item codes (e.g., SA0 = All items)
CREATE TABLE IF NOT EXISTS cpitems (
    itemcode    varchar(16)  NOT NULL,
    itemname    varchar(256) NOT NULL,
    CONSTRAINT pk_cpitems PRIMARY KEY (itemcode)
);

-- Reference table: CPI geographic area codes (e.g., 0000 = U.S. city average)
CREATE TABLE IF NOT EXISTS cpiareas (
    areacode    varchar(16)  NOT NULL,
    areaname    varchar(256) NOT NULL,
    CONSTRAINT pk_cpiareas PRIMARY KEY (areacode)
);

-- Reference table: Periodicity codes (M=monthly, S=semi-annual, A=annual)
CREATE TABLE IF NOT EXISTS cpiperiodicities (
    periodicitycode  varchar(4)  NOT NULL,
    periodicityname  varchar(64) NOT NULL,
    CONSTRAINT pk_cpiperiodicities PRIMARY KEY (periodicitycode)
);

-- Reference table: Seasonal adjustment codes (S=seasonally adjusted, U=unadjusted)
CREATE TABLE IF NOT EXISTS cpiseasonaladjustments (
    seasonalcode    varchar(2)   NOT NULL,
    seasonalname    varchar(64)  NOT NULL,
    CONSTRAINT pk_cpiseasonaladjustments PRIMARY KEY (seasonalcode)
);

-- Series index: one row per BLS CPI time series
-- seriesid format: CU[S|U][A|M|S]<areacode><itemcode>
-- e.g., CUSR0000SA0 = CPI-U seasonally adjusted, U.S. city average, All items
CREATE TABLE IF NOT EXISTS cpiseries (
    seriesid         varchar(32)  NOT NULL,
    seasonalcode     varchar(2)   NOT NULL,
    periodicitycode  varchar(4)   NOT NULL,
    areacode         varchar(16)  NOT NULL,
    itemcode         varchar(16)  NOT NULL,
    basetypecode     varchar(4),
    baseyear         varchar(8),
    footnotecodesstr varchar(64),
    beginperiod      varchar(4),
    beginYear        smallint,
    endperiod        varchar(4),
    endYear          smallint,
    seriesname       varchar(512),
    CONSTRAINT pk_cpiseries PRIMARY KEY (seriesid)
);

CREATE INDEX IF NOT EXISTS ix_cpiseries_areacode  ON cpiseries(areacode);
CREATE INDEX IF NOT EXISTS ix_cpiseries_itemcode  ON cpiseries(itemcode);

-- CPI observation values
CREATE TABLE IF NOT EXISTS cpi (
    seriesid    varchar(32)  NOT NULL,
    year        smallint     NOT NULL,
    period      varchar(4)   NOT NULL,   -- e.g., M01..M12, M13=annual avg, S01/S02
    value       numeric(12,3),
    footnotes   varchar(32),
    CONSTRAINT pk_cpi PRIMARY KEY (seriesid, year, period)
);

CREATE INDEX IF NOT EXISTS ix_cpi_seriesid ON cpi(seriesid);
CREATE INDEX IF NOT EXISTS ix_cpi_year     ON cpi(year);

-- Seed standard periodicity codes (these are static BLS reference values)
INSERT INTO cpiperiodicities (periodicitycode, periodicityname) VALUES
    ('M', 'Monthly'),
    ('S', 'Semi-Annual'),
    ('A', 'Annual')
ON CONFLICT (periodicitycode) DO NOTHING;

-- Seed seasonal adjustment codes
INSERT INTO cpiseasonaladjustments (seasonalcode, seasonalname) VALUES
    ('S', 'Seasonally Adjusted'),
    ('U', 'Not Seasonally Adjusted')
ON CONFLICT (seasonalcode) DO NOTHING;
