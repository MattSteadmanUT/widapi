-- =============================================================================
-- 010_revert_to_char6_area.sql
-- Converts area columns from char(7) (wrong) to char(6) (WID 3.0 spec).
-- Removes any rows with 7-character area codes introduced by migration 009.
-- =============================================================================

-- Delete all rows that have 7-character all-zero area codes (migration 009 artifact).
-- State rows like "4900000" (7 chars) are also all-numeric-zero-padded patterns.
-- Easiest: delete everything where rtrim(area) has length > 6 characters of zeros.
-- We do this before ALTER TABLE so the column change succeeds.
DELETE FROM laborforce        WHERE length(trim(area)) > 6;
DELETE FROM ces               WHERE length(trim(area)) > 6;
DELETE FROM industry          WHERE length(trim(area)) > 6;
DELETE FROM iowage            WHERE length(trim(area)) > 6;
DELETE FROM projectionsmatrix WHERE length(trim(area)) > 6;
DELETE FROM geographies       WHERE length(trim(area)) > 6;

-- Re-seed the national geography with the correct 6-char code.
INSERT INTO geographies (stfips, areatype, areatypeversion, area, areatitle, areatypetitle)
VALUES ('00', '00', '0', '000000', 'United States', 'National')
ON CONFLICT (stfips, areatype, areatypeversion, area) DO UPDATE SET
    areatitle = EXCLUDED.areatitle,
    areatypetitle = EXCLUDED.areatypetitle;

-- Alter columns to char(6) per WID 3.0 specification.
ALTER TABLE laborforce        ALTER COLUMN area TYPE char(6);
ALTER TABLE ces               ALTER COLUMN area TYPE char(6);
ALTER TABLE industry          ALTER COLUMN area TYPE char(6);
ALTER TABLE iowage            ALTER COLUMN area TYPE char(6);
ALTER TABLE projectionsmatrix ALTER COLUMN area TYPE char(6);
ALTER TABLE geographies       ALTER COLUMN area TYPE char(6);
ALTER TABLE licenseauthorities ALTER COLUMN area TYPE char(6);
ALTER TABLE licensehistory    ALTER COLUMN area TYPE char(6);

-- Reset defaults.
ALTER TABLE laborforce        ALTER COLUMN area SET DEFAULT '000000';
ALTER TABLE ces               ALTER COLUMN area SET DEFAULT '000000';
ALTER TABLE industry          ALTER COLUMN area SET DEFAULT '000000';
ALTER TABLE iowage            ALTER COLUMN area SET DEFAULT '000000';
ALTER TABLE projectionsmatrix ALTER COLUMN area SET DEFAULT '000000';
ALTER TABLE geographies       ALTER COLUMN area SET DEFAULT '000000';


-- Fix any rows that were incorrectly updated to 7-char area codes.
UPDATE laborforce     SET area = trim(area) WHERE trim(area) != area;
UPDATE ces            SET area = trim(area) WHERE trim(area) != area;
UPDATE industry       SET area = trim(area) WHERE trim(area) != area;
UPDATE iowage         SET area = trim(area) WHERE trim(area) != area;
UPDATE projectionsmatrix SET area = trim(area) WHERE trim(area) != area;
UPDATE geographies    SET area = trim(area) WHERE trim(area) != area;

-- Remove the duplicate "0000000" rows that migration 009 introduced.
DELETE FROM laborforce     WHERE area = '0000000';
DELETE FROM ces            WHERE area = '0000000';
DELETE FROM industry       WHERE area = '0000000';
DELETE FROM iowage         WHERE area = '0000000';
DELETE FROM projectionsmatrix WHERE area = '0000000';
DELETE FROM geographies    WHERE area = '0000000';
-- Re-seed the national geography with the correct 6-char code.
INSERT INTO geographies (stfips, areatype, areatypeversion, area, areatitle, areatypetitle)
VALUES ('00', '00', '0', '000000', 'United States', 'National')
ON CONFLICT (stfips, areatype, areatypeversion, area) DO UPDATE SET
    areatitle = EXCLUDED.areatitle,
    areatypetitle = EXCLUDED.areatypetitle;

-- Alter columns to char(6) per WID 3.0 specification.
ALTER TABLE laborforce
    ALTER COLUMN area TYPE char(6);
ALTER TABLE ces
    ALTER COLUMN area TYPE char(6);
ALTER TABLE industry
    ALTER COLUMN area TYPE char(6);
ALTER TABLE iowage
    ALTER COLUMN area TYPE char(6);
ALTER TABLE projectionsmatrix
    ALTER COLUMN area TYPE char(6);
ALTER TABLE geographies
    ALTER COLUMN area TYPE char(6);
ALTER TABLE licenseauthorities
    ALTER COLUMN area TYPE char(6);
ALTER TABLE licensehistory
    ALTER COLUMN area TYPE char(6);
ALTER TABLE matrixxind  -- no area column, skip
    ALTER COLUMN stfips TYPE char(2);  -- no-op to keep syntax consistent
ALTER TABLE license
    -- no area column
    ALTER COLUMN stfips TYPE char(2);

-- Also shrink the inddirectories/occdirectories/cescodes/periodyears
-- if they had char(7) area — they don't, they don't have area columns.

-- Reset the national geography seed default for new inserts.
ALTER TABLE laborforce     ALTER COLUMN area SET DEFAULT '000000';
ALTER TABLE ces            ALTER COLUMN area SET DEFAULT '000000';
ALTER TABLE industry       ALTER COLUMN area SET DEFAULT '000000';
ALTER TABLE iowage         ALTER COLUMN area SET DEFAULT '000000';
ALTER TABLE projectionsmatrix ALTER COLUMN area SET DEFAULT '000000';
ALTER TABLE geographies    ALTER COLUMN area SET DEFAULT '000000';
