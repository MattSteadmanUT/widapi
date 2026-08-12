-- =============================================================================
-- 007_fix_geography_area_codes.sql
-- Clears bad geography data and resets hashes so the ingestor re-populates
-- with the corrected area code convention on next run.
-- =============================================================================

-- Remove all geography rows except the national seed (it was already correct).
DELETE FROM geographies WHERE NOT (stfips = '00' AND areatype = '00' AND area = '0000000');

-- Remove all statefips rows (will be re-populated fresh).
DELETE FROM statefips;

-- Clear the ingestsourcehash entry for the WIDCenter geography bundle so the
-- next Lambda run re-downloads and processes it with the fixed area codes.
DELETE FROM ingestsourcehash WHERE dataset = 'WIDCENTER-GEOG';
