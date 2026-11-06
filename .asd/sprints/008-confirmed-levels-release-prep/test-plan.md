---
responsibility:
  owns: test approach for sprint change scope, removal reasons, no-test decisions, suite run result, code defects found by tests, manual-verification spec (single home — never duplicated in a review file)
  excludes: task breakdown, requirements, review verdicts, code, change surface (derivable from the diff)
  delegates_to: plan.md (tasks), persistent docs (requirements), reviews/impl/wave-<K>/iter-NN/quality.md (verdict)
---

# Test plan — sprint 008-confirmed-levels-release-prep

## Entry log

Appended each entry, never rewritten. `HEAD analysed` is the commit the strategy/prune passes
were scoped through; the next re-entry's delta is `git diff <this sha>...HEAD`.

| Entry | HEAD analysed | Scope |
|---|---|---|
| 1 | 63cc799f | full change surface (`git diff main...HEAD`, без `.asd/sprints`) |

## Risk → check decisions

Перед стратегией прогнан весь backend (951 тест, 0 сбоев) и весь web (855 тестов, 0 сбоев): `TestEnvironment` общий для всех интеграционных тестов, поэтому влияемый набор = весь набор. Первый живой прогон под ролью `kalibr` без `SUPERUSER` и под новой `InitialCreate` зелёный.

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `ReviewReader.PreviousAsync`, `ReviewConfirmedLevelResponse`: подтверждённый уровень в `GET`/`PUT .../review` (AC-1, AC-2, AC-4, AC-5, AC-6, AC-7) | неверный выбор подтверждения: не последнее по дате, считаются только «Соответствует», учитывается рассматриваемая заявка, видна неопубликованная сессия; шкала не той версии; подтверждение двигает рейтинг; видят не те | интеграционный на реальном PostgreSQL через тестовый хост | add | `PromotionConfirmedLevelTests`: 3 сессии одного кандидата (Meets, затем DoesNotMeet, затем рассматриваемая) — последнее по дате решение, любое решение, `GET` и `PUT`, своя заявка до и после публикации, неопубликованная сессия скрывает и откатывается к предыдущему, повторная публикация возвращает; сравнение рейтинга, готовности и начальных баллов с кандидатом без истории; администратор видит, кандидат, оценивающий и посторонний нет, кандидату и ранжированию поле не отдаётся. Вторая проверка — две сессии на разных версиях матрицы: шкала подтверждения остаётся шкалой своей версии при другой шкале у компетенции, замещённая компетенция не показывается |
| Откат сессии (AC-5) | заявка, возвращённая откатом из «Решение вынесено», даёт подтверждение | интеграционный | none | Реальный откат невозможен вместе со второй сессией того же подразделения: откат закрыт для завершённой сессии, а следующая объявляется только после неё. Проверено эквивалентом, который читает код: этап сессии сдвинут датами до обратной связи (`PutInStageAsync(5)`) — подтверждение скрыто, обратно в 7 — вернулось. Условие `Status == Decided` общее с историей: заявку не в этом статусе `PromotionHistoryTests.Ac84` уже исключает (FormalRejected) |
| `ChairReviewView`, `MemberReviewView`, `ConfirmedLevelText` (AC-3) | строка не рисуется, рисуется для всех строк, максимум шкалы взят не из своей шкалы | компонентный (vitest, testing-library) | add | `ReviewParts.test.tsx`: формат строки, максимум шкалы подтверждения, `null` — пусто. Обе формы проверены через демо-сценарий ниже: ровно одна строка у компетенции с подтверждением, остальные строки как раньше |
| `demo/scenario.tsx` (AC-8) | демо не показывает подтверждённый уровень | компонентный | add | `DemoPage.test.tsx`: на шагах «Карточка кандидата» (форма члена) и «Итоговые оценки и решение» (форма председателя) ровно одна строка «Подтверждено: 3 из 4, … · Разработчик». Отдельные фикстуры `Review` не заводятся: сценарий и есть фикстура |
| `tours.ts` фраза шага `review-card`, `CHANGELOG.md`, `Directory.Build.props`, `web/package.json`, правило «Release compatibility», `backward_compat` (AC-12, AC-13, AC-18, AC-19) | неверный текст/значение | статическая (сборка, `check-api`, lint) | none | Текст и константы без поведения; сборка и lint зелёные, версия единая для всех проектов видна из сборки |
| `openapi.json`, `schema.d.ts` (AC-11) | рассинхрон контракта | статическая | none | `npm run check:api` чист, `ef-pending-changes` чист (шаг 8) |
| `InitialCreate` (AC-9) | схема или начальные данные чистой базы расходятся со старой линией; потеряны объекты вне модели | разовое сравнение на реальных базах (результат ниже), постоянные проверки уже есть | none | Сравнение со старой линией нельзя повторять: линии нет в репозитории. Постоянные проверки остаются: `BaseScaleSeedTests` (шкала и её отсутствие в журнале), `PlatformTests.Ac15` (триггеры `ENABLE ALWAYS`), 953 теста идут на схеме `InitialCreate`, `ef-pending-changes` |
| `TestEnvironment`: роль `kalibr` без `SUPERUSER`, `AdminConnectionString` (AC-16) | тесты проходят только потому, что приложение суперпользователь | сам набор | keep | Весь набор идёт под ролью `kalibr`; единственный тест, которому нужен суперпользователь (`session_replication_role`), переведён на административное подключение. Отдельный тест не нужен: роль создаётся явно с `NOSUPERUSER`, а любой тест, выполняющий DDL или `session_replication_role`, упал бы при нехватке прав |
| `db-init.sh`, `10-app-role.sh`, `deploy/**` (AC-15, AC-17) | роль создаётся с лишними правами или не владеет базой; `InitialCreate` не применяется под ней | разовый прогон на реальном образе (результат ниже) | none | Скрипты исполняются только при инициализации тома образом PostgreSQL; автотест потребовал бы запуска образа с init-скриптами — новой инфраструктуры сверх `Testcontainers` набора |
| `deploy/README.md` раздел 5 (AC-14) | команды обновления и отката не работают | разовый прогон на реальном образе (результат ниже); `DeploymentTests.Ac6` остаётся | none | Команды выполняются на живом кластере; в наборе проверяется только наличие упоминаний в руководстве |

