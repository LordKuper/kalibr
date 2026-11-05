---
responsibility:
  owns: task breakdown, task status (checkboxes), sprint-specific DoD additions
  excludes: requirements, design decisions, code, review findings, the standing DoD (owned by sprint-lifecycle.md "Plan file format")
  delegates_to: reviews/ (findings); persistent docs (requirements/design) are named in the impl dispatch payload, not linked here
---

# Plan

## Overview

Спринт lite, источник критериев — `sprint.md` AC-1…AC-20, входы — `research.md` и `audit.md`. Сначала меняется режим работы (Task 1), затем функция подтверждённых уровней (Task 2, 3), развёртывание и версия (Task 5, 6), и последней сжимается линия миграций (Task 4), когда схема уже не меняется. Тесты приводятся к новой роли БД в Task 7, новые тесты пишет impl-test.

Новый дом правила: `.asd/project/custom-common-rules.md`, раздел «Release compatibility» (Task 1). Правило читают все последующие диспетчеризации.

Внешние интерфейсы и их справки: `dotnet ef migrations add`, `has-pending-model-changes` — `docs/architecture/tech-reference/dotnet-ef-10.0.12.md`; скрипты `/docker-entrypoint-initdb.d`, доверенное расширение `pg_trgm`, владелец схемы `public` — `docs/architecture/tech-reference/postgres-image-18.6-trixie.md`.

## Definition of Done

Standing DoD applies (`sprint-lifecycle.md` "Plan file format") — not restated here.

Дополнительно: impl-test сравнивает чистые базы старой линии миграций (коммит `c95047a0`) и новой `InitialCreate`: `pg_dump --schema-only` и строки начальных данных, результат записан в `test-plan.md` (AC-9). Полный набор тестов идёт под ролью без `SUPERUSER` (AC-16).

