-- =============================================================================
-- wid-30-schema-postgres.sql
-- WID 3.0 Database Schema — PostgreSQL 14+ / Aurora PostgreSQL
--
-- Usage: psql -h <host> -U <user> -d <dbname> -f wid-30-schema-postgres.sql
--
-- All field names are lowercase. Flag fields are char(1) using '0'/'1'.
-- This script is idempotent: safe to re-run on an existing database.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Core data tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS laborforce (
    stfips              char(2)         NOT NULL DEFAULT '00',
    areatype            char(2)         NOT NULL DEFAULT '00',
    areatypeversion     char(4)         NOT NULL DEFAULT '0',
    area            char(6)         NOT NULL DEFAULT '000000',
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
    area            char(6)     NOT NULL DEFAULT '000000',
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
    wagesource      char(1)     NOT NULL DEFAULT '3',
    ratetype        char(1)     NOT NULL DEFAULT '2',
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
    area            char(6)     NOT NULL DEFAULT '000000',
    projectionsperiod   char(9)     NOT NULL,
    indcodetype         char(2)     NOT NULL DEFAULT '10',
    indcode             varchar(10) NOT NULL,
    occcodetype         char(2)     NOT NULL DEFAULT '19',
    occcode             varchar(10) NOT NULL,
    baseyearemp         numeric(12,2),
    projectedemp        numeric(12,2),
    change              numeric(12,2),
    pctchange           numeric(8,4),
    exits               numeric(12,2),
    annualexits         numeric(12,2),
    transfers           numeric(12,2),
    annualtransfers     numeric(12,2),
    openings            numeric(12,2),
    annualopenings      numeric(12,2),
    growthcode          char(2),
    supprecord          char(1)     NOT NULL DEFAULT '0',
    CONSTRAINT pk_projectionsmatrix
        PRIMARY KEY (stfips, areatype, areatypeversion, area, projectionsperiod,
                     indcodetype, indcode, occcodetype, occcode)
);

-- ---------------------------------------------------------------------------
-- Licensing tables (WID 3.0 — state-supplied data)
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS licenseauthorities (
    stfips          char(2)     NOT NULL,
    areatype        char(2)     NOT NULL,
    areatypeversion char(4)     NOT NULL DEFAULT '0',
    area            char(6)     NOT NULL,
    licauthid       varchar(20) NOT NULL,
    department      varchar(200),
    division        varchar(200),
    board           varchar(200),
    address1        varchar(200),
    city            varchar(120),
    state           char(2),
    zipcode         varchar(20),
    telephone       varchar(50),
    email           varchar(200),
    url             varchar(500),
    CONSTRAINT pk_licenseauthorities
        PRIMARY KEY (stfips, areatype, areatypeversion, area, licauthid)
);

CREATE TABLE IF NOT EXISTS license (
    stfips          char(2)     NOT NULL,
    licenseid       varchar(80) NOT NULL,
    licauthid       varchar(20),
    licensetitle    varchar(240),
    licensetype     varchar(20),
    exam            char(1)     NOT NULL DEFAULT '0',
    education       char(1)     NOT NULL DEFAULT '0',
    continuingedu   char(1)     NOT NULL DEFAULT '0',
    certification   char(1)     NOT NULL DEFAULT '0',
    experience      char(1)     NOT NULL DEFAULT '0',
    criminal        char(1)     NOT NULL DEFAULT '0',
    physicalreq     char(1)     NOT NULL DEFAULT '0',
    veteran         char(1)     NOT NULL DEFAULT '0',
    inactive        char(1)     NOT NULL DEFAULT '0',
    licenseurl      varchar(500),
    licenseupdated  char(8),
    CONSTRAINT pk_license
        PRIMARY KEY (stfips, licenseid)
);

CREATE TABLE IF NOT EXISTS licensehistory (
    stfips              char(2)     NOT NULL,
    areatype            char(2)     NOT NULL,
    areatypeversion     char(4)     NOT NULL DEFAULT '0',
    area            char(6)     NOT NULL,
    periodyear          char(4)     NOT NULL,
    periodtype          char(2)     NOT NULL,
    period              char(2)     NOT NULL,
    licenseid           varchar(80) NOT NULL,
    licensenumbertype   varchar(20),
    licensenumber       integer,
    CONSTRAINT pk_licensehistory
        PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype, period, licenseid)
);

CREATE TABLE IF NOT EXISTS licensexocc (
    stfips          char(2)     NOT NULL,
    licenseid       varchar(80) NOT NULL,
    occcodetype     char(2)     NOT NULL DEFAULT '19',
    occcode         varchar(10) NOT NULL,
    CONSTRAINT pk_licensexocc
        PRIMARY KEY (stfips, licenseid, occcodetype, occcode)
);

