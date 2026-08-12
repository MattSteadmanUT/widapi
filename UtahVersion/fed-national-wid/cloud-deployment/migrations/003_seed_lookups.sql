-- =============================================================================
-- 003_seed_lookups.sql
-- Standard WID 3.0 field values — area types and period types
-- Source: WID 3.0 Structure Document, Standard Field Values section
-- =============================================================================

-- ---------------------------------------------------------------------------
-- National geography record (stfips='00', areatype='00', area='000000')
-- ---------------------------------------------------------------------------
-- NOTE: uses "areatitle" (the column name as of migration 001), not "areaname" --
-- migration 012 renames areatitle -> areaname later in the sequence, and this
-- seeded row's data carries over automatically when that rename runs. Inserting
-- into "areaname" here would fail on a fresh database, since that column does
-- not exist until migration 012 (confirmed by tracing the schema at each step;
-- see docs/database-schema.md).
INSERT INTO geographies (stfips, areatype, areatypeversion, area, areatitle, areatypetitle)
VALUES ('00', '00', '0', '000000', 'United States', 'National')
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- Standard area type titles (for reference — states populate their own rows)
-- ---------------------------------------------------------------------------
-- The WID area type codes are defined in the structure doc; these labels are
-- for display purposes in the geographies lookup.

-- ---------------------------------------------------------------------------
-- Seed periodyears for 2000–current+1 (annual only; monthly/quarterly added
-- by the ingestion Lambda as it encounters actual BLS periods)
-- ---------------------------------------------------------------------------
DO $$
DECLARE
    yr int;
BEGIN
    FOR yr IN 2000 .. EXTRACT(YEAR FROM now())::int + 1 LOOP
        INSERT INTO periodyears (stfips, periodyear, periodtype, period, periodtitle)
        VALUES ('00', yr::text, '01', '00', yr::text || ' Annual Average')
        ON CONFLICT DO NOTHING;
    END LOOP;
END;
$$;

-- Monthly period titles for reference
DO $$
DECLARE
    mo  int;
    mo_names text[] := ARRAY[
        'January','February','March','April','May','June',
        'July','August','September','October','November','December'
    ];
    yr  int;
BEGIN
    FOR yr IN 2000 .. EXTRACT(YEAR FROM now())::int + 1 LOOP
        FOR mo IN 1 .. 12 LOOP
            INSERT INTO periodyears (stfips, periodyear, periodtype, period, periodtitle)
            VALUES (
                '00',
                yr::text,
                '03',
                lpad(mo::text, 2, '0'),
                mo_names[mo] || ' ' || yr::text
            )
            ON CONFLICT DO NOTHING;
        END LOOP;
    END LOOP;
END;
$$;

-- Quarterly period titles
DO $$
DECLARE
    q   int;
    yr  int;
BEGIN
    FOR yr IN 2000 .. EXTRACT(YEAR FROM now())::int + 1 LOOP
        FOR q IN 1 .. 4 LOOP
            INSERT INTO periodyears (stfips, periodyear, periodtype, period, periodtitle)
            VALUES (
                '00',
                yr::text,
                '02',
                lpad(q::text, 2, '0'),
                'Q' || q::text || ' ' || yr::text
            )
            ON CONFLICT DO NOTHING;
        END LOOP;
    END LOOP;
END;
$$;