### Разовые проверки (результаты)

**A. AC-9, схема.** Старая линия — `c95047a0`, новая — `HEAD`; оба состояния выгружены `git archive` в каталог вне репозитория, миграции применены `dotnet ef database update` (настоящий путь EF; `migrations script` старой линии не годится: сырые `Sql()` без `;`) к двум базам одного `postgres:18.6-trixie` под `postgres`. Затем `pg_dump --schema-only --no-owner --no-privileges` обеих, порядок колонок внутри `CREATE TABLE` нормализован сортировкой.

- Расхождение одно, шесть колонок: в старой линии `DEFAULT false` (`promotion_applications.artifacts_missing`, `is_sent_to_review`, `review_letter_pending`, `was_held_at_close`; `promotion_waves.no_chair_letter_sent`, `versions_fixed`), в новой нет. Вердикт: допустимо и ожидаемо — следы `AddColumn(defaultValue: false)` старых миграций; модель EF default не задаёт (иначе `ef-pending-changes` не был бы чист), приложение всегда передаёт значение. Прямых SQL-вставок без этих колонок в `src`/`tests` нет. Решение оркестратора: AC-9 требует совпадения схемы без исключений, отказ от него был бы сменой скоупа; расхождение устраняется в `InitialCreate`, дефект D-1.
- Только порядок колонок отличается у `employees.education`, `search_vector` (2 таблицы), ряд колонок `promotion_applications` и `promotion_waves`, `email_confirmed_*`: допустимо, колонки добавлены поздно.
- Ограничения, индексы, внешние ключи, последовательности, коллации (`ru-x-icu`), сгенерированные колонки совпадают построчно (иных расхождений в дампе нет).
- Явно равны в обеих базах: триггеры `audit_events_reject_update_delete` и `audit_events_reject_truncate` с `tgenabled = 'A'`; `IX_positions_name_lower`, `IX_legal_entities_name_lower` (`CREATE UNIQUE INDEX … lower((name)::text)`); тело `audit_events_reject_change` (md5 `600736ab…`); расширение `pg_trgm` 1.6.
- Начальные данные (`pg_dump --data-only --column-inserts`, без `__EFMigrationsHistory`): строки есть только в `roles` (2), `promotion_settings` (1, `stage_durations_days = {10,2,5,2,5,5}`), `competency_scales` (1, базовая шкала с тем же Guid); совпадают, различается только `created_at` строки шкалы (момент применения). Остальные таблицы пусты в обеих.
- `__EFMigrationsHistory`: 15 строк против одной (`20261105155028_InitialCreate`), исключено по заданию.

