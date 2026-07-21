-- Миграция 007 — связь черновика с созданным тестом
-- Отдельным шагом: на момент Script0005 таблицы tests ещё не существует.

ALTER TABLE ai_draft_test
    ADD COLUMN test_id uuid NULL;

ALTER TABLE ai_draft_test
    ADD CONSTRAINT fk_ai_draft_test_test
        FOREIGN KEY (test_id) REFERENCES tests (id) ON DELETE SET NULL;

ALTER TABLE ai_draft_test
    ADD CONSTRAINT ck_ai_draft_test_approved_has_test
        CHECK (review_status <> 'approved' OR test_id IS NOT NULL);

-- Частичный уникальный индекс: один черновик порождает не более одного теста,
-- при этом непринятые черновики с test_id IS NULL не конфликтуют друг с другом.
CREATE UNIQUE INDEX ux_ai_draft_test_test
    ON ai_draft_test (test_id)
    WHERE test_id IS NOT NULL;
