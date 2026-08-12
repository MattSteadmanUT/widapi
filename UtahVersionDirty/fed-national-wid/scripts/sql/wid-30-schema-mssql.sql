-- =============================================================================
-- wid-30-schema-mssql.sql
-- WID 3.0 Database Schema — SQL Server 2016+ / Azure SQL
--
-- Usage: sqlcmd -S <server> -d <dbname> -U <user> -P <pass> -i wid-30-schema-mssql.sql
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Core data tables
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'laborforce')
CREATE TABLE dbo.laborforce (
    stfips              CHAR(2)         NOT NULL CONSTRAINT df_lf_stfips  DEFAULT '00',
    areatype            CHAR(2)         NOT NULL CONSTRAINT df_lf_at      DEFAULT '00',
    areatypeversion     CHAR(4)         NOT NULL CONSTRAINT df_lf_atv     DEFAULT '0',
    area                CHAR(6)         NOT NULL CONSTRAINT df_lf_area    DEFAULT '000000',
    periodyear          CHAR(4)         NOT NULL,
    periodtype          CHAR(2)         NOT NULL,
    period              CHAR(2)         NOT NULL CONSTRAINT df_lf_period  DEFAULT '00',
    adjusted            CHAR(1)         NOT NULL CONSTRAINT df_lf_adj     DEFAULT '0',
    laborforce          BIGINT,
    employed            BIGINT,
    unemployed          BIGINT,
    unemprate           DECIMAL(8,4),
    supprecord          CHAR(1)         NOT NULL CONSTRAINT df_lf_sr      DEFAULT '0',
    supprate            CHAR(1)         NOT NULL CONSTRAINT df_lf_srate   DEFAULT '0',
    prelim              CHAR(1)         NOT NULL CONSTRAINT df_lf_prelim  DEFAULT '0',
    CONSTRAINT pk_laborforce PRIMARY KEY (stfips, areatype, areatypeversion, area,
                                          periodyear, periodtype, period, adjusted)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ces')
CREATE TABLE dbo.ces (
    stfips                      CHAR(2)     NOT NULL DEFAULT '00',
    areatype                    CHAR(2)     NOT NULL DEFAULT '00',
    areatypeversion             CHAR(4)     NOT NULL DEFAULT '0',
    area                        CHAR(6)     NOT NULL DEFAULT '000000',
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
    CONSTRAINT pk_ces PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype,
                                    period, seriescodetype, seriescode, adjusted)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'industry')
CREATE TABLE dbo.industry (
    stfips          CHAR(2)     NOT NULL DEFAULT '00',
    areatype        CHAR(2)     NOT NULL DEFAULT '00',
    areatypeversion CHAR(4)     NOT NULL DEFAULT '0',
    area            CHAR(6)     NOT NULL DEFAULT '000000',
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
    CONSTRAINT pk_industry PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear,
                                         periodtype, period, ownership, codetype, indcode)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'iowage')
CREATE TABLE dbo.iowage (
    stfips          CHAR(2)     NOT NULL DEFAULT '00',
    areatype        CHAR(2)     NOT NULL DEFAULT '00',
    areatypeversion CHAR(4)     NOT NULL DEFAULT '0',
    area            CHAR(6)     NOT NULL DEFAULT '000000',
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
    CONSTRAINT pk_iowage PRIMARY KEY (stfips, areatype, areatypeversion, area, periodyear, periodtype,
                                       period, indcodetype, indcode, occcodetype, occcode)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'projectionsmatrix')
