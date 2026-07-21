-- Миграция 006 — tests, test_themes, tasks
-- Опубликованный контент. Записи создаются при аппруве черновика копированием из ai_draft_*.

CREATE TABLE tests (
    id           uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    title        varchar(500) NOT NULL,
    description  text         NULL,
    is_published boolean      NOT NULL DEFAULT false,
    published_at timestamptz  NULL,
    created_at   timestamptz  NOT NULL DEFAULT now(),
    updated_at   timestamptz  NOT NULL DEFAULT now(),

    -- is_published управляет видимостью сейчас, published_at хранит дату первой
    -- публикации. При снятии с публикации сбрасывается только флаг.
    CONSTRAINT ck_tests_published
        CHECK (is_published = false OR published_at IS NOT NULL)
);

CREATE INDEX ix_tests_published
    ON tests (is_published, published_at DESC);


CREATE TABLE test_themes (
    test_id  uuid NOT NULL,
    theme_id uuid NOT NULL,

    CONSTRAINT pk_test_themes PRIMARY KEY (test_id, theme_id),

    CONSTRAINT fk_test_themes_test
        FOREIGN KEY (test_id) REFERENCES tests (id) ON DELETE CASCADE,

    CONSTRAINT fk_test_themes_theme
        FOREIGN KEY (theme_id) REFERENCES themes (id) ON DELETE RESTRICT
);

-- Составной PK уже покрывает поиск по test_id. Этот индекс — для обратного
-- направления: «все тесты по этой теме».
CREATE INDEX ix_test_themes_theme ON test_themes (theme_id);


CREATE TABLE tasks (
    id             uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    test_id        uuid         NOT NULL,
    theme_id       uuid         NULL,
    type           task_type    NOT NULL,
    question       text         NOT NULL,
    options        jsonb        NOT NULL,
    correct_answer jsonb        NOT NULL,
    scoring_mode   scoring_mode NOT NULL DEFAULT 'all_or_nothing',
    points         integer      NOT NULL DEFAULT 1,
    order_index    integer      NOT NULL,
    created_at     timestamptz  NOT NULL DEFAULT now(),
    updated_at     timestamptz  NOT NULL DEFAULT now(),

    CONSTRAINT fk_tasks_test
        FOREIGN KEY (test_id) REFERENCES tests (id) ON DELETE CASCADE,

    CONSTRAINT fk_tasks_theme
        FOREIGN KEY (theme_id) REFERENCES themes (id) ON DELETE SET NULL,

    CONSTRAINT ck_tasks_points
        CHECK (points > 0),

    CONSTRAINT ck_tasks_options_shape
        CHECK (jsonb_typeof(options) = 'array' AND jsonb_array_length(options) >= 2),

    CONSTRAINT ck_tasks_answer_shape
        CHECK (jsonb_typeof(correct_answer) = 'array'),

    CONSTRAINT ck_tasks_answer_count
        CHECK (
            (type = 'single' AND jsonb_array_length(correct_answer) = 1)
            OR (type = 'multi' AND jsonb_array_length(correct_answer) >= 2)
        )
);

-- Не даёт двум вопросам занять одну позицию. Перестановка вопросов в админке
-- требует обновления всей пачки в транзакции либо временных отрицательных значений.
CREATE UNIQUE INDEX ux_tasks_test_order
    ON tasks (test_id, order_index);

-- theme_id нужен для разбивки результата по темам: «английский 8/10, ТГО 4/10».
CREATE INDEX ix_tasks_theme ON tasks (theme_id);
