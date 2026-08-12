-- =============================================================================
-- 001_create_schema.sql
-- National WID 3.0 database schema — PostgreSQL 16
-- Matches WID 3.0 Structure Document (rev. 2025-05-01)
-- All field names are lowercase; flag fields are char(1) ('0'/'1').
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Core data tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS laborforce (
    stfips              char(2)         NOT NULL DEFAULT '00',
    areatype            char(2)         NOT NULL DEFAULT '00',
    areatypeversion     char(4)         NOT NULL DEFAULT '0',
    area                char(6)     NOT NULL DEFAULT '000000',
    periodyear          char(4)         NOT NULL,
    periodtype          char(2)         NOT NULL,
    period              char(2)         NOT NULL DEFAULT '00',
    adjusted            char(1)         NOT NULL DEFAULT '0',
    laborforce          bigint,
    employed            bigint,
    unemployed          bigint,
    unemprate           numeric(8,4),
    supprecord          char(1)         NOT NULL DEFAULT '0',
    supprate            char(1)         NOT NULL DEFAULT '0',
    prelim              char(1)         NOT NULL DEFAULT '0',
    CONSTRAINT pk_laborforce
        PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype, period, adjusted)
);

CREATE TABLE IF NOT EXISTS ces (
    stfips                      char(2)     NOT NULL DEFAULT '00',
    areatype                    char(2)     NOT NULL DEFAULT '00',
    areatypeversion             char(4)     NOT NULL DEFAULT '0',
    area                        char(6)     NOT NULL DEFAULT '000000',
    periodyear                  char(4)     NOT NULL,
    periodtype                  char(2)     NOT NULL,
    period                      char(2)     NOT NULL DEFAULT '00',
    adjusted                    char(1)     NOT NULL DEFAULT '0',
    seriescodetype              varchar(10) NOT NULL,
    seriescode                  varchar(20) NOT NULL,
    empces                      bigint,
    empproductionworkers        bigint,
    hoursperweek                numeric(6,1),
    earningsperweek             numeric(8,2),
    earningsperhour             numeric(8,2),
    avgweeklyearningspctchange  numeric(8,4),
    supprecord                  char(1)     NOT NULL DEFAULT '0',
    supphoursearnings           char(1)     NOT NULL DEFAULT '0',
    suppprodworkers             char(1)     NOT NULL DEFAULT '0',
    prelim                      char(1)     NOT NULL DEFAULT '0',
    CONSTRAINT pk_ces
        PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                     seriescodetype, seriescode, adjusted)
);

CREATE TABLE IF NOT EXISTS industry (
    stfips          char(2)     NOT NULL DEFAULT '00',
    areatype        char(2)     NOT NULL DEFAULT '00',
    areatypeversion char(4)     NOT NULL DEFAULT '0',
    area            char(6)     NOT NULL DEFAULT '000000',
    periodyear      char(4)     NOT NULL,
    periodtype      char(2)     NOT NULL,
    period          char(2)     NOT NULL DEFAULT '00',
    ownership       char(1)     NOT NULL DEFAULT '0',
    codetype        char(2)     NOT NULL DEFAULT '10',
    indcode         varchar(10) NOT NULL,
    avgmonthlyemp   numeric(12,2),
    totalwages      numeric(16,2),
    taxablewages    numeric(16,2),
    contributions   numeric(16,2),
    weeklywage      numeric(10,2),
    empcount        bigint,
    highempq        char(2),
    lowempq         char(2),
    supprecord      char(1)     NOT NULL DEFAULT '0',
    prelim          char(1)     NOT NULL DEFAULT '0',
    CONSTRAINT pk_industry
        PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                     ownership, codetype, indcode)
);

CREATE TABLE IF NOT EXISTS iowage (
    stfips          char(2)     NOT NULL DEFAULT '00',
    areatype        char(2)     NOT NULL DEFAULT '00',
    areatypeversion char(4)     NOT NULL DEFAULT '0',
    area            char(6)     NOT NULL DEFAULT '000000',
    periodyear      char(4)     NOT NULL,
    periodtype      char(2)     NOT NULL DEFAULT '01',
    period          char(2)     NOT NULL DEFAULT '00',
    indcodetype     char(2)     NOT NULL DEFAULT '10',
    indcode         varchar(10) NOT NULL DEFAULT '000000',
    occcodetype     char(2)     NOT NULL DEFAULT '19',
    occcode         varchar(10) NOT NULL,
    empcount        bigint,
    pct10           numeric(10,2),
    pct25           numeric(10,2),
    medianwage      numeric(10,2),
    meanwage        numeric(10,2),
    pct75           numeric(10,2),
    pct90           numeric(10,2),
    meanhourly      numeric(10,2),
    annualmean      numeric(12,2),
    supprecord      char(1)     NOT NULL DEFAULT '0',
    CONSTRAINT pk_iowage
        PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                     indcodetype, indcode, occcodetype, occcode)
);