CREATE TABLE dbo.projectionsmatrix (
    stfips              CHAR(2)     NOT NULL DEFAULT '00',
    areatype            CHAR(2)     NOT NULL DEFAULT '00',
    areatypeversion     CHAR(4)     NOT NULL DEFAULT '0',
    area                CHAR(6)     NOT NULL DEFAULT '000000',
    projectionsperiod   CHAR(9)     NOT NULL,
    indcodetype         CHAR(2)     NOT NULL DEFAULT '10',
    indcode             VARCHAR(10) NOT NULL,
    occcodetype         CHAR(2)     NOT NULL DEFAULT '19',
    occcode             VARCHAR(10) NOT NULL,
    baseyearemp         DECIMAL(12,2),
    projectedemp        DECIMAL(12,2),
    [change]            DECIMAL(12,2),
    pctchange           DECIMAL(8,4),
    openings            DECIMAL(12,2),
    supprecord          CHAR(1)     NOT NULL DEFAULT '0',
    CONSTRAINT pk_projectionsmatrix PRIMARY KEY (stfips, areatype, areatypeversion, area,
                                                  projectionsperiod, indcodetype, indcode,
                                                  occcodetype, occcode)
);
GO

-- ---------------------------------------------------------------------------
-- Lookup tables
-- ---------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'geographies')
CREATE TABLE dbo.geographies (
    stfips          CHAR(2)     NOT NULL,
    areatype        CHAR(2)     NOT NULL,
    areatypeversion CHAR(4)     NOT NULL DEFAULT '0',
    area            CHAR(6)     NOT NULL,
    areatitle       VARCHAR(80),
    areatypetitle   VARCHAR(40),
    CONSTRAINT pk_geographies PRIMARY KEY (stfips, areatype, areatypeversion, area)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'statefips')
CREATE TABLE dbo.statefips (
    stfips      CHAR(2)      NOT NULL,
    statename   NVARCHAR(120) NOT NULL,
    stateabbrev CHAR(2)      NOT NULL,
    CONSTRAINT pk_statefips PRIMARY KEY (stfips)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'periodyears')
CREATE TABLE dbo.periodyears (
    stfips      CHAR(2)     NOT NULL DEFAULT '00',
    periodyear  CHAR(4)     NOT NULL,
    periodtype  CHAR(2)     NOT NULL,
    period      CHAR(2)     NOT NULL DEFAULT '00',
    periodtitle VARCHAR(40),
    CONSTRAINT pk_periodyears PRIMARY KEY (stfips, periodyear, periodtype, period)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'cescodes')
CREATE TABLE dbo.cescodes (
    stfips          CHAR(2)     NOT NULL DEFAULT '00',
    seriescodetype  VARCHAR(10) NOT NULL,
    seriescode      VARCHAR(20) NOT NULL,
    seriestitle     VARCHAR(120),
    indcode         VARCHAR(10),
    indcodetype     CHAR(2),
    CONSTRAINT pk_cescodes PRIMARY KEY (stfips, seriescodetype, seriescode)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'inddirectories')
CREATE TABLE dbo.inddirectories (
    stfips      CHAR(2)     NOT NULL DEFAULT '00',
    projperiod  CHAR(9)     NOT NULL,
    indcodetype CHAR(2)     NOT NULL DEFAULT '10',
    indcode     VARCHAR(10) NOT NULL,
    indtitle    NVARCHAR(120),
    CONSTRAINT pk_inddirectories PRIMARY KEY (stfips, projperiod, indcodetype, indcode)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'occdirectories')
CREATE TABLE dbo.occdirectories (
    stfips      CHAR(2)     NOT NULL DEFAULT '00',
    projperiod  CHAR(9)     NOT NULL,
    occcodetype CHAR(2)     NOT NULL DEFAULT '19',
    occcode     VARCHAR(10) NOT NULL,
    occtitle    NVARCHAR(120),
    CONSTRAINT pk_occdirectories PRIMARY KEY (stfips, projperiod, occcodetype, occcode)
);
GO

-- ---------------------------------------------------------------------------
-- Performance indexes
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_laborforce_stfips_year')
    CREATE INDEX ix_laborforce_stfips_year ON dbo.laborforce (stfips, periodyear DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_ces_stfips_year')
    CREATE INDEX ix_ces_stfips_year ON dbo.ces (stfips, periodyear DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_industry_stfips_year')
    CREATE INDEX ix_industry_stfips_year ON dbo.industry (stfips, periodyear DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_iowage_stfips_year')
    CREATE INDEX ix_iowage_stfips_year ON dbo.iowage (stfips, periodyear DESC);
GO

