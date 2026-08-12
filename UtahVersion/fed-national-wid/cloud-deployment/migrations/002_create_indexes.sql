-- =============================================================================
-- 002_create_indexes.sql
-- Performance indexes for the National WID 3.0 database
-- =============================================================================

-- laborforce — most common query patterns
CREATE INDEX IF NOT EXISTS ix_laborforce_stfips_periodyear
    ON laborforce (stfips, periodyear);

CREATE INDEX IF NOT EXISTS ix_laborforce_area_periodyear
    ON laborforce (area, periodyear);

CREATE INDEX IF NOT EXISTS ix_laborforce_periodyear_periodtype
    ON laborforce (periodyear, periodtype, period);

-- ces
CREATE INDEX IF NOT EXISTS ix_ces_stfips_periodyear
    ON ces (stfips, periodyear);

CREATE INDEX IF NOT EXISTS ix_ces_seriescode
    ON ces (seriescodetype, seriescode);

CREATE INDEX IF NOT EXISTS ix_ces_area_periodyear
    ON ces (area, periodyear);

-- industry
CREATE INDEX IF NOT EXISTS ix_industry_stfips_periodyear
    ON industry (stfips, periodyear);

CREATE INDEX IF NOT EXISTS ix_industry_indcode
    ON industry (codetype, indcode);

CREATE INDEX IF NOT EXISTS ix_industry_area_periodyear
    ON industry (area, periodyear);

-- iowage
CREATE INDEX IF NOT EXISTS ix_iowage_stfips_periodyear
    ON iowage (stfips, periodyear);

CREATE INDEX IF NOT EXISTS ix_iowage_occcode
    ON iowage (occcodetype, occcode);

CREATE INDEX IF NOT EXISTS ix_iowage_area
    ON iowage (area, periodyear);

-- projectionsmatrix
CREATE INDEX IF NOT EXISTS ix_projectionsmatrix_stfips_period
    ON projectionsmatrix (stfips, projectionsperiod);

CREATE INDEX IF NOT EXISTS ix_projectionsmatrix_occcode
    ON projectionsmatrix (occcodetype, occcode);

CREATE INDEX IF NOT EXISTS ix_projectionsmatrix_indcode
    ON projectionsmatrix (indcodetype, indcode);

-- geographies — lookup joins
CREATE INDEX IF NOT EXISTS ix_geographies_areatype
    ON geographies (areatype, areatypeversion);

-- ingestlog — operations monitoring
CREATE INDEX IF NOT EXISTS ix_ingestlog_dataset_runat
    ON ingestlog (dataset, runat DESC);
