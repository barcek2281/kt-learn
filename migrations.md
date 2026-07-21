# Миграции БД: платформа тестов с ИИ-генерацией

## Контекст

- **СУБД:** PostgreSQL 14+
- **ORM:** EF Core (Npgsql)
- **Приложение:** монолит на C#
- Генерация теста из PDF выполняется синхронно внутри процесса

Миграции разбиты на 8 независимых шагов. Каждый шаг — отдельная миграция EF Core.
Порядок обязателен: есть внешние ключи между шагами.

**Общие правила для всех таблиц:**

- Первичные ключи — `uuid`, генерация на стороне приложения (`Guid.CreateVersion7()` для
  временной упорядоченности), не `gen_random_uuid()`
- Все временные метки — `timestamptz`, значения в UTC
- `created_at` — `DEFAULT now()`
- `updated_at` — проставляется приложением через перехватчик `SaveChangesAsync`,
  не через триггер БД
- Имена таблиц и колонок — `snake_case`

---

## ENUM-типы

Все статусы реализуются нативными enum-типами PostgreSQL, а не строками.
БД тогда сама не даст записать опечатку. Добавление нового значения потом
требует миграции с `ALTER TYPE ... ADD VALUE`.

### `user_role`

Роль пользователя.

| Значение | Описание |
|---|---|
| `student` | Обычный пользователь, проходит тесты |
| `admin` | Загружает PDF, проверяет черновики, публикует тесты |

### `pdf_status`

Состояние обработки загруженного PDF. Переходы: `uploaded` → `generated` \| `failed`.

| Значение | Описание |
|---|---|
| `uploaded` | Файл сохранён, запрос к ИИ выполняется. Если запись зависла в этом статусе — процесс упал во время обработки |
| `generated` | ИИ вернула результат, черновик `ai_draft_test` создан |
| `failed` | Ошибка обработки, причина в `error_message`. Повторная попытка возвращает статус в `uploaded` |

### `draft_review_status`

Решение админа по черновику. Переходы: `pending` → `approved` \| `rejected`.

| Значение | Описание |
|---|---|
| `pending` | Черновик ждёт проверки |
| `approved` | Принят. Созданы записи в `tests` и `tasks`, `ai_draft_test.test_id` заполнен |
| `rejected` | Отклонён целиком, причина в `review_comment`. Тест не создаётся |

### `completion_status`

Состояние попытки прохождения. Переходы: `in_progress` → `completed`.

| Значение | Описание |
|---|---|
| `not_started` | Неначито |
| `in_progress` | Попытка идёт, `finished_at` пустой |
| `completed` | Пользователь завершил тест, `finished_at` заполнен |

### `task_type`

Тип вопроса. Определяет и виджет на фронте, и правило валидации `correct_answer`.

| Значение | Описание | Длина `correct_answer` |
|---|---|---|
| `single` | Один правильный ответ, радиокнопки | ровно 1 |
| `multi` | Несколько правильных, чекбоксы | 2 и более |

### `scoring_mode`

Начисление баллов за вопрос типа `multi`. Для `single` не влияет ни на что.

| Значение | Описание |
|---|---|
| `all_or_nothing` | Балл начисляется, только если набор ответов совпал точно. Значение по умолчанию |
| `partial` | Балл пропорционален доле верных ответов, лишние выбранные варианты вычитаются, итог не ниже нуля |

---

## Формат JSONB-полей

Три поля хранят JSON: `options`, `correct_answer` (в `tasks` и `ai_draft_tasks`)
и `answer` (в `task_completion`).

### `options`

Массив вариантов ответа. Поле `id` — стабильный идентификатор варианта внутри вопроса.

```json
[
  { "id": "a", "text": "Алматы" },
  { "id": "b", "text": "Астана" },
  { "id": "c", "text": "Шымкент" }
]
```

### `correct_answer` и `answer`

Массив строк — идентификаторов из `options`.

```json
["b"]
```

