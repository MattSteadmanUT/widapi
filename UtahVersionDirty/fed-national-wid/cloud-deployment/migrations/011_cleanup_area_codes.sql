-- =============================================================================
-- 011_cleanup_area_codes.sql
-- Removes data rows that used the old incorrect area code conventions.
-- Old convention: stFips+zeros right-padded ("490000" for Utah state).
-- Correct WID 3.0 convention: stFips left-padded ("000049" for Utah state).
-- =============================================================================

-- Remove state-level rows that used the old {stFips}+zeros format.
-- These all start with a non-zero digit (e.g., "010000", "490000").
-- Correct state codes are left-padded zeros (e.g., "000001", "000049").
DELETE FROM laborforce        WHERE area ~ '^[1-9]';
DELETE FROM ces               WHERE area ~ '^[1-9]';
DELETE FROM industry          WHERE area ~ '^[1-9]';
DELETE FROM iowage            WHERE area ~ '^[1-9]';
DELETE FROM projectionsmatrix WHERE area ~ '^[1-9]';
