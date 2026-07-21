-- Миграция 009 — tasks_ai_help
-- Диалог с ИИ-помощником по конкретному вопросу.

CREATE TABLE tasks_ai_help (
    id                 uuid         PRIMARY KEY,
    task_completion_id uuid         NOT NULL,
    user_prompt        text         NOT NULL,
    ai_response        text         NOT NULL,
    model              varchar(100) NOT NULL,
    tokens_used        integer      NOT NULL DEFAULT 0,
    created_at         timestamptz  NOT NULL DEFAULT now(),

    -- Привязка к task_completion, а не к tasks: подсказка относится к конкретному
    -- ответу конкретного пользователя.
    CONSTRAINT fk_tasks_ai_help_task_completion
        FOREIGN KEY (task_completion_id) REFERENCES task_completion (id) ON DELETE CASCADE,

    CONSTRAINT ck_tasks_ai_help_tokens
        CHECK (tokens_used >= 0)
);

-- Несколько строк на одну запись образуют переписку, порядок задаёт created_at.
CREATE INDEX ix_tasks_ai_help_task_completion
    ON tasks_ai_help (task_completion_id, created_at);