```json
["a", "c", "d"]
```

**Критично:** ссылаться нужно на `id`, а не на позицию в массиве. Админ может поменять
варианты местами при редактировании — при использовании индексов все ранее сохранённые
`task_completion.answer` молча станут неправильными.

Проверка ответа: отсортировать оба массива и сравнить.

---

## Миграция 001 — ENUM-типы

Создаёт все пользовательские типы. Отдельной миграцией, чтобы типы существовали
до первой таблицы, которая на них ссылается.

```sql
CREATE TYPE user_role AS ENUM ('student', 'admin');
CREATE TYPE pdf_status AS ENUM ('uploaded', 'generated', 'failed');
CREATE TYPE draft_review_status AS ENUM ('pending', 'approved', 'rejected');
CREATE TYPE completion_status AS ENUM ('in_progress', 'completed');
CREATE TYPE task_type AS ENUM ('single', 'multi');
CREATE TYPE scoring_mode AS ENUM ('all_or_nothing', 'partial');
```

**Down:** `DROP TYPE` для каждого в обратном порядке.

**EF Core:** зарегистрировать в `AddNpgsql`:

```csharp
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.MapEnum<UserRole>("user_role");
        npgsql.MapEnum<PdfStatus>("pdf_status");
        npgsql.MapEnum<DraftReviewStatus>("draft_review_status");
        npgsql.MapEnum<CompletionStatus>("completion_status");
        npgsql.MapEnum<TaskType>("task_type");
        npgsql.MapEnum<ScoringMode>("scoring_mode");
    }));
```

Имена значений C#-енумов по умолчанию переводятся Npgsql в `snake_case`,
поэтому `ScoringMode.AllOrNothing` ложится в `all_or_nothing` без ручного маппинга.

---

## Миграция 002 — `users`

```sql
CREATE TABLE users (
    id            uuid PRIMARY KEY,
    email         varchar(255) NOT NULL,
    password_hash varchar(255) NOT NULL,
    name          varchar(200) NOT NULL,
    role          user_role    NOT NULL DEFAULT 'student',
    created_at    timestamptz  NOT NULL DEFAULT now(),
    updated_at    timestamptz  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_users_email ON users (lower(email));
```

Индекс по `lower(email)`, а не по `email` — иначе `User@mail.com` и `user@mail.com`
окажутся разными пользователями. При поиске в коде тоже приводить к нижнему регистру.

---

## Миграция 003 — `themes`

Справочник тем: `ENG` (английский), `TGO` и так далее.

```sql
CREATE TABLE themes (
    id         uuid         PRIMARY KEY,
    code       varchar(50)  NOT NULL,
    title      varchar(200) NOT NULL,
    is_active  boolean      NOT NULL DEFAULT true,
    created_at timestamptz  NOT NULL DEFAULT now(),
    updated_at timestamptz  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_themes_code ON themes (code);
```

`is_active` вместо удаления: тему, на которую ссылаются существующие задания,
удалить не выйдет, а скрыть из выпадающих списков нужно.

---

## Миграция 004 — `pdf_to_test`

Загруженный админом PDF и результат его обработки.

```sql
CREATE TABLE pdf_to_test (
    id            uuid         PRIMARY KEY,
    uploaded_by   uuid         NOT NULL,
    file_url      varchar(1000) NOT NULL,
    file_name     varchar(500) NOT NULL,
    file_size     integer      NOT NULL,
    status        pdf_status   NOT NULL DEFAULT 'uploaded',
    error_message text         NULL,
    model         varchar(100) NULL,
    created_at    timestamptz  NOT NULL DEFAULT now(),
    processed_at  timestamptz  NULL,

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
```

`processed_at` заполняется в момент перехода в `generated` или `failed` —
по разнице с `created_at` видно, сколько заняла генерация.

`ON DELETE RESTRICT` на `uploaded_by`: удаление админа не должно уносить историю загрузок.

---

## Миграция 005 — `ai_draft_test` и `ai_draft_tasks`