CREATE TABLE IF NOT EXISTS projectionsmatrix (
    stfips              char(2)     NOT NULL DEFAULT '00',
    areatype            char(2)     NOT NULL DEFAULT '00',
    areatypeversion     char(4)     NOT NULL DEFAULT '0',
    area                char(6)     NOT NULL DEFAULT '000000',
    projectionsperiod   char(9)     NOT NULL,
    indcodetype         char(2)     NOT NULL DEFAULT '10',
    indcode             varchar(10) NOT NULL,
    occcodetype         char(2)     NOT NULL DEFAULT '19',
    occcode             varchar(10) NOT NULL,
    baseyearemp         numeric(12,2),
    projectedemp        numeric(12,2),
    change              numeric(12,2),
    pctchange           numeric(8,4),
    openings            numeric(12,2),
    supprecord          char(1)     NOT NULL DEFAULT '0',
    CONSTRAINT pk_projectionsmatrix
        PRIMARY KEY (stfips, areatype, areatypeversion, area, projectionsperiod,
                     indcodetype, indcode, occcodetype, occcode)
);

-- ---------------------------------------------------------------------------
-- Lookup tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS geographies (
    stfips          char(2)     NOT NULL,
    areatype        char(2)     NOT NULL,
    areatypeversion char(4)     NOT NULL DEFAULT '0',
    area            char(6)     NOT NULL,
    areatitle       varchar(80),
    areatypetitle   varchar(40),
    CONSTRAINT pk_geographies
        PRIMARY KEY (stfips, areatype, areatypeversion, area)
);

CREATE TABLE IF NOT EXISTS periodyears (
    stfips      char(2)     NOT NULL DEFAULT '00',
    periodyear  char(4)     NOT NULL,
    periodtype  char(2)     NOT NULL,
    period      char(2)     NOT NULL DEFAULT '00',
    periodtitle varchar(40),
    CONSTRAINT pk_periodyears
        PRIMARY KEY (stfips, periodyear, periodtype, period)
);

CREATE TABLE IF NOT EXISTS cescodes (
    stfips          char(2)     NOT NULL DEFAULT '00',
    seriescodetype  varchar(10) NOT NULL,
    seriescode      varchar(20) NOT NULL,
    seriestitle     varchar(120),
    indcode         varchar(10),
    indcodetype     char(2),
    CONSTRAINT pk_cescodes
        PRIMARY KEY (stfips, seriescodetype, seriescode)
);

CREATE TABLE IF NOT EXISTS inddirectories (
    stfips      char(2)     NOT NULL DEFAULT '00',
    projperiod  char(9)     NOT NULL,
    indcodetype char(2)     NOT NULL DEFAULT '10',
    indcode     varchar(10) NOT NULL,
    indtitle    varchar(120),
    CONSTRAINT pk_inddirectories
        PRIMARY KEY (stfips, projperiod, indcodetype, indcode)
);

CREATE TABLE IF NOT EXISTS occdirectories (
    stfips      char(2)     NOT NULL DEFAULT '00',
    projperiod  char(9)     NOT NULL,
    occcodetype char(2)     NOT NULL DEFAULT '19',
    occcode     varchar(10) NOT NULL,
    occtitle    varchar(120),
    CONSTRAINT pk_occdirectories
        PRIMARY KEY (stfips, projperiod, occcodetype, occcode)
);

-- ---------------------------------------------------------------------------
-- Administrative / operational tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS ingestlog (
    id              bigserial       PRIMARY KEY,
    dataset         varchar(20)     NOT NULL,
    runat           timestamptz     NOT NULL DEFAULT now(),
    recordsupserted integer,
    status          varchar(10)     NOT NULL DEFAULT 'ok',
    errormessage    text
);


