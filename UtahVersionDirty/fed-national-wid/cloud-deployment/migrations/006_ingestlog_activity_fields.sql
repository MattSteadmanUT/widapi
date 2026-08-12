-- =============================================================================
-- 006_ingestlog_activity_fields.sql
-- Adds richer table activity telemetry for status transparency and history.
-- =============================================================================

ALTER TABLE ingestlog
    ADD COLUMN IF NOT EXISTS recordschanged integer,
    ADD COLUMN IF NOT EXISTS recordsadded integer,
    ADD COLUMN IF NOT EXISTS totalrecords bigint,
    ADD COLUMN IF NOT EXISTS lastcheckedat timestamptz,
    ADD COLUMN IF NOT EXISTS lastchangedat timestamptz;

CREATE INDEX IF NOT EXISTS ix_ingestdetail_dataset_retrievedat
    ON ingestdetail (dataset, retrievedat DESC);

CREATE INDEX IF NOT EXISTS ix_ingestsourcehash_dataset_lastseenat
    ON ingestsourcehash (dataset, lastseenat DESC);