Черновик, сгенерированный ИИ. Админ правит его здесь, до попадания в основные таблицы.
Колонка `test_id` добавляется позже, в миграции 007 — таблицы `tests` ещё не существует.

```sql
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

CREATE UNIQUE INDEX ux_ai_draft_test_pdf
    ON ai_draft_test (pdf_to_test_id);

CREATE INDEX ix_ai_draft_test_review_status
    ON ai_draft_test (review_status, created_at DESC);
```

Уникальный индекс по `pdf_to_test_id` обеспечивает связь один-к-одному:
из одного PDF получается ровно один черновик.

```sql
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
```

`theme_id` допускает NULL: ИИ возвращает название темы текстом, а не идентификатором
из справочника. Сырое значение кладётся в `theme_raw`, сопоставление с `themes`
делает админ на ревью либо автомаппинг по `themes.code`.

Отклонение отдельного вопроса реализуется удалением строки. `ON DELETE CASCADE`
от `ai_draft_test` очищает все вопросы при удалении черновика.

Факт правки виден по `updated_at > created_at`, отдельный флаг не нужен.

---

## Миграция 006 — `tests`, `test_themes`, `tasks`

Опубликованный контент. Записи создаются при аппруве черновика копированием
из `ai_draft_*`.

```sql
CREATE TABLE tests (
    id           uuid         PRIMARY KEY,
    title        varchar(500) NOT NULL,
    description  text         NULL,
    is_published boolean      NOT NULL DEFAULT false,
    published_at timestamptz  NULL,
    created_at   timestamptz  NOT NULL DEFAULT now(),
    updated_at   timestamptz  NOT NULL DEFAULT now(),

    CONSTRAINT ck_tests_published
        CHECK (is_published = false OR published_at IS NOT NULL)
);

CREATE INDEX ix_tests_published
    ON tests (is_published, published_at DESC);
```

`is_published` и `published_at` — разные вещи. Флаг управляет видимостью здесь и сейчас,
метка хранит дату первой публикации. При снятии теста с публикации сбрасывается только
флаг, дата остаётся.

```sql
CREATE TABLE test_themes (
    test_id  uuid NOT NULL,
    theme_id uuid NOT NULL,

    CONSTRAINT pk_test_themes PRIMARY KEY (test_id, theme_id),

    CONSTRAINT fk_test_themes_test
        FOREIGN KEY (test_id) REFERENCES tests (id) ON DELETE CASCADE,

    CONSTRAINT fk_test_themes_theme
        FOREIGN KEY (theme_id) REFERENCES themes (id) ON DELETE RESTRICT
);

CREATE INDEX ix_test_themes_theme ON test_themes (theme_id);
```

Составной первичный ключ уже покрывает поиск по `test_id`. Отдельный индекс по
`theme_id` нужен для обратного направления — «все тесты по этой теме».

Комбинация тем у теста получается набором строк: тест с темами `ENG` и `TGO` —
это две записи. Запрос «тесты ровно по ENG и TGO»:

```sql
SELECT t.*
FROM tests t
JOIN test_themes tt ON tt.test_id = t.id
JOIN themes th ON th.id = tt.theme_id
WHERE t.is_published
GROUP BY t.id
HAVING array_agg(th.code ORDER BY th.code) = ARRAY['ENG', 'TGO'];
```

```sql
CREATE TABLE tasks (
    id             uuid         PRIMARY KEY,
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

CREATE UNIQUE INDEX ux_tasks_test_order
    ON tasks (test_id, order_index);

CREATE INDEX ix_tasks_theme ON tasks (theme_id);
```

`order_index` задаёт порядок вопросов при выдаче: `ORDER BY order_index`.
Без него порядок строк не определён и вопросы будут перемешиваться между заходами.

Уникальность пары `(test_id, order_index)` не даёт двум вопросам занять одну позицию.
Перестановка вопросов в админке требует обновления всей пачки внутри транзакции,
либо использования отрицательных временных значений во избежание конфликта индекса.

