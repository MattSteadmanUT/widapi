-- =============================================================================
-- 017_widen_cpi_baseyear.sql
-- CPI series rows use base-period strings like '1982-84=100', which do not fit
-- the original varchar(8) column definition.
-- =============================================================================

ALTER TABLE cpiseries
    ALTER COLUMN baseyear TYPE varchar(32);