-- ---------------------------------------------------------------------------
-- Projection crosswalk tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS matrixxind (
    stfips          char(2)     NOT NULL DEFAULT '00',
    matrixindcode   varchar(10) NOT NULL,
    indcodetype     char(2)     NOT NULL DEFAULT '10',
    indcode         varchar(10) NOT NULL,
    CONSTRAINT pk_matrixxind PRIMARY KEY (stfips, matrixindcode, indcodetype, indcode)
);

CREATE TABLE IF NOT EXISTS matrixxocc (
    stfips          char(2)     NOT NULL DEFAULT '00',
    matrixocccode   varchar(10) NOT NULL,
    occcodetype     char(2)     NOT NULL DEFAULT '19',
    occcode         varchar(10) NOT NULL,
    CONSTRAINT pk_matrixxocc PRIMARY KEY (stfips, matrixocccode, occcodetype, occcode)
);

-- ---------------------------------------------------------------------------
-- Lookup / reference tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS geographies (
    stfips          char(2)     NOT NULL,
    areatype        char(2)     NOT NULL,
    areatypeversion char(4)     NOT NULL DEFAULT '0',
    area            char(6)     NOT NULL,
    areatitle       varchar(80),
    areatypetitle   varchar(40),
    CONSTRAINT pk_geographies PRIMARY KEY (stfips, areatype, areatypeversion, area)
);

CREATE TABLE IF NOT EXISTS areatypes (
    stfips          char(2)      NOT NULL,
    areatype        char(2)      NOT NULL,
    areatypename    varchar(120) NOT NULL,
    CONSTRAINT pk_areatypes PRIMARY KEY (stfips, areatype)
);

CREATE TABLE IF NOT EXISTS statefips (
    stfips          char(2)      NOT NULL,
    statename       varchar(120) NOT NULL,
    stateabbrev     char(2)      NOT NULL,
    CONSTRAINT pk_statefips PRIMARY KEY (stfips)
);

CREATE TABLE IF NOT EXISTS periodyears (
    stfips      char(2)     NOT NULL DEFAULT '00',
    periodyear  char(4)     NOT NULL,
    periodtype  char(2)     NOT NULL,
    period      char(2)     NOT NULL DEFAULT '00',
    periodtitle varchar(40),
    CONSTRAINT pk_periodyears PRIMARY KEY (stfips, periodyear, periodtype, period)
);

CREATE TABLE IF NOT EXISTS cescodes (
    stfips          char(2)     NOT NULL DEFAULT '00',
    seriescodetype  varchar(10) NOT NULL,
    seriescode      varchar(20) NOT NULL,
    seriestitle     varchar(120),
    indcode         varchar(10),
    indcodetype     char(2),
    CONSTRAINT pk_cescodes PRIMARY KEY (stfips, seriescodetype, seriescode)
);

CREATE TABLE IF NOT EXISTS inddirectories (
    stfips      char(2)     NOT NULL DEFAULT '00',
    projperiod  char(9)     NOT NULL,
    indcodetype char(2)     NOT NULL DEFAULT '10',
    indcode     varchar(10) NOT NULL,
    indtitle    varchar(120),
    CONSTRAINT pk_inddirectories PRIMARY KEY (stfips, projperiod, indcodetype, indcode)
);

CREATE TABLE IF NOT EXISTS occdirectories (
    stfips      char(2)     NOT NULL DEFAULT '00',
    projperiod  char(9)     NOT NULL,
    occcodetype char(2)     NOT NULL DEFAULT '19',
    occcode     varchar(10) NOT NULL,
    occtitle    varchar(120),
    CONSTRAINT pk_occdirectories PRIMARY KEY (stfips, projperiod, occcodetype, occcode)
);

-- ---------------------------------------------------------------------------
-- Performance indexes
-- ---------------------------------------------------------------------------

CREATE INDEX IF NOT EXISTS ix_laborforce_stfips_year   ON laborforce  (stfips, periodyear DESC);
CREATE INDEX IF NOT EXISTS ix_ces_stfips_year          ON ces         (stfips, periodyear DESC);
CREATE INDEX IF NOT EXISTS ix_industry_stfips_year     ON industry    (stfips, periodyear DESC);
CREATE INDEX IF NOT EXISTS ix_iowage_stfips_year       ON iowage      (stfips, periodyear DESC);
CREATE INDEX IF NOT EXISTS ix_projections_stfips       ON projectionsmatrix (stfips, projectionsperiod);
CREATE INDEX IF NOT EXISTS ix_geographies_stfips_at    ON geographies (stfips, areatype);
CREATE INDEX IF NOT EXISTS ix_inddirectories_stfips    ON inddirectories (stfips, indcodetype, indcode);
CREATE INDEX IF NOT EXISTS ix_occdirectories_stfips    ON occdirectories (stfips, occcodetype, occcode);
CREATE INDEX IF NOT EXISTS ix_cescodes_stfips          ON cescodes    (stfips, seriescodetype);
CREATE INDEX IF NOT EXISTS ix_periodyears_stfips_year  ON periodyears (stfips, periodyear, periodtype);

