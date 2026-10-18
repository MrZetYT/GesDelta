# 04. Хранилище данных

**СУБД:** PostgreSQL (D-006)
**Доступ:** EF Core (Npgsql), миграции EF Migrations (D-017)

Соглашения: время `timestamptz` (UTC), id `bigint identity`, перечисления хранятся строками, строка подключения только в конфиге. Исключение: claims.id берётся из sequence claims_id_seq, а не identity. Код резервирует id заранее (D-037). Значение задаёт код (ValueGeneratedNever), sequence создаётся миграцией.

## 1. Таблицы

### topics

| Поле | Тип | Описание |
|---|---|---|
| id | bigint PK | |
| name | text | Предметная область |
| check_interval | interval | Периодичность мониторинга |
| enabled | bool | Мониторинг включён |
| created_at | timestamptz | |

### sources

| Поле     | Тип       | Описание                                                                                   |
| -------- | --------- | ------------------------------------------------------------------------------------------ |
| id       | bigint PK |                                                                                            |
| topic_id | bigint FK |                                                                                            |
| name     | text      |                                                                                            |
| type     | text      | Rss / Arxiv / Html                                                                         |
| url      | text      |                                                                                            |
| config   | jsonb     | Параметры конкретного источника (селекторы, запрос). Rss: fullText; Arxiv: query, maxItems |
| enabled  | bool      |                                                                                            |

### materials

| Поле          | Тип              | Описание                                                        |
| ------------- | ---------------- | --------------------------------------------------------------- |
| id            | bigint PK        |                                                                 |
| source_id     | bigint FK        |                                                                 |
| external_id   | text             | guid (RSS), id без версии (arXiv), канонический URL (остальные) |
| title         | text             |                                                                 |
| url           | text             |                                                                 |
| published_at  | timestamptz null | Дата публикации по данным источника                             |
| first_seen_at | timestamptz      |                                                                 |
| last_seen_at  | timestamptz      | Когда последний раз был в выдаче                                |

Уникальность: `(source_id, external_id)`.

### snapshots

> Версия материала во времени. Создаётся только при изменении `content_hash`.

| Поле | Тип | Описание |
|---|---|---|
| id | bigint PK | |
| material_id | bigint FK | |
| run_id | bigint FK | Запуск, который получил снимок |
| fetched_at | timestamptz | |
| raw_text | text | До очистки |
| clean_text | text | После нормализации (см. 06) |
| content_hash | text | SHA256 от clean_text |

### changes

> Значимое изменение (Новое / Обновлено). Шум сюда не попадает.

| Поле             | Тип              | Описание                                                                                                                       |
| ---------------- | ---------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| id               | bigint PK        |                                                                                                                                |
| run_id           | bigint FK        |                                                                                                                                |
| material_id      | bigint FK        |                                                                                                                                |
| kind             | text             | New / Updated                                                                                                                  |
| prev_snapshot_id | bigint FK null   | null для New. Для Updated: базовый снимок (новый снимок самого свежего Change материала), не обязательно предыдущий по времени |
| new_snapshot_id  | bigint FK        |                                                                                                                                |
| summary          | text null        | Заполняется Analyzer-ом, null пока Pending                                                                                     |
| status           | text             | Pending / Done / Failed / Skipped (D-028)                                                                                      |
| error            | text null        | Причина для Failed                                                                                                             |
| done_at          | timestamptz null | Когда Change стал Done (финальная транзакция Run). null, пока Pending / Failed / Skipped                                       |
| created_at       | timestamptz      |                                                                                                                                |

### claims

| Поле                 | Тип                                                                     | Описание    |
| -------------------- | ----------------------------------------------------------------------- | ----------- |
| id                   | bigint PK, из sequence claims_id_seq (резервируется кодом, не identity) |             |
| topic_id             | bigint FK                                                               |             |
| text                 | text                                                                    | Утверждение |
| created_by_change_id | bigint FK                                                               |             |
| created_at           | timestamptz                                                             |             |
Раздел Claim в БД не хранится, определяется по маркеру в текущей версии описания

