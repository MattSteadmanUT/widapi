-- =============================================================================
-- 008_seed_state_periodyears.sql
-- Seeds periodyears for all states (stfips 01-56) so the lookup endpoint
-- returns state-specific year/period coverage without waiting for ingestion.
-- =============================================================================
DO $$
DECLARE
    st  text;
    yr  int;
    mo  int;
    q   int;
    state_fips text[] := ARRAY[
        '01','02','04','05','06','08','09','10','11','12','13','15','16','17','18','19',
        '20','21','22','23','24','25','26','27','28','29','30','31','32','33','34','35',
        '36','37','38','39','40','41','42','44','45','46','47','48','49','50','51','53',
        '54','55','56'
    ];
    mo_names text[] := ARRAY[
        'January','February','March','April','May','June',
        'July','August','September','October','November','December'
    ];
BEGIN
    FOREACH st IN ARRAY state_fips LOOP
        FOR yr IN 2000 .. EXTRACT(YEAR FROM now())::int + 1 LOOP
            -- Annual
            INSERT INTO periodyears (stfips, periodyear, periodtype, period, periodtitle)
            VALUES (st, yr::text, '01', '00', yr::text || ' Annual Average')
            ON CONFLICT DO NOTHING;
            -- Monthly
            FOR mo IN 1 .. 12 LOOP
                INSERT INTO periodyears (stfips, periodyear, periodtype, period, periodtitle)
                VALUES (st, yr::text, '03', lpad(mo::text, 2, '0'), mo_names[mo] || ' ' || yr::text)
                ON CONFLICT DO NOTHING;
            END LOOP;
            -- Quarterly
            FOR q IN 1 .. 4 LOOP
                INSERT INTO periodyears (stfips, periodyear, periodtype, period, periodtitle)
                VALUES (st, yr::text, '02', lpad(q::text, 2, '0'), 'Q' || q::text || ' ' || yr::text)
                ON CONFLICT DO NOTHING;
            END LOOP;
        END LOOP;
    END LOOP;
END;
$$;
