-- Миграция 008 — test_completion и task_completion
-- Прохождение тестов пользователями.

CREATE TABLE test_completion (
    id          uuid              PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     uuid              NOT NULL,
    test_id     uuid              NOT NULL,
    status      completion_status NOT NULL DEFAULT 'in_progress',
    attempt_no  integer           NOT NULL DEFAULT 1,
    score       integer           NOT NULL DEFAULT 0,
    tasks_done  integer           NOT NULL DEFAULT 0,
    tasks_total integer           NOT NULL,
    started_at  timestamptz       NOT NULL DEFAULT now(),
    finished_at timestamptz       NULL,

    CONSTRAINT fk_test_completion_user
        FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,

    CONSTRAINT fk_test_completion_test
        FOREIGN KEY (test_id) REFERENCES tests (id) ON DELETE CASCADE,

    CONSTRAINT ck_test_completion_counters
        CHECK (tasks_done >= 0 AND tasks_total > 0 AND tasks_done <= tasks_total),

    CONSTRAINT ck_test_completion_attempt
        CHECK (attempt_no > 0),

    CONSTRAINT ck_test_completion_finished
        CHECK (
            (status = 'in_progress' AND finished_at IS NULL)
            OR (status = 'completed' AND finished_at IS NOT NULL)
        )
);

-- Ограничить одной попыткой на тест можно заменой этого индекса на (user_id, test_id) —
-- тогда колонка attempt_no становится не нужна.
CREATE UNIQUE INDEX ux_test_completion_attempt
    ON test_completion (user_id, test_id, attempt_no);

CREATE INDEX ix_test_completion_user
    ON test_completion (user_id, started_at DESC);

CREATE INDEX ix_test_completion_test
    ON test_completion (test_id);


CREATE TABLE task_completion (
    id                 uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    test_completion_id uuid         NOT NULL,
    task_id            uuid         NOT NULL,
    answer             jsonb        NOT NULL,
    is_correct         boolean      NOT NULL,
    score_ratio        numeric(5,4) NOT NULL DEFAULT 0,
    points_awarded     integer      NOT NULL DEFAULT 0,
    answered_at        timestamptz  NOT NULL DEFAULT now(),

    CONSTRAINT fk_task_completion_attempt
        FOREIGN KEY (test_completion_id) REFERENCES test_completion (id) ON DELETE CASCADE,

    CONSTRAINT fk_task_completion_task
        FOREIGN KEY (task_id) REFERENCES tasks (id) ON DELETE CASCADE,

    CONSTRAINT ck_task_completion_answer_shape
        CHECK (jsonb_typeof(answer) = 'array'),

    CONSTRAINT ck_task_completion_ratio
        CHECK (score_ratio >= 0 AND score_ratio <= 1),

    CONSTRAINT ck_task_completion_points
        CHECK (points_awarded >= 0)
);

-- Один ответ на вопрос в рамках попытки. Изменение ответа до сдачи — UPDATE,
-- а не вставка новой строки.
CREATE UNIQUE INDEX ux_task_completion_task
    ON task_completion (test_completion_id, task_id);

CREATE INDEX ix_task_completion_task
    ON task_completion (task_id);
