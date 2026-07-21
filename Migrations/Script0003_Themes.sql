-- Миграция 003 — themes
-- Справочник тем: ENG (английский), TGO и так далее.

CREATE TABLE themes (
    id         uuid         PRIMARY KEY,
    code       varchar(50)  NOT NULL,
    title      varchar(200) NOT NULL,
    is_active  boolean      NOT NULL DEFAULT true,
    created_at timestamptz  NOT NULL DEFAULT now(),
    updated_at timestamptz  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_themes_code ON themes (code);