### Task 1: Режим миграций и правило проекта
Material risk: artifact: project rule swap
Settings change: backward_compat=migration
AC: AC-12, AC-13.
- [x] В `.asd/project/custom-common-rules.md` заменить раздел «No backward compatibility before 1.0» на раздел «Release compatibility» со следующим текстом:

  ```markdown
  ## Release compatibility

  - The API keeps no backward compatibility: the SPA, its only consumer, ships in the same image. The contract version stays 1.0.0 and the path `/api/v1`; obsolete surface is deleted at once, never marked deprecated. This overrides `backward_compat: migration` for the API: a breaking API change needs no migration path, deprecation notice or version bump.
  - Data survives every release: the DB schema changes only forward, by incremental EF migrations over the baseline `InitialCreate`, which is never regenerated. A migration preserves existing data and transfers it where a change moves it or alters its meaning; the transfer is SQL in `Up` after the new structure is created and before the old one is dropped (`docs/architecture/stack.html` «Контракт из кода»).
  - An upgrade starts with a database backup; a rollback restores the backup and the previous image. Expand/contract is not used: one replica, `strategy: Recreate`. (`008-confirmed-levels-release-prep#AC-13`, replaces `004-role-desktop-user-journeys#AC-58`, `#AC-63`)
  ```
- [x] Проверить: `grep -n "before 1.0\|Until release 1.0" .asd/project/custom-common-rules.md` пуст.

### Task 2: Подтверждённый уровень в карточке кандидата
Material risk: none
AC: AC-1, AC-2, AC-4, AC-5, AC-6, AC-7.
- [ ] Добавить `src/Kalibr.Promotion/ReviewConfirmedLevelResponse.cs`: `internal sealed record ReviewConfirmedLevelResponse(int Score, IReadOnlyList<ScalePoint> ScalePoints, DateTimeOffset DecidedAt, ApplicationTargetResponse Target)` с XML-комментариями. `ScalePoints` — шкала компетенции в версии той заявки: шкала может отличаться от версии текущей заявки.
- [ ] В `ReviewCompetencyResponse` добавить параметр `ReviewConfirmedLevelResponse? ConfirmedLevel` после `Change`, с описанием в XML-комментарии.
- [ ] В `ReviewReader.PreviousAsync` к уже отобранным заявкам `shown` (`Status == Decided`, результаты опубликованы, без рассматриваемой) дочитать `PromotionFinalScore` и вернуть третьим значением словарь `CompetencyId → ReviewConfirmedLevelResponse`: обход `shown` по убыванию `DecidedAt`, `TryAdd` по компетенции, `Target` из `facts[other.Id].Target`, шкала из `facts[other.Id].Version`. Новых запросов на другие таблицы, индексов и таблиц нет.
- [ ] Передать значение через `DescribeAsync` в `CompetencyOf`. Расчёты `career.Score`, `DefaultFinalScores`, `RankAsync`, `ReviewResponse.History` не менять.
- [ ] Собрать backend: `openapi/openapi.json` обновится сам; `ef-pending-changes` чист.

### Task 3: Подтверждённый уровень на формах и в демо
Material risk: none
AC: AC-3, AC-8, AC-11.
- [ ] Выполнить `gen-api`, `web/src/api/schema.d.ts` обновлён, `check-api` чист.
- [ ] В `web/src/features/promotion/ReviewParts.tsx` добавить `ConfirmedLevelText({ level })`: вторичным текстом в одну строку «Подтверждено: 3 из 4, {дата решения} · {levelName}»; максимум шкалы — `level.scalePoints.at(-1)`, дата — существующий `formatDate`, без новых токенов. `null` не рисует ничего.
- [ ] В `ChairReviewView.tsx` и `MemberReviewView.tsx` вывести `ConfirmedLevelText` в ячейке «Компетенция» под названием и тегами; новой колонки нет, строка без подтверждения не меняется.
- [ ] В `demo/scenario.tsx` задать подтверждённый уровень хотя бы одной компетенции и `null` у остальных, в типах и строках сценария.
- [ ] В `tours.ts` шаг `review-card` дополнить одной фразой о подтверждённом уровне, по правилу надписей: только то, что нужно для действия.

### Task 4: Базовая миграция InitialCreate
Material risk: change: migration
AC: AC-9.
- [ ] Сохранить в рабочей копии тексты объектов вне модели, затем удалить все файлы `src/Kalibr.Platform/Migrations/` (15 миграций, их `.Designer.cs`, `AppDbContextModelSnapshot.cs`); удаление разрешено пользователем на гейте scope.
- [ ] Выполнить `ef-migration-add InitialCreate`: новый идентификатор с текущей датой.
- [ ] В `Up` новой миграции добавить без изменений тела объектов вне модели, с `Down`: функция `audit_events_reject_change`, триггеры `audit_events_reject_update_delete`, `audit_events_reject_truncate`, `ENABLE ALWAYS TRIGGER` для обоих, индексы `IX_positions_name_lower`, `IX_legal_entities_name_lower`, вставка шкалы по умолчанию в `competency_scales`.
- [ ] Не переносить разовые `UPDATE` и `DELETE` старых миграций: базы пустые.
- [ ] Проверить все формы удаляемых имён: `rg -n "2026102[0-9]{7}_|20261030[0-9]{6}_" src tests deploy` находит только новую миграцию и удаляемый в Task 7 `IdentityRolesMigrationTests.cs`.
- [ ] `ef-pending-changes` чист, `dotnet build Kalibr.slnx --tl:off -c Release` без предупреждений.

### Task 5: Роль БД без суперправ и инструкция развёртывания
Material risk: change: security
AC: AC-10, AC-14, AC-15, AC-17, AC-18.
- [ ] Схема учётных записей: `POSTGRES_USER=postgres` — суперпользователь, только для обслуживания; роль приложения `kalibr`: `LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE`, владелец базы `kalibr`, пароль `KALIBR_DB_PASSWORD`.
- [ ] В `deploy/k8s/configmap.yaml` сменить `POSTGRES_USER` на `postgres`, обновить комментарий и добавить ConfigMap `db-init` со скриптом `10-app-role.sh`: `psql -v ON_ERROR_STOP=1 -v app_password="$KALIBR_DB_PASSWORD" --username "$POSTGRES_USER" --dbname "$POSTGRES_DB"` с командами `CREATE ROLE kalibr … PASSWORD :'app_password'` и `ALTER DATABASE kalibr OWNER TO kalibr`.
- [ ] В `deploy/k8s/db.yaml` передать `KALIBR_DB_PASSWORD` из Secret и смонтировать `db-init` в `/docker-entrypoint-initdb.d` только на чтение.
- [ ] В `deploy/secret.template.yaml` добавить `KALIBR_DB_PASSWORD`, в `ConnectionStrings__Default` указать его, обновить комментарии.
- [ ] В `deploy/dev/docker-compose.yml` сменить `POSTGRES_USER` на `postgres`, добавить `KALIBR_DB_PASSWORD: dev_pass_123` и монтирование нового `deploy/dev/db-init.sh`; `healthcheck` оставить на `kalibr`. `launchSettings.json` не меняется: имя и пароль те же.
- [ ] В `deploy/README.md` заменить строку 92 и связанные пункты: первая установка 1.0.0, единственный переход старых баз на новую `InitialCreate` пересозданием (с прежними командами и упоминанием `__EFMigrationsHistory`), порядок обновления с резервной копией `pg_dump` перед обновлением и откат восстановлением копии с прежним образом, две учётные записи; Q5 назвать открытым. `<VERSION>` определить как версию выпуска, первая — `1.0.0`, с примером `kalibr-app:1.0.0`.
- [ ] В `deploy/dev/README.md` обновить таблицу учётных данных и описать разовое пересоздание тома `kalibr-dev-pgdata` пользователем командами для него; агенты их не выполняют.

### Task 6: Версия 1.0.0 и CHANGELOG
Material risk: none
AC: AC-18, AC-19.
- [ ] В `Directory.Build.props` добавить `<Version>1.0.0</Version>`.
- [ ] В `web/package.json` и обоих полях версии корня `web/package-lock.json` указать `1.0.0`.
- [ ] Создать `CHANGELOG.md` в корне на русском: раздел `## 1.0.0 — 2026-11-05`, что входит в первый релиз по `docs/architecture/subsystems.md`, примечание об установке с нуля.

