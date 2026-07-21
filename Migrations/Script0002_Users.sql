-- Миграция 002 — users

CREATE TABLE users (
    id            uuid         PRIMARY KEY,
    email         varchar(255) NOT NULL,
    password_hash varchar(255) NOT NULL,
    name          varchar(200) NOT NULL,
    role          user_role    NOT NULL DEFAULT 'student',
    created_at    timestamptz  NOT NULL DEFAULT now(),
    updated_at    timestamptz  NOT NULL DEFAULT now()
);

-- По lower(email), иначе User@mail.com и user@mail.com станут разными пользователями.
-- В коде при поиске тоже приводить к нижнему регистру, иначе индекс не применится.
CREATE UNIQUE INDEX ux_users_email ON users (lower(email));
