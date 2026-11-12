-- Tables for the sample, created when the container first starts.

-- The chassis's HTTP idempotency store (see docs/web-api.md, "HTTP idempotency").
CREATE TABLE IF NOT EXISTS public.idempotency_records (
    idempotency_record_key text        PRIMARY KEY,
    status                 text        NOT NULL,
    fingerprint            text        NOT NULL,
    response_json          text        NULL,
    expires_at_utc         timestamptz NOT NULL,
    created_utc            timestamptz NOT NULL DEFAULT NOW(),
    updated_utc            timestamptz NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_idempotency_records_expires_at_utc
    ON public.idempotency_records (expires_at_utc);

-- The sample's own data.
CREATE TABLE IF NOT EXISTS public.orders (
    id       text    PRIMARY KEY,
    sku      text    NOT NULL,
    quantity integer NOT NULL
);
