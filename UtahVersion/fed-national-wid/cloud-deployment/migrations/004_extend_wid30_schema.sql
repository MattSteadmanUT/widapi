-- =============================================================================
-- 004_extend_wid30_schema.sql
-- Adds tables needed for full National WID API spec coverage and ETL source hashing.
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Projections crosswalk tables
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS matrixxind (
    stfips          char(2)     NOT NULL DEFAULT '00',
    matrixindcode   varchar(10) NOT NULL,
    indcodetype     char(2)     NOT NULL DEFAULT '10',
    indcode         varchar(10) NOT NULL,
    CONSTRAINT pk_matrixxind
        PRIMARY KEY (stfips, matrixindcode, indcodetype, indcode)
);

CREATE TABLE IF NOT EXISTS matrixxocc (
    stfips          char(2)     NOT NULL DEFAULT '00',
    matrixocccode   varchar(10) NOT NULL,
    occcodetype     char(2)     NOT NULL DEFAULT '19',
    occcode         varchar(10) NOT NULL,
    CONSTRAINT pk_matrixxocc
        PRIMARY KEY (stfips, matrixocccode, occcodetype, occcode)
);

-- ---------------------------------------------------------------------------
-- Licensing tables (WID 3.0)
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
    area                char(6)     NOT NULL,
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
-- ETL detail / source tracking
-- ---------------------------------------------------------------------------

ALTER TABLE ingestlog
    ADD COLUMN IF NOT EXISTS sourceurl text,
    ADD COLUMN IF NOT EXISTS sourcehash char(64),
    ADD COLUMN IF NOT EXISTS sourcechanged boolean,
    ADD COLUMN IF NOT EXISTS bytesdownloaded bigint;

CREATE TABLE IF NOT EXISTS ingestsourcehash (
    dataset         varchar(40) NOT NULL,
    sourceurl       text        NOT NULL,
    sourcehash      char(64)    NOT NULL,
    lastchangedat   timestamptz NOT NULL DEFAULT now(),
    lastseenat      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_ingestsourcehash
        PRIMARY KEY (dataset, sourceurl)
);

CREATE TABLE IF NOT EXISTS ingestdetail (
    id              bigserial   PRIMARY KEY,
    ingestlogid     bigint,
    dataset         varchar(40) NOT NULL,
    sourceurl       text        NOT NULL,
    sourcehash      char(64)    NOT NULL,
    sourcechanged   boolean     NOT NULL,
    bytesdownloaded bigint,
    retrievedat     timestamptz NOT NULL DEFAULT now(),
    errormessage    text,
    CONSTRAINT fk_ingestdetail_ingestlog
        FOREIGN KEY (ingestlogid) REFERENCES ingestlog (id) ON DELETE SET NULL
);

ALTER TABLE iowage
    ADD COLUMN IF NOT EXISTS wagesource char(1) NOT NULL DEFAULT '3',
    ADD COLUMN IF NOT EXISTS ratetype char(1) NOT NULL DEFAULT '2';

