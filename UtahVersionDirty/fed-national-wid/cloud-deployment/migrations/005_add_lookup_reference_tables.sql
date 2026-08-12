-- =============================================================================
-- 005_add_lookup_reference_tables.sql
-- DB-backed non-core lookup reference tables populated from data.widcenter.org.
-- =============================================================================

CREATE TABLE IF NOT EXISTS areatypes (
    stfips          char(2)      NOT NULL,
    areatype        char(2)      NOT NULL,
    areatypename    varchar(120) NOT NULL,
    CONSTRAINT pk_areatypes
        PRIMARY KEY (stfips, areatype)
);

CREATE TABLE IF NOT EXISTS statefips (
    stfips          char(2)      NOT NULL,
    statename       varchar(120) NOT NULL,
    stateabbrev     char(2)      NOT NULL,
    CONSTRAINT pk_statefips
        PRIMARY KEY (stfips)
);