`tasks.theme_id` нужен для разбивки результата по темам: «английский 8/10, ТГО 4/10».
Без него по тесту с несколькими темами видно только общий балл.

---

## Миграция 007 — связь черновика с созданным тестом

Добавляет `ai_draft_test.test_id`. Отдельным шагом, потому что на момент миграции 005
таблицы `tests` ещё нет.

```sql
ALTER TABLE ai_draft_test
    ADD COLUMN test_id uuid NULL;

ALTER TABLE ai_draft_test
    ADD CONSTRAINT fk_ai_draft_test_test
        FOREIGN KEY (test_id) REFERENCES tests (id) ON DELETE SET NULL;

ALTER TABLE ai_draft_test
    ADD CONSTRAINT ck_ai_draft_test_approved_has_test
        CHECK (review_status <> 'approved' OR test_id IS NOT NULL);

CREATE UNIQUE INDEX ux_ai_draft_test_test
    ON ai_draft_test (test_id)
    WHERE test_id IS NOT NULL;
```

Частичный уникальный индекс: один черновик порождает не более одного теста,
при этом непринятые черновики с `test_id IS NULL` не конфликтуют друг с другом.

---

## Миграция 008 — `test_completion` и `task_completion`

Прохождение тестов пользователями.

```sql
CREATE TABLE test_completion (
    id          uuid              PRIMARY KEY,
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

CREATE UNIQUE INDEX ux_test_completion_attempt
    ON test_completion (user_id, test_id, attempt_no);

CREATE INDEX ix_test_completion_user
    ON test_completion (user_id, started_at DESC);

CREATE INDEX ix_test_completion_test
    ON test_completion (test_id);
```

`tasks_total` фиксируется в момент старта попытки. Если админ потом добавит вопрос
в тест, уже идущие попытки не поедут.

`tasks_done` денормализован намеренно: прогресс показывается часто,
считать его агрегатом по `task_completion` на каждый запрос дорого.

Ограничить пользователя одной попыткой можно заменой уникального индекса на
`(user_id, test_id)` — тогда колонка `attempt_no` становится не нужна.

```sql
CREATE TABLE task_completion (
    id                 uuid        PRIMARY KEY,
    test_completion_id uuid        NOT NULL,
    task_id            uuid        NOT NULL,
    answer             jsonb       NOT NULL,
    is_correct         boolean     NOT NULL,
    score_ratio        numeric(5,4) NOT NULL DEFAULT 0,
    points_awarded     integer     NOT NULL DEFAULT 0,
    answered_at        timestamptz NOT NULL DEFAULT now(),

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

CREATE UNIQUE INDEX ux_task_completion_task
    ON task_completion (test_completion_id, task_id);

CREATE INDEX ix_task_completion_task
    ON task_completion (task_id);
```

Ответ привязан к попытке, а не к пользователю напрямую: история разных попыток
не смешивается, `user_id` не дублируется.

Уникальность `(test_completion_id, task_id)` — один ответ на вопрос в рамках попытки.
Изменение ответа до сдачи — это `UPDATE`, а не вставка новой строки.

`score_ratio` хранит долю верного (`0.6667` для двух правильных из трёх) и нужен
только при `scoring_mode = 'partial'`, чтобы показывать разбор без пересчёта.
При `all_or_nothing` дублирует `is_correct` и принимает значения 0 либо 1.

---

## Миграция 009 — `tasks_ai_help`

Диалог с ИИ-помощником по конкретному вопросу.

