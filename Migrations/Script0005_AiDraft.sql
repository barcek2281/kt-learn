-- Миграция 005 — ai_draft_test и ai_draft_tasks
-- Черновик, сгенерированный ИИ. Админ правит его здесь, до попадания в основные таблицы.
-- Колонка test_id добавляется позже, в Script0007 — таблицы tests ещё не существует.

CREATE TABLE ai_draft_test (
    id              uuid                PRIMARY KEY,
    pdf_to_test_id  uuid                NOT NULL,
    title           varchar(500)        NOT NULL,
    description     text                NULL,
    review_status   draft_review_status NOT NULL DEFAULT 'pending',
    review_comment  text                NULL,
    reviewed_by     uuid                NULL,
    reviewed_at     timestamptz         NULL,
    created_at      timestamptz         NOT NULL DEFAULT now(),
    updated_at      timestamptz         NOT NULL DEFAULT now(),

    CONSTRAINT fk_ai_draft_test_pdf
        FOREIGN KEY (pdf_to_test_id) REFERENCES pdf_to_test (id) ON DELETE CASCADE,

    CONSTRAINT fk_ai_draft_test_reviewer
        FOREIGN KEY (reviewed_by) REFERENCES users (id) ON DELETE SET NULL,

    CONSTRAINT ck_ai_draft_test_reviewed
        CHECK (
            (review_status = 'pending' AND reviewed_at IS NULL)
            OR (review_status <> 'pending' AND reviewed_at IS NOT NULL)
        ),

    CONSTRAINT ck_ai_draft_test_rejected_has_reason
        CHECK (review_status <> 'rejected' OR review_comment IS NOT NULL)
);

-- Связь один-к-одному: из одного PDF получается ровно один черновик.
CREATE UNIQUE INDEX ux_ai_draft_test_pdf
    ON ai_draft_test (pdf_to_test_id);

CREATE INDEX ix_ai_draft_test_review_status
    ON ai_draft_test (review_status, created_at DESC);


CREATE TABLE ai_draft_tasks (
    id                uuid         PRIMARY KEY,
    ai_draft_test_id  uuid         NOT NULL,
    theme_id          uuid         NULL,
    theme_raw         varchar(200) NULL,
    type              task_type    NOT NULL,
    question          text         NOT NULL,
    options           jsonb        NOT NULL,
    correct_answer    jsonb        NOT NULL,
    points            integer      NOT NULL DEFAULT 1,
    order_index       integer      NOT NULL,
    created_at        timestamptz  NOT NULL DEFAULT now(),
    updated_at        timestamptz  NOT NULL DEFAULT now(),

    CONSTRAINT fk_ai_draft_tasks_draft
        FOREIGN KEY (ai_draft_test_id) REFERENCES ai_draft_test (id) ON DELETE CASCADE,

    CONSTRAINT fk_ai_draft_tasks_theme
        FOREIGN KEY (theme_id) REFERENCES themes (id) ON DELETE SET NULL,

    CONSTRAINT ck_ai_draft_tasks_points
        CHECK (points > 0),

    CONSTRAINT ck_ai_draft_tasks_options_shape
        CHECK (jsonb_typeof(options) = 'array' AND jsonb_array_length(options) >= 2),

    CONSTRAINT ck_ai_draft_tasks_answer_shape
        CHECK (jsonb_typeof(correct_answer) = 'array'),

    CONSTRAINT ck_ai_draft_tasks_answer_count
        CHECK (
            (type = 'single' AND jsonb_array_length(correct_answer) = 1)
            OR (type = 'multi' AND jsonb_array_length(correct_answer) >= 2)
        )
);

CREATE INDEX ix_ai_draft_tasks_draft_order
    ON ai_draft_tasks (ai_draft_test_id, order_index);
