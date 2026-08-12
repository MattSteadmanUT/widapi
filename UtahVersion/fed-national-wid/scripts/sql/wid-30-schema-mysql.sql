-- =============================================================================
-- wid-30-schema-mysql.sql
-- WID 3.0 Database Schema — MySQL 8.0+ / MariaDB 10.6+
--
-- Usage: mysql -h <host> -u <user> -p <dbname> < wid-30-schema-mysql.sql
--
-- Note: MySQL/MariaDB CHAR fields pad with spaces on read in some clients.
--       The application layer should always TRIM() char fields when querying.
-- =============================================================================

SET sql_mode = 'STRICT_TRANS_TABLES,NO_ENGINE_SUBSTITUTION';

-- ---------------------------------------------------------------------------
-- Core data tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS laborforce (
    stfips              CHAR(2)         NOT NULL DEFAULT '00',
    areatype            CHAR(2)         NOT NULL DEFAULT '00',
    areatypeversion     CHAR(4)         NOT NULL DEFAULT '0',
    AREA            CHAR(6)         NOT NULL DEFAULT '000000',
    periodyear          CHAR(4)         NOT NULL,
    periodtype          CHAR(2)         NOT NULL,
    period              CHAR(2)         NOT NULL DEFAULT '00',
    adjusted            CHAR(1)         NOT NULL DEFAULT '0',
    laborforce          BIGINT,
    employed            BIGINT,
    unemployed          BIGINT,
    unemprate           DECIMAL(8,4),
    supprecord          CHAR(1)         NOT NULL DEFAULT '0',
    supprate            CHAR(1)         NOT NULL DEFAULT '0',
    prelim              CHAR(1)         NOT NULL DEFAULT '0',
    PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype, period, adjusted)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS ces (
    stfips                      CHAR(2)     NOT NULL DEFAULT '00',
    areatype                    CHAR(2)     NOT NULL DEFAULT '00',
    areatypeversion             CHAR(4)     NOT NULL DEFAULT '0',
    AREA            CHAR(6)     NOT NULL DEFAULT '000000',
    periodyear                  CHAR(4)     NOT NULL,
    periodtype                  CHAR(2)     NOT NULL,
    period                      CHAR(2)     NOT NULL DEFAULT '00',
    adjusted                    CHAR(1)     NOT NULL DEFAULT '0',
    seriescodetype              VARCHAR(10) NOT NULL,
    seriescode                  VARCHAR(20) NOT NULL,
    empces                      BIGINT,
    empproductionworkers        BIGINT,
    hoursperweek                DECIMAL(6,1),
    earningsperweek             DECIMAL(8,2),
    earningsperhour             DECIMAL(8,2),
    avgweeklyearningspctchange  DECIMAL(8,4),
    supprecord                  CHAR(1)     NOT NULL DEFAULT '0',
    supphoursearnings           CHAR(1)     NOT NULL DEFAULT '0',
    suppprodworkers             CHAR(1)     NOT NULL DEFAULT '0',
    prelim                      CHAR(1)     NOT NULL DEFAULT '0',
    PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                 seriescodetype, seriescode, adjusted)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS industry (
    stfips          CHAR(2)     NOT NULL DEFAULT '00',
    areatype        CHAR(2)     NOT NULL DEFAULT '00',
    areatypeversion CHAR(4)     NOT NULL DEFAULT '0',
    AREA            CHAR(6)     NOT NULL DEFAULT '000000',
    periodyear      CHAR(4)     NOT NULL,
    periodtype      CHAR(2)     NOT NULL,
    period          CHAR(2)     NOT NULL DEFAULT '00',
    ownership       CHAR(1)     NOT NULL DEFAULT '0',
    codetype        CHAR(2)     NOT NULL DEFAULT '10',
    indcode         VARCHAR(10) NOT NULL,
    avgmonthlyemp   DECIMAL(12,2),
    totalwages      DECIMAL(16,2),
    taxablewages    DECIMAL(16,2),
    contributions   DECIMAL(16,2),
    weeklywage      DECIMAL(10,2),
    empcount        BIGINT,
    highempq        CHAR(2),
    lowempq         CHAR(2),
    supprecord      CHAR(1)     NOT NULL DEFAULT '0',
    prelim          CHAR(1)     NOT NULL DEFAULT '0',
    PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                 ownership, codetype, indcode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS iowage (
    stfips          CHAR(2)     NOT NULL DEFAULT '00',
    areatype        CHAR(2)     NOT NULL DEFAULT '00',
    areatypeversion CHAR(4)     NOT NULL DEFAULT '0',
    AREA            CHAR(6)     NOT NULL DEFAULT '000000',
    periodyear      CHAR(4)     NOT NULL,
    periodtype      CHAR(2)     NOT NULL DEFAULT '01',
    period          CHAR(2)     NOT NULL DEFAULT '00',
    indcodetype     CHAR(2)     NOT NULL DEFAULT '10',
    indcode         VARCHAR(10) NOT NULL DEFAULT '000000',
    occcodetype     CHAR(2)     NOT NULL DEFAULT '19',
    occcode         VARCHAR(10) NOT NULL,
    wagesource      CHAR(1)     NOT NULL DEFAULT '3',
    ratetype        CHAR(1)     NOT NULL DEFAULT '2',
    empcount        BIGINT,
    pct10           DECIMAL(10,2),
    pct25           DECIMAL(10,2),
    medianwage      DECIMAL(10,2),
    meanwage        DECIMAL(10,2),
    pct75           DECIMAL(10,2),
    pct90           DECIMAL(10,2),
    meanhourly      DECIMAL(10,2),
    annualmean      DECIMAL(12,2),
    supprecord      CHAR(1)     NOT NULL DEFAULT '0',
    PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                 indcodetype, indcode, occcodetype, occcode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS projectionsmatrix (
    stfips              CHAR(2)     NOT NULL DEFAULT '00',
    areatype            CHAR(2)     NOT NULL DEFAULT '00',
    areatypeversion     CHAR(4)     NOT NULL DEFAULT '0',
    AREA            CHAR(6)     NOT NULL DEFAULT '000000',
    projectionsperiod   CHAR(9)     NOT NULL,
    indcodetype         CHAR(2)     NOT NULL DEFAULT '10',
    indcode             VARCHAR(10) NOT NULL,
    occcodetype         CHAR(2)     NOT NULL DEFAULT '19',
    occcode             VARCHAR(10) NOT NULL,
    baseyearemp         DECIMAL(12,2),
    projectedemp        DECIMAL(12,2),
    `change`            DECIMAL(12,2),
    pctchange           DECIMAL(8,4),
    openings            DECIMAL(12,2),
    supprecord          CHAR(1)     NOT NULL DEFAULT '0',
    PRIMARY KEY (stfips, areatype, areatypeversion, area, projectionsperiod,
                 indcodetype, indcode, occcodetype, occcode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS license (
    stfips          CHAR(2)     NOT NULL,
    licenseid       VARCHAR(80) NOT NULL,
    licauthid       VARCHAR(20),
    licensetitle    VARCHAR(240),
    licensetype     VARCHAR(20),
    exam            CHAR(1)     NOT NULL DEFAULT '0',
    education       CHAR(1)     NOT NULL DEFAULT '0',
    continuingedu   CHAR(1)     NOT NULL DEFAULT '0',
    certification   CHAR(1)     NOT NULL DEFAULT '0',
    experience      CHAR(1)     NOT NULL DEFAULT '0',
    criminal        CHAR(1)     NOT NULL DEFAULT '0',
    physicalreq     CHAR(1)     NOT NULL DEFAULT '0',
    veteran         CHAR(1)     NOT NULL DEFAULT '0',
    inactive        CHAR(1)     NOT NULL DEFAULT '0',
    licenseurl      VARCHAR(500),
    licenseupdated  CHAR(8),
    PRIMARY KEY (stfips, licenseid)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---------------------------------------------------------------------------
-- Lookup / reference tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS geographies (
    stfips          CHAR(2)     NOT NULL,
    areatype        CHAR(2)     NOT NULL,
    areatypeversion CHAR(4)     NOT NULL DEFAULT '0',
    AREA            CHAR(6)     NOT NULL,
    areatitle       VARCHAR(80),
    areatypetitle   VARCHAR(40),
    PRIMARY KEY (stfips, areatype, areatypeversion, area)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS statefips (
    stfips      CHAR(2)      NOT NULL,
    statename   VARCHAR(120) NOT NULL,
    stateabbrev CHAR(2)      NOT NULL,
    PRIMARY KEY (stfips)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS periodyears (
    stfips      CHAR(2)     NOT NULL DEFAULT '00',
    periodyear  CHAR(4)     NOT NULL,
    periodtype  CHAR(2)     NOT NULL,
    period      CHAR(2)     NOT NULL DEFAULT '00',
    periodtitle VARCHAR(40),
    PRIMARY KEY (stfips, periodyear, periodtype, period)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS cescodes (
    stfips          CHAR(2)     NOT NULL DEFAULT '00',
    seriescodetype  VARCHAR(10) NOT NULL,
    seriescode      VARCHAR(20) NOT NULL,
    seriestitle     VARCHAR(120),
    indcode         VARCHAR(10),
    indcodetype     CHAR(2),
    PRIMARY KEY (stfips, seriescodetype, seriescode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS inddirectories (
    stfips      CHAR(2)     NOT NULL DEFAULT '00',
    projperiod  CHAR(9)     NOT NULL,
    indcodetype CHAR(2)     NOT NULL DEFAULT '10',
    indcode     VARCHAR(10) NOT NULL,
    indtitle    VARCHAR(120),
    PRIMARY KEY (stfips, projperiod, indcodetype, indcode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS occdirectories (
    stfips      CHAR(2)     NOT NULL DEFAULT '00',
    projperiod  CHAR(9)     NOT NULL,
    occcodetype CHAR(2)     NOT NULL DEFAULT '19',
    occcode     VARCHAR(10) NOT NULL,
    occtitle    VARCHAR(120),
    PRIMARY KEY (stfips, projperiod, occcodetype, occcode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ---------------------------------------------------------------------------
-- Performance indexes
-- ---------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS ix_laborforce_stfips_year  ON laborforce (stfips, periodyear);
CREATE INDEX IF NOT EXISTS ix_ces_stfips_year         ON ces (stfips, periodyear);
CREATE INDEX IF NOT EXISTS ix_industry_stfips_year    ON industry (stfips, periodyear);
CREATE INDEX IF NOT EXISTS ix_iowage_stfips_year      ON iowage (stfips, periodyear);
CREATE INDEX IF NOT EXISTS ix_geographies_stfips      ON geographies (stfips, areatype);