**B. AC-15, AC-17, роль БД.** Два временных контейнера `postgres:18.6-trixie`, пароль `KALIBR_DB_PASSWORD=pa ss'w$rd` (пробел, одинарная кавычка, `$`), через `--env-file`:

- `deploy/dev/db-init.sh`, смонтирован как в compose (в контейнере режим `rwx`, исполняется).
- Текст `10-app-role.sh` из `deploy/k8s/configmap.yaml`, скопирован без права исполнения (`-rw-r--r--`), контейнер от `999:999`, как в `db.yaml`; журнал: `docker-entrypoint.sh: sourcing /docker-entrypoint-initdb.d/10-app-role.sh`.

В обоих: `kalibr` — `rolsuper=f`, `rolcreatedb=f`, `rolcreaterole=f`, `rolcanlogin=t`; `pg_database.datdba` базы `kalibr` = `kalibr`; вход `kalibr` по TCP на адрес контейнера (не loopback) с этим паролем проходит, с неверным — `password authentication failed`; `dotnet ef database update` ролью `kalibr` с хоста по проброшенному порту применил `20261105155028_InitialCreate`, `pg_trgm` создан, владелец расширения `kalibr`, оба триггера `A`. Дефектов нет.

**C. AC-14, обновление и откат.** Контейнер из B (k8s-вариант) с мигрированной базой; под `kalibr` вставлены 2 `org_units` (кириллица), 2 `audit_events`, 1 `competency_scales`. Команды раздела 5 с заменой `kubectl exec statefulset/db` на `docker exec <контейнер>` (переменные `$POSTGRES_USER`, `$POSTGRES_DB` раскрывает `sh -c` внутри контейнера):

1. `pg_dump --format=custom` в файл через перенаправление stdout — exit 0, 112 782 байта, заголовок `PGDMP`, `pg_restore -l` читает.
2. `dropdb --force` и `createdb --owner kalibr` — exit 0.
3. `docker exec -i … pg_restore --exit-on-error < dump` — exit 0.

Отпечаток до и после (`rows`, md5 строк `org_units`, `audit_events`, `competency_scales`, владелец базы, число объектов `public` не у `kalibr` = 0, триггеры `A`, md5 тела функции, расширения, история миграций) равен. Расхождение одно: md5 `pg_indexes.indexdef` — `pg_dump`/`pg_restore` переписывает `= ANY (ARRAY[…])` в `check` и двух частичных индексах (`ux_matrix_versions_in_work`, `ux_promotion_applications_unfinished`) в эквивалентную запись `ANY ((ARRAY[…])::text[])`; семантика та же, дефектом README не считается. После восстановления: `kalibr` (не суперпользователь) вставляет строки и выполняет DDL; `UPDATE`, `DELETE`, `TRUNCATE` журнала отклонены триггером; `DELETE` суперпользователя при `session_replication_role = replica` тоже. Дефектов README нет.