### claim_evidence

> Подтверждения утверждения. «N источников» = число разных материалов среди evidence.

| Поле           | Тип       | Описание                                                                   |
| -------------- | --------- | -------------------------------------------------------------------------- |
| id             | bigint PK |                                                                            |
| claim_id       | bigint FK |                                                                            |
| snapshot_id    | bigint FK |                                                                            |
| quote_original | text      | Дословная цитата, проверяется кодом. Цитата, найденная в clean_text снимка |
| quote_ru       | text null | Перевод для показа                                                         |

### documents

| Поле | Тип | Описание |
|---|---|---|
| id | bigint PK | |
| topic_id | bigint FK | |
| kind | text | Description (Changelog это представление, не строка) |

Строка `kind = Description` создаётся в `StartMonitoring` вместе с `topics` и `sources`, до первого Run. Файла версии на этот момент нет (09, раздел 1).

### document_versions

> Каждая версия это неизменяемый файл. Текущая версия = максимальный `version_no`.

| Поле        | Тип         | Описание                                                                                                |
| ----------- | ----------- | ------------------------------------------------------------------------------------------------------- |
| id          | bigint PK   |                                                                                                         |
| document_id | bigint FK   |                                                                                                         |
| version_no  | int         |                                                                                                         |
| file_path   | text        | Например `documents/{topic}/description/v0003.md`                                                       |
| file_hash   | text        | Для обнаружения ручных правок/потери файла. Сверяется оркестратором в начале каждого Run (07, раздел 4) |
| run_id      | bigint FK   | Запуск, создавший версию                                                                                |
| created_at  | timestamptz |                                                                                                         |

Утверждения в описании ссылаются на Claim маркером с id в тексте `(формат` [c:id]`, 09)`. Код извлекает маркеры при сохранении версии и проверяет, что каждый Claim существует.

### runs

| Поле        | Тип              | Описание                                                                                                                                                                                                 |
| ----------- | ---------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| id          | bigint PK        |                                                                                                                                                                                                          |
| topic_id    | bigint FK        |                                                                                                                                                                                                          |
| trigger     | text             | Scheduled / Manual / CatchUp                                                                                                                                                                             |
| status      | text             | Pending / Running / Succeeded / NoChanges / Failed                                                                                                                                                       |
| stats       | jsonb            | Счётчики: checked, new, updated, noise, failed, analysisFailed, analyzerCalls, skipped, confirmOnly, overBudget, pending, llmFallback, llmModelUsed, fallbackReason, promptVersion, descriptionOverLimit |
| started_at  | timestamptz      |                                                                                                                                                                                                          |
| finished_at | timestamptz null |                                                                                                                                                                                                          |
| tokens_used | int              |                                                                                                                                                                                                          |
| error       | text null        | Причина для Failed: timeout, interrupted и т.д.                                                                                                                                                          |

### concepts, reports

Вне MVP (D-014). Отчёт заменяют Change, сгруппированные по Run, и версии описания.

## 2. Связи и индексы

Внешние ключи: как в ER-диаграмме 01_DOMAIN плюс `snapshots.run_id`, `document_versions.run_id`, `claim_evidence → claims, snapshots`.