### Task 7: Тесты под ролью без суперправ
Material risk: none
Test-only: tests/Kalibr.Tests/IdentityRolesMigrationTests.cs, tests/Kalibr.Tests/Infrastructure/TestEnvironment.cs, tests/Kalibr.Tests/PlatformTests.cs, tests/Kalibr.Tests/DeploymentTests.cs
AC: AC-9, AC-10, AC-16.
- [ ] Удалить `IdentityRolesMigrationTests.cs` вместе с удалённой миграцией, как велит комментарий этого класса.
- [ ] В `TestEnvironment` при инициализации создать роль приложения без `SUPERUSER`, `CreateDatabaseAsync` создаёт базу с этим владельцем и отдаёт строку подключения этой роли; добавить административную строку подключения к той же базе для проверок, которым нужен суперпользователь.
- [ ] В `PlatformTests` тест `Ac15_Journal_RejectsUpdateDeleteAndTruncate_EvenWhenOrdinaryTriggersAreSilencedByTheReplicaRole` перевести на административное подключение: `session_replication_role` доступен только суперпользователю.
- [ ] Привести `DeploymentTests.cs` к новому тексту `deploy/README.md`.

## Вне Task-блоков

Действие оркестратора, design-promote: `docs/architecture/stack.html` (раздел «Контракт из кода», пункт «Учётная запись БД приложения», ревизия), `docs/architecture/tech-reference/efcore-10.0.12.md`, `docs/architecture/tech-reference/postgres-image-18.6-trixie.md` (учётные записи, правила миграций), `docs/architecture/promotion.md`, `docs/product/requirements/promotion.html` (требования подтверждённого уровня), `docs/product/requirements/desk.html` AC-121.

Места, повторяющие заменяемое правило, без правок: история ревизий `stack.html`, отозванные пункты `career-framework.html` AC-149, AC-152 и `org-structure.html` AC-41 — исторические записи; `.claude/agent-memory/asd-architect/` — память агента, которого нет в составе.

Действие оркестратора, фаза pr: дата раздела `CHANGELOG.md` сверяется с днём PR перед открытием. После подтверждения мержа — аннотированный тег `v1.0.0` на merge-коммит и его отправка в `origin` (AC-20).

Действие пользователя после мержа: пересоздание dev-тома по `deploy/dev/README.md` (AC-10).

## Risks

- Docker не запущен при audit: без него нет impl-test и сравнения схем. Проверяется перед impl-test.
- Task 1 меняет настройку раньше кода: ревью этого спринта видит `backward_compat: migration` и правило «Release compatibility», которое исключает API из режима.

## Dependencies

| Wave | Tasks |
|---|---|
| 1 | Task 1 |
| 2 | Task 2 |
| 3 | Task 3, Task 5, Task 6 |
| 4 | Task 4 |
| 5 | Task 7 |

- Task 1 один в волне: несёт изменение настройки.
- Task 3 зависит от контракта Task 2.
- Task 4 идёт после всего кода: `dotnet build` и `dotnet ef` делят рабочую копию, а базовая миграция фиксирует итоговую модель.
- Task 7 покрывает код Task 4 и Task 5.

## Out of scope

- Всё из раздела Out of scope `sprint.md`.
