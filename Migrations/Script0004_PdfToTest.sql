-- Миграция 004 — pdf_to_test
-- Загруженный админом PDF и результат его обработки.

CREATE TABLE pdf_to_test (
    id            uuid          PRIMARY KEY,
    uploaded_by   uuid          NOT NULL,
    file_url      varchar(1000) NOT NULL,
    file_name     varchar(500)  NOT NULL,
    file_size     integer       NOT NULL,
    status        pdf_status    NOT NULL DEFAULT 'uploaded',
    error_message text          NULL,
    model         varchar(100)  NULL,
    created_at    timestamptz   NOT NULL DEFAULT now(),
    processed_at  timestamptz   NULL,

    -- RESTRICT: удаление админа не должно уносить историю загрузок.
    CONSTRAINT fk_pdf_to_test_user
        FOREIGN KEY (uploaded_by) REFERENCES users (id) ON DELETE RESTRICT,

    CONSTRAINT ck_pdf_to_test_file_size
        CHECK (file_size > 0),

    CONSTRAINT ck_pdf_to_test_failed_has_reason
        CHECK (status <> 'failed' OR error_message IS NOT NULL)
);

CREATE INDEX ix_pdf_to_test_status_created
    ON pdf_to_test (status, created_at DESC);

CREATE INDEX ix_pdf_to_test_uploaded_by
    ON pdf_to_test (uploaded_by);
