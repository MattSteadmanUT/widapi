-- =============================================================================
-- 013_add_api_keys.sql
-- User-managed API keys for external system integration.
--
-- Design decisions:
--   1. Only a SHA-256 hex digest of the key is stored — plaintext is returned
--      exactly once at creation and never persisted.
--   2. The first 8 characters of the plaintext key are stored as a display prefix
--      so a user can recognize which key is which without exposing the secret.
--   3. Keys are scoped to a Cognito username so disabling the Cognito account
--      can disable all keys for that user at the application layer.
--   4. Rate profiles are stored as free-form labels resolved against a config
--      table so throttling limits can be updated without redeploying code.
-- =============================================================================

CREATE TABLE IF NOT EXISTS apikeys (
    keyid           uuid         NOT NULL DEFAULT gen_random_uuid(),
    username        varchar(128) NOT NULL,
    keyhash         char(64)     NOT NULL,       -- SHA-256 hex digest (lower-case)
    keyprefix       varchar(16)  NOT NULL,        -- first 8 chars of plaintext for display
    description     varchar(255),
    status          varchar(16)  NOT NULL DEFAULT 'active',  -- active | disabled | revoked | expired
    createdat       timestamptz  NOT NULL DEFAULT now(),
    expiresat       timestamptz,                 -- null = no expiry
    lastusedAt      timestamptz,
    revokedat       timestamptz,
    rateprofile     varchar(64)  NOT NULL DEFAULT 'standard',
    CONSTRAINT pk_apikeys PRIMARY KEY (keyid),
    CONSTRAINT uq_apikeys_keyhash UNIQUE (keyhash)
);

CREATE INDEX IF NOT EXISTS ix_apikeys_username ON apikeys(username);
CREATE INDEX IF NOT EXISTS ix_apikeys_keyhash  ON apikeys(keyhash);
CREATE INDEX IF NOT EXISTS ix_apikeys_status   ON apikeys(status);

-- Rate profiles define per-key throttle limits. Profiles are resolved by name.
-- Updating a row here immediately affects all keys on that profile.
CREATE TABLE IF NOT EXISTS apikeyratepolicies (
    profile         varchar(64) NOT NULL,
    requestspermin  integer     NOT NULL DEFAULT 60,
    requestsperday  integer     NOT NULL DEFAULT 10000,
    CONSTRAINT pk_apikeyratepolicies PRIMARY KEY (profile)
);

INSERT INTO apikeyratepolicies (profile, requestspermin, requestsperday) VALUES
    ('standard',  60,  10000),
    ('elevated',  300, 100000)
ON CONFLICT (profile) DO NOTHING;
