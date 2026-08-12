-- =============================================================================
-- wid-30-seed-national.sql
-- Seeds the geographies, statefips, areatypes, and periodyears lookup tables
-- with nationally-applicable reference data following the WID 3.0 standard.
--
-- Run this after wid-30-schema-postgres.sql.
-- This data is the same for all states; state-specific data is loaded
-- separately via your state BLS extracts or the National WID API.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- National geography seed
-- ---------------------------------------------------------------------------
INSERT INTO geographies (stfips, areatype, areatypeversion, area, areatitle, areatypetitle)
VALUES ('00', '00', '0', '000000', 'United States', 'National')
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- State FIPS seed (50 states + DC + participating territories)
-- ---------------------------------------------------------------------------
INSERT INTO statefips (stfips, statename, stateabbrev) VALUES
('00', 'United States',     'US'), ('01', 'Alabama',             'AL'),
('02', 'Alaska',            'AK'), ('04', 'Arizona',             'AZ'),
('05', 'Arkansas',          'AR'), ('06', 'California',          'CA'),
('08', 'Colorado',          'CO'), ('09', 'Connecticut',         'CT'),
('10', 'Delaware',          'DE'), ('11', 'District of Columbia', 'DC'),
('12', 'Florida',           'FL'), ('13', 'Georgia',             'GA'),
('15', 'Hawaii',            'HI'), ('16', 'Idaho',               'ID'),
('17', 'Illinois',          'IL'), ('18', 'Indiana',             'IN'),
('19', 'Iowa',              'IA'), ('20', 'Kansas',              'KS'),
('21', 'Kentucky',          'KY'), ('22', 'Louisiana',           'LA'),
('23', 'Maine',             'ME'), ('24', 'Maryland',            'MD'),
('25', 'Massachusetts',     'MA'), ('26', 'Michigan',            'MI'),
('27', 'Minnesota',         'MN'), ('28', 'Mississippi',         'MS'),
('29', 'Missouri',          'MO'), ('30', 'Montana',             'MT'),
('31', 'Nebraska',          'NE'), ('32', 'Nevada',              'NV'),
('33', 'New Hampshire',     'NH'), ('34', 'New Jersey',          'NJ'),
('35', 'New Mexico',        'NM'), ('36', 'New York',            'NY'),
('37', 'North Carolina',    'NC'), ('38', 'North Dakota',        'ND'),
('39', 'Ohio',              'OH'), ('40', 'Oklahoma',            'OK'),
('41', 'Oregon',            'OR'), ('42', 'Pennsylvania',        'PA'),
('44', 'Rhode Island',      'RI'), ('45', 'South Carolina',      'SC'),
('46', 'South Dakota',      'SD'), ('47', 'Tennessee',           'TN'),
('48', 'Texas',             'TX'), ('49', 'Utah',                'UT'),
('50', 'Vermont',           'VT'), ('51', 'Virginia',            'VA'),
('53', 'Washington',        'WA'), ('54', 'West Virginia',       'WV'),
('55', 'Wisconsin',         'WI'), ('56', 'Wyoming',             'WY'),
('66', 'Guam',              'GU'), ('72', 'Puerto Rico',         'PR'),
('78', 'US Virgin Islands', 'VI')
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- State-level geography records (one per state)
-- ---------------------------------------------------------------------------
INSERT INTO geographies (stfips, areatype, areatypeversion, area, areatitle, areatypetitle)
SELECT stfips, '01', '0', stfips || '00000', statename, 'Statewide'
FROM statefips
WHERE stfips NOT IN ('00')
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- Area type codes (WID 3.0 standard values)
-- ---------------------------------------------------------------------------
INSERT INTO areatypes (stfips, areatype, areatypename) VALUES
('00', '00', 'National'),
('00', '01', 'Statewide'),
('00', '04', 'County'),
('00', '21', 'Metropolitan Statistical Area (MSA)'),
('00', '31', 'Metropolitan Statistical Area (CBSA)'),
('00', '32', 'Micropolitan Statistical Area'),
('00', '33', 'Combined Statistical Area'),
('00', '34', 'New England City and Town Area')
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- Period years: national annual (2000–current+1)
-- ---------------------------------------------------------------------------
DO $$
DECLARE yr int;
BEGIN
    FOR yr IN 2000 .. EXTRACT(YEAR FROM now())::int + 1 LOOP
        INSERT INTO periodyears (stfips, periodyear, periodtype, period, periodtitle)
        VALUES ('00', yr::text, '01', '00', yr::text || ' Annual Average')
        ON CONFLICT DO NOTHING;
    END LOOP;
END;
$$;

-- Monthly
DO $$
DECLARE yr int; mo int;
    mo_names text[] := ARRAY['January','February','March','April','May','June',
                              'July','August','September','October','November','December'];
BEGIN
    FOR yr IN 2000 .. EXTRACT(YEAR FROM now())::int + 1 LOOP
        FOR mo IN 1..12 LOOP
            INSERT INTO periodyears (stfips, periodyear, periodtype, period, periodtitle)
            VALUES ('00', yr::text, '03', lpad(mo::text,2,'0'), mo_names[mo] || ' ' || yr::text)
            ON CONFLICT DO NOTHING;
        END LOOP;
    END LOOP;
END;
$$;

-- Quarterly
DO $$
DECLARE yr int; q int;
BEGIN
    FOR yr IN 2000 .. EXTRACT(YEAR FROM now())::int + 1 LOOP
        FOR q IN 1..4 LOOP
            INSERT INTO periodyears (stfips, periodyear, periodtype, period, periodtitle)
            VALUES ('00', yr::text, '02', lpad(q::text,2,'0'), 'Q'||q::text||' '||yr::text)
            ON CONFLICT DO NOTHING;
        END LOOP;
    END LOOP;
END;
$$;

-- ---------------------------------------------------------------------------
-- Ownership codes seed (industry table ownership field)
-- ---------------------------------------------------------------------------
COMMENT ON COLUMN industry.ownership IS '0=Total, 1=Federal, 2=State Govt, 3=Local Govt, 5=Private';