| Ограничение / индекс                                   | Зачем                                               |
| ------------------------------------------------------ | --------------------------------------------------- |
| UNIQUE `materials(source_id, external_id)`             | Идентичность материала                              |
| INDEX `snapshots(material_id, fetched_at DESC)`        | Последний снимок материала                          |
| INDEX `changes(run_id)`, `changes(created_at)`         | Журнал по запуску и по датам                        |
| UNIQUE `document_versions(document_id, version_no)`    | Нумерация версий                                    |
| UNIQUE `runs(topic_id) WHERE status = 'Running'`       | Защита от двух параллельных запусков                |
| INDEX `runs(topic_id, started_at DESC)`                | Последний запуск, догоняющий запуск                 |
| INDEX `claim_evidence(claim_id)`                       | Подсчёт источников                                  |
| INDEX changes(done_at)                                 | Журнал и вопросы «с прошлого раза» по дате фиксации |
| INDEX `changes(material_id, id DESC)`                  | Базовый снимок материала для diff (D-056)           |
| `GIN to_tsvector('simple', text) на claims` (Explain)  | Поиск для вопросов «что такое Z» (Explain)          |
| GIN `to_tsvector('simple', clean_text)` на `snapshots` | Поиск «когда появилось понятие»                     |
| INDEX changes(status) WHERE status = 'Pending'         | Добор незавершённых при старте запуска              |

Частичный уникальный индекс и GIN в EF создаются сырым SQL в миграции.

## 3. Дедупликация

- **Материалы:** по `(source_id, external_id)`. Один и тот же сюжет из разных источников = разные Material, не склеиваем (D-018).
- **Снимки:** новый Snapshot только если `content_hash` отличается от последнего снимка материала.
- **Утверждения:** подтверждение между источниками считается на уровне Claim (несколько evidence). Алгоритм сопоставления «то же утверждение» TODO (06 или 03).

## 4. Ключевые запросы

Во всех запросах только `status = 'Done'`: Pending и Failed пользователю не показываем. Период считается по `done_at` (когда изменение попало в описание), а не по `created_at` (D-036).

Поиск по понятиям: код превращает `terms` в строку вида `"a" or "b" or "c"` (`@tsQuery`) и список шаблонов `%a%`, `%b%` (`@patterns`, символы `%` и `_` экранируются). Блок «фильтр по terms» код добавляет только если `terms` непустые.

**Что изменилось за период по теме X** (`WhatChanged`)

```sql
SELECT c.done_at, c.kind, c.summary, m.title, m.url,
       COUNT(*) OVER() AS total
FROM changes c
JOIN runs r ON r.id = c.run_id
JOIN materials m ON m.id = c.material_id
JOIN snapshots s ON s.id = c.new_snapshot_id
WHERE r.topic_id = @topicId
  AND c.status = 'Done'
  AND c.done_at >= @from AND c.done_at < @toExclusive
  -- фильтр по terms (только если terms непустые):
  AND (c.summary ILIKE ANY(@patterns)
       OR m.title ILIKE ANY(@patterns)
       OR to_tsvector('simple', s.clean_text) @@ websearch_to_tsquery('simple', @tsQuery))
ORDER BY c.done_at DESC
LIMIT @limit;
```

`total` нужен для строки «и ещё M». `@limit` = 10 (конфиг QA).

**Какие изменения с прошлого раза** (`sinceLastRun`, кнопка «Что изменилось с прошлого запуска»)

```sql
SELECT c.done_at, c.kind, c.summary, m.title, m.url
FROM changes c
JOIN runs r ON r.id = c.run_id
JOIN materials m ON m.id = c.material_id
WHERE r.topic_id = @topicId
  AND c.status = 'Done'
  AND c.done_at = (
      SELECT max(c2.done_at)
      FROM changes c2
      JOIN runs r2 ON r2.id = c2.run_id
      WHERE r2.topic_id = @topicId AND c2.status = 'Done'
  )
ORDER BY c.id;
```

`LIMIT` не нужен: выбирается одна группа. N и «и ещё M» считает код по числу строк.

Все Change одной фиксации получают одинаковый `done_at`: в Postgres `now()` внутри транзакции одно и то же значение. Фильтр по terms из предыдущего запроса добавляется так же (с join на `snapshots`).

**Когда впервые появилось понятие Y** (`WhenFirst`)