**D. Утечка термина (шаг 4).** Поиск по `.claude/agent-memory/**` (с `--hidden`) точных удалённых формулировок: «No backward compatibility before 1.0», «Until release 1.0», «squash/squashed … right before release», `backward_compat: none`, 14 имён старых миграций и их временных меток. Реальное попадание одно: `asd-architect/feedback_contract-removal-classification.md` (строки 3, 8, 15) и его индексная строка в `asd-architect/MEMORY.md` (строка 7). Файл `asd-dev-critical/feedback_commit-subject-length.md` — «pre-push squash» это git-squash, а не удалённая схема, не попадание. Вне памяти агентов остались описания старой линии в `docs/architecture/{stack.html,audit.md,career-framework.md,org-structure.md}` (`20261021091246_InitialCreate`, «при схлопывании перед релизом»): ждут действия оркестратора из `plan.md` «Вне Task-блоков».

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| `tests/Kalibr.Tests/IdentityRolesMigrationTests.cs`: `Ac63_RolesAndHrAssignments_PassToIdentityRolesAndEmployees_WhenTheDatabaseMovesToTheNextMigration` | проверял перенос данных между двумя миграциями старой линии, которой нет; класс сам требовал удаления вместе со схлопыванием; удалён Task 7 (`b4fd325e`), здесь только подтверждён | yes |

Других удалений нет: остальные тесты поверхности (`PlatformTests.Ac15`, `DeploymentTests`, `BaseScaleSeedTests`) проверяют живое поведение или контракт.

## Added tests

Level and AC/risk covered are visible in the test file itself (name, path) — not restated here.

Серверные прогоны: `dotnet test --solution Kalibr.slnx --filter-class "*PromotionConfirmedLevelTests"` в копии `HEAD` вне репозитория (`git archive`), мутация в `src/Kalibr.Promotion/ReviewReader.cs`; рабочее дерево репозитория не менялось. Контроль той же копии без мутации: exit 0, 2 пройдено. Web: `vitest run src/features/promotion/ReviewParts.test.tsx src/features/promotion/demo/DemoPage.test.tsx -t "confirmed level"` в копии `web/` вне репозитория, мутация в файле компонента; контроль exit 0, 5 пройдено.

| Test | Regression proof |
|---|---|
| `tests/Kalibr.Tests/PromotionConfirmedLevelTests.cs`: `Ac1_Ac2_Ac5_Ac6_Ac7_TheCard_ConfirmsTheLatestPublishedDecisionOfAnotherApplication_…` | mutation сортировка по возрастанию `DecidedAt`: тот же вызов → exit 2, этот тест (`to be 2 … found 4`); mutation только `Meets` в подтверждении: exit 2, этот тест (`found 4`); mutation неопубликованные сессии считаются: exit 2, этот тест (`hidden … to be 4 … found 2`); mutation рассматриваемая заявка входит в выборку: exit 2, этот тест (`published … to be 2 … found 3`); mutation подтверждение подменяет начальные баллы формы: exit 2, этот тест (`Informational(card)` различается); runs: 5. Проверки доступа (посторонний, оценивающий, кандидат, ранжирование) мутацией не доказаны: они подтверждают неизменные охранные условия и отсутствие поля в чужих ответах, нового поведения под ними нет |
| `tests/Kalibr.Tests/PromotionConfirmedLevelTests.cs`: `Ac4_TheConfirmation_FollowsACompetencyAcrossMatrixVersions_…` | mutation шкала подтверждения берётся у компетенции текущей версии: тот же вызов → exit 2, этот тест (`scalePoints … to contain 5 item(s) … found 3`); runs: 1 |
| `web/src/features/promotion/ReviewParts.test.tsx`: `is one line with the score out of the top of its scale, …` | mutation `scalePoints.at(0)` вместо `at(-1)`: vitest → exit 1, этот тест и `takes the top of the scale …` (`Unable to find an element with the text`); runs: 1 |
| `web/src/features/promotion/ReviewParts.test.tsx`: `takes the top of the scale of the application it was confirmed in` | та же мутация, тот же прогон: exit 1, этот тест; runs: 1 |
| `web/src/features/promotion/ReviewParts.test.tsx`: `draws nothing when the competency has no confirmation` | mutation строка «Подтверждено: нет» при `null`: exit 1, этот тест (контейнер не пуст); runs: 1 |
| `web/src/features/promotion/demo/DemoPage.test.tsx`: `shows the confirmed level of one competency only on the step «Карточка кандидата»` | mutation `ConfirmedLevelText` убран из `MemberReviewView`: exit 1, этот тест (`Unable to find an element with the text: /^Подтверждено:/`); mutation `confirmedLevel: null` у всех компетенций демо: exit 1, этот тест; mutation строка для `null`: exit 1, этот тест (`length 1 but got 3`); runs: 3 |
| `web/src/features/promotion/demo/DemoPage.test.tsx`: `shows the confirmed level of one competency only on the step «Итоговые оценки и решение»` | mutation `ConfirmedLevelText` убран из `ChairReviewView`: exit 1, этот тест; те же две мутации компонента и сценария: exit 1, этот тест; runs: 3 |

