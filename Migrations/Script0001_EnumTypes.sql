-- Миграция 001 — ENUM-типы
-- Отдельным шагом, чтобы типы существовали до первой таблицы, которая на них ссылается.

CREATE TYPE user_role AS ENUM ('student', 'admin');
CREATE TYPE pdf_status AS ENUM ('uploaded', 'generated', 'failed');
CREATE TYPE draft_review_status AS ENUM ('pending', 'approved', 'rejected');
CREATE TYPE completion_status AS ENUM ('in_progress', 'completed');
CREATE TYPE task_type AS ENUM ('single', 'multi');
CREATE TYPE scoring_mode AS ENUM ('all_or_nothing', 'partial');