```sql
CREATE TABLE tasks_ai_help (
    id                 uuid         PRIMARY KEY,
    task_completion_id uuid         NOT NULL,
    user_prompt        text         NOT NULL,
    ai_response        text         NOT NULL,
    model              varchar(100) NOT NULL,
    tokens_used        integer      NOT NULL DEFAULT 0,
    created_at         timestamptz  NOT NULL DEFAULT now(),

    CONSTRAINT fk_tasks_ai_help_task_completion
        FOREIGN KEY (task_completion_id) REFERENCES task_completion (id) ON DELETE CASCADE,

    CONSTRAINT ck_tasks_ai_help_tokens
        CHECK (tokens_used >= 0)
);

CREATE INDEX ix_tasks_ai_help_task_completion
    ON tasks_ai_help (task_completion_id, created_at);
```

Привязка к `task_completion`, а не к `tasks`: подсказка относится к конкретному
ответу конкретного пользователя. Несколько строк на одну запись образуют переписку,
порядок задаёт `created_at`.

`tokens_used` и `model` нужны для учёта расхода на ИИ в разрезе пользователей.

---

## Порядок применения

| № | Миграция | Зависит от |
|---|---|---|
| 001 | ENUM-типы | — |
| 002 | `users` | 001 |
| 003 | `themes` | — |
| 004 | `pdf_to_test` | 001, 002 |
| 005 | `ai_draft_test`, `ai_draft_tasks` | 001, 002, 003, 004 |
| 006 | `tests`, `test_themes`, `tasks` | 001, 003 |
| 007 | `ai_draft_test.test_id` | 005, 006 |
| 008 | `test_completion`, `task_completion` | 001, 002, 006 |
| 009 | `tasks_ai_help` | 008 |

---

## Замечания по реализации на EF Core

### Автоматический `updated_at`

Интерфейс-маркер и перехватчик в `SaveChangesAsync`, чтобы не расставлять
присваивание руками в каждом сервисе:

```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

public override Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var now = DateTime.UtcNow;

    foreach (var entry in ChangeTracker.Entries<IAuditable>())
    {
        if (entry.State == EntityState.Added)
        {
            entry.Entity.CreatedAt = now;
            entry.Entity.UpdatedAt = now;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Entity.UpdatedAt = now;
        }
    }

    return base.SaveChangesAsync(ct);
}
```

`IAuditable` реализуют: `users`, `themes`, `ai_draft_test`, `ai_draft_tasks`,
`tests`, `tasks`. Остальные таблицы — журналы событий, у них свои осмысленные
метки времени вместо `updated_at`.

### Маппинг JSONB

```csharp
builder.Property(t => t.Options)
    .HasColumnType("jsonb")
    .HasColumnName("options");

builder.Property(t => t.CorrectAnswer)
    .HasColumnType("jsonb")
    .HasColumnName("correct_answer");
```

Для `Options` завести тип `List<TaskOption>` с полями `Id` и `Text`,
для `CorrectAnswer` и `Answer` — `List<string>`. Настроить
`JsonNamingPolicy.SnakeCaseLower`, чтобы имена свойств в JSON совпадали
с описанным выше форматом.

Массивы `correct_answer` и `answer` сравнивать по множествам, а не по порядку:

```csharp
var isCorrect = correctAnswer.ToHashSet().SetEquals(answer);
```

### Транзакция при аппруве черновика

Аппрув затрагивает несколько таблиц и должен быть атомарным: создать `tests`,
создать все `tasks`, заполнить `ai_draft_test.test_id`, выставить
`review_status = 'approved'` и `reviewed_at`. Констрейнт
`ck_ai_draft_test_approved_has_test` не даст сохранить статус `approved` без `test_id`,
поэтому обновление черновика выполняется после вставки теста, в одной транзакции.

### Проверка ответов

Правильные ответы лежат в `tasks.correct_answer` и никогда не должны попадать
в ответ API при выдаче вопросов пользователю. Отдельная DTO для прохождения
теста без этого поля, либо проекция в `Select` — не отдавать сущность целиком.

### Значения по умолчанию для генерации

При создании черновика ИИ не возвращает `order_index` — его проставляет приложение
по позиции вопроса в ответе модели. `points` при отсутствии в ответе модели — 1.
`scoring_mode` при копировании в `tasks` — `all_or_nothing`, если не задано иначе.