## Suite run

Written twice per cycle: `impl-test`'s suite gate records an **impacted-set** run here each entry
(`.asd/rules/sprint-lifecycle.md` "Impacted test set"); `impl-review`'s terminal step overwrites
it with the cycle's one **full-suite** run once the last review wave's reviewers are
APPROVE/latched. The `pr` gate always reads whatever is recorded here last — the full-suite
record, by the time `pr` runs. Each per-entry record measures only the tree that entry
analysed, not any tree produced later
(`.asd/rules/sprint-lifecycle.md` "Impacted test set").

- Command: `dotnet test --solution Kalibr.slnx` и `npm --prefix web test` (предохранитель общей инфраструктуры: `TestEnvironment`, миграции, `Directory.Build.props` — полный набор)
- Scope: full
- Result: pass — backend 953 пройдено, 0 сбоев, 0 пропущено (exit 0, 4 м 34 с); web 855 пройдено в 84 файлах, 0 сбоев (exit 0). Первый web-прогон шага 8 был красным: 6 тестов из 5 файлов вне поверхности (`linkScreens`, `catalogueTables`, `DeskPage`, `DraftsQueuePage`, `userMail`) упали по `Test timed out in 5000ms` сразу после полного backend-набора (контейнеры и сборка ещё грузили машину); тот же код при повторе — 855 из 855, 49 с, до прогона — 855 из 855, 57 с. Тесты не менялись; это зависимость от нагрузки, а не дефект поверхности спринта, оркестратору как `flagged`.
- Lint / build: pass — `dotnet build Kalibr.slnx --tl:off` 0 предупреждений 0 ошибок; `npm --prefix web run lint` (biome ci + typecheck) exit 0; `npm --prefix web run build` exit 0; `npm --prefix web run check:api` exit 0; `dotnet ef migrations has-pending-model-changes` — изменений нет
- HEAD: 98f01fe2f3591be5ddeeffa6fe76b908310472f8 — прогоны на нём; последующий коммит меняет только `test-plan.md`

## Defects

Code defects found by the suite. Resolved in `impl` test-fix mode. `Entry` through `Failing test` are never edited once written — the stalemate check compares them (`.asd/workflows/asd-phase-impl-test.md` step 9). One table only: append rows, never a second `## Defects` section — the check fails on one.

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Kalibr.Platform/Migrations/20261105155028_InitialCreate.cs | Шесть колонок без DEFAULT false, в старой линии он есть: promotion_applications.artifacts_missing, is_sent_to_review, review_letter_pending, was_held_at_close; promotion_waves.no_chair_letter_sent, versions_fixed | Разовая проверка A, сравнение схем AC-9 | pending | |

## Manual verification (optional)

Нет: все критерии проверяются автоматически.