```sql
SELECT m.title, m.url, s.fetched_at, m.published_at
FROM snapshots s
JOIN materials m ON m.id = s.material_id
JOIN sources src ON src.id = m.source_id
WHERE src.topic_id = @topicId
  AND to_tsvector('simple', s.clean_text) @@ websearch_to_tsquery('simple', @tsQuery)
ORDER BY s.run_id, COALESCE(m.published_at, s.fetched_at), s.id
LIMIT 1;
```

Сортировка по `run_id` и дате публикации, а не по `fetched_at`: в первом запуске все снимки получены почти одновременно, и порядок по `fetched_at` был бы случайным.

Оговорка: это «когда впервые встретилось в наших данных», а не в мире. Ответ идёт с датой публикации материала, если она есть.

**Утверждения и подтверждения для вопроса «Что такое Z»** (`Explain`)

```sql
WITH hit AS (
    SELECT cl.id, cl.text
    FROM claims cl
    WHERE cl.topic_id = @topicId
      AND (to_tsvector('simple', cl.text) @@ websearch_to_tsquery('simple', @tsQuery)
           OR cl.id = ANY(@claimIdsFromSections))
    ORDER BY cl.id
    LIMIT 5
)
SELECT hit.id, hit.text, ce.quote_ru, m.id AS material_id,
       m.title, m.url, m.published_at, s.fetched_at
FROM hit
JOIN claim_evidence ce ON ce.claim_id = hit.id
JOIN snapshots s ON s.id = ce.snapshot_id
JOIN materials m ON m.id = s.material_id
ORDER BY hit.id, s.fetched_at;
```

`@claimIdsFromSections`: id из маркеров `[c:id]` в разделах текущей версии Description, где нашлись `terms`. Разделы код читает из файла текущей версии (Markdig), в БД их нет. Лимит 5 стоит на утверждениях, а не на строках: иначе у утверждения с тремя подтверждениями «съелись» бы чужие.

**Последний запуск темы** (строка «Последняя проверка» в QA и «последняя проверка» на экране статуса)

```sql
SELECT status, started_at, finished_at
FROM runs
WHERE topic_id = @topicId AND status IN ('Succeeded', 'NoChanges', 'Failed')
ORDER BY started_at DESC
LIMIT 1;
```

Строка выводится, если `status = 'NoChanges'` и `finished_at` позже максимального `done_at`.

**Журнал изменений** (группировка по `done_at` в коде)

```sql
WITH page AS (
    SELECT DISTINCT c.done_at
    FROM changes c
    JOIN runs r ON r.id = c.run_id
    WHERE r.topic_id = @topicId
      AND c.status = 'Done'
      AND (@before IS NULL OR c.done_at < @before)
    ORDER BY c.done_at DESC
    LIMIT @groups
)
SELECT c.done_at, c.kind, c.summary, m.title, m.url
FROM changes c
JOIN runs r ON r.id = c.run_id
JOIN materials m ON m.id = c.material_id
JOIN page p ON p.done_at = c.done_at
WHERE r.topic_id = @topicId AND c.status = 'Done'
ORDER BY c.done_at DESC, c.id;
```

Страница = `@groups` последних групп целиком (по умолчанию 20). Группа не обрывается посередине.

## 5. Хранение и очистка

- `raw_text` и `clean_text` храним без ограничений, снимки создаются только при изменениях.
- Очистка и архивация вне MVP. Пересмотреть, если БД вырастет заметно (оценка: ~1 МБ в неделю на тему из 10 материалов).
- Файлы версий не удаляем.
- Порядок записи версии: сначала файл на диск, затем строка в БД. Осиротевший файл безвреден, строка без файла нет. Если файл vNNNN.md есть, а строки в БД нет, при следующем Run он перезаписывается (07).

## 6. Миграции

- EF Core Migrations, миграции в репозитории.
- Применяются при старте приложения (`Database.Migrate()`).
- Правила: миграции не редактируем после применения, любое изменение схемы это новая миграция.