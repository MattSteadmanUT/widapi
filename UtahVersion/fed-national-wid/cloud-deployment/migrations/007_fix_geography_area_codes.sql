-- =============================================================================
-- 007_fix_geography_area_codes.sql
-- Clears bad geography data and resets hashes so the ingestor re-populates
-- with the corrected area code convention on next run.
-- =============================================================================

-- Remove all geography rows except the national seed (it was already correct).
-- NOTE: area is char(6); the national seed row (migration 003) uses '000000' (6
-- zeros), matching the column's actual length. The literal here previously read
-- '0000000' (7 zeros) -- a plain length typo that doesn't match the stated
-- intent of "keep the national seed row" (a 7-char literal never equals a
-- 6-char column value, so the seed row would have been deleted right along
-- with everything else). Corrected to the actual 6-zero value.
DELETE FROM geographies WHERE NOT (stfips = '00' AND areatype = '00' AND area = '000000');

-- Remove all statefips rows (will be re-populated fresh).
DELETE FROM statefips;

-- Clear the ingestsourcehash entry for the WIDCenter geography bundle so the
-- next Lambda run re-downloads and processes it with the fixed area codes.
DELETE FROM ingestsourcehash WHERE dataset = 'WIDCENTER-GEOG';
