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
| 2 | c1325443 | delta since entry 1 (`git diff 63cc799f...HEAD`, без `.asd/sprints`): `20261105155028_InitialCreate.cs` |

## Risk → check decisions

Строки записи 1 — в `test-plan.entry-01.md` (там же результаты разовых проверок B, C, D и первый прогон A).

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `InitialCreate`: `defaultValue: false` у шести колонок (D-1, AC-9) | схема чистой базы снова расходится со старой линией; приложение не задаёт значение — `DEFAULT` в схеме наблюдаем только сравнением с базой старой линии | разовое сравнение на реальных базах (результат ниже); весь backend-набор идёт на схеме `InitialCreate` | none | Заменяет строку `InitialCreate` записи 1. Поведение не менялось: приложение всегда передаёт значение, ни один тест не вставляет строки без этих колонок. Постоянный тест на `DEFAULT` шести колонок закреплял бы след старой линии, а не требование, и повторял бы разовое сравнение. Модель EF default не задаёт, поэтому `ef-pending-changes` остаётся проверкой, что правка в миграции не разошлась с моделью |
| `ReviewReader`: выбор подтверждения по компетенции (AC-4, замечание F3 `wave-1/iter-01`) | в последней решённой заявке компетенции нет (другая версия матрицы), а ранняя её подтверждала: подтверждение берётся только из последней заявки и пропадает. Ни `Ac1`, ни `Ac4` не отличают «последнюю заявку» от «последней заявки, где компетенция была» | интеграционный (реальный PostgreSQL, тестовый хост) | add | `PromotionConfirmedLevelTests.Ac4_…_ComesFromTheLatestOneThatHadIt`: три версии матрицы, профильная компетенция в первой и третьей, во второй её нет; карточка третьей заявки показывает оценку первой, а критическая — оценку второй. Хелпер публикации версии общий с `Ac4_…FollowsACompetencyAcrossMatrixVersions…` |
| `web/src/features/promotion/ReviewParts.test.tsx`: `takes the top of the scale of the application it was confirmed in` (замечание F5 `wave-1/iter-01`) | отдельного риска нет: компонент видит только `level.scalePoints`, «из 4» уже проверяет первый тест, оба падают от одной мутации `at(0)`; выбор шкалы версии проверяет сервер, `Ac4` | компонентный (vitest) | remove | Удалить второй тест `ReviewParts.test.tsx` (`code-style.md` §17, дубликат). Review-fix-тестер тесты не удаляет (`artifact-layout.md` «Test plan»): удаляет следующая запись `impl-test` с причиной в `Removed tests`. Правка `Added tests` записи 1 о втором тесте — там же |

### Разовые проверки (результаты)

**A. AC-9, схема (повтор после D-1).** Тот же способ, что в записи 1: старая линия `c95047a0` (15 миграций), новая — `HEAD` (`20261105155028_InitialCreate`); оба состояния выгружены `git archive` в каталог вне репозитория, собраны, миграции применены `dotnet ef database update --no-build` под `postgres` к двум базам одного временного контейнера `postgres:18.6-trixie` (`kalibr-t008-e2-pg`, затем `docker rm -f -v`).

- Схема: `pg_dump --schema-only --no-owner --no-privileges` обеих баз, порядок колонок внутри `CREATE TABLE` нормализован сортировкой, случайные токены `\restrict` вырезаны. `diff` нормализованных дампов (по 2042 строки) пуст: ограничения, индексы, внешние ключи, последовательности, коллации, сгенерированные колонки, триггеры `audit_events_reject_*`, индексы `lower(name)`, тело `audit_events_reject_change` и расширение `pg_trgm` входят в этот дамп и совпали. В записи 1 то же сравнение показывало шесть расхождений `DEFAULT false`.
- Значения по умолчанию: `information_schema.columns.column_default = 'false'` у шести колонок в обеих базах — `promotion_applications.artifacts_missing`, `is_sent_to_review`, `review_letter_pending`, `was_held_at_close`; `promotion_wave_units.no_chair_letter_sent`; `promotion_waves.versions_fixed`. Колонок с `column_default` не `NULL` в каждой базе ровно шесть. Запись 1 называла таблицу `no_chair_letter_sent` ошибочно: это `promotion_wave_units`.
- Начальные данные: `pg_dump --data-only --column-inserts` без `__EFMigrationsHistory`, четыре строки (`roles` 2, `promotion_settings` 1, `competency_scales` 1) совпадают; различается только `created_at` строки шкалы (момент применения миграции, 07:24:53 против 07:24:56) и токен `\restrict`. В новой базе `__EFMigrationsHistory` — одна строка.
- Вердикт: расхождений схемы и начальных данных, кроме порядка колонок и `created_at` строки шкалы, нет; AC-9 выполнен без исключений, D-1 закрыт.

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

## Added tests

| Test | Regression proof |
|---|---|
| `tests/Kalibr.Tests/PromotionConfirmedLevelTests.cs`: `Ac4_TheConfirmation_OfACompetencyAbsentFromTheLatestApplication_ComesFromTheLatestOneThatHadIt` | mutation `.Take(1)` на цикле по `shown` (`ReviewReader.cs:254`, «подтверждение только из последней заявки») в копии вне репозитория: `dotnet test --solution Kalibr.slnx -c Release --filter-class "*PromotionConfirmedLevelTests"` → exit 2, упал этот тест (строка проверки профильной компетенции), два других зелёные; на неизменённом коде класс — exit 0, 3 пройдено; runs: 1 |

## Suite run

Written twice per cycle: `impl-test`'s suite gate records an **impacted-set** run here each entry
(`.asd/rules/sprint-lifecycle.md` "Impacted test set"); `impl-review`'s terminal step overwrites
it with the cycle's one **full-suite** run once the last review wave's reviewers are
APPROVE/latched. The `pr` gate always reads whatever is recorded here last — the full-suite
record, by the time `pr` runs. Each per-entry record measures only the tree that entry
analysed, not any tree produced later
(`.asd/rules/sprint-lifecycle.md` "Impacted test set").

- Command: `dotnet test --solution Kalibr.slnx` и `npm --prefix web test` (предохранитель общей инфраструктуры: правка миграции, которую применяет `TestEnvironment` каждого интеграционного теста — полный набор). Шаг 3 и шаг 8 — один прогон: между ними не менялись ни тесты, ни код
- Scope: full
- Result: pass — backend 953 пройдено, 0 сбоев, 0 пропущено (exit 0, 3 м 38 с); web 855 пройдено в 84 файлах, 0 сбоев (exit 0, 75 с), запущен отдельно после backend, таймаутов нет
- Lint / build: pass — `dotnet build Kalibr.slnx --tl:off` 0 предупреждений 0 ошибок; `npm --prefix web run lint` (biome ci + typecheck) exit 0; `npm --prefix web run build` exit 0; `npm --prefix web run check:api` exit 0; `dotnet ef migrations has-pending-model-changes` — изменений нет
- HEAD: 523715989993cbc0231ab462e95cc6c487c3e903 — прогоны на нём; последующий коммит меняет только `test-plan.md` и `test-plan.entry-01.md`

## Defects

Code defects found by the suite. Resolved in `impl` test-fix mode. `Entry` through `Failing test` are never edited once written — the stalemate check compares them (`.asd/workflows/asd-phase-impl-test.md` step 9). One table only: append rows, never a second `## Defects` section — the check fails on one.

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Kalibr.Platform/Migrations/20261105155028_InitialCreate.cs | Шесть колонок без DEFAULT false, в старой линии он есть: promotion_applications.artifacts_missing, is_sent_to_review, review_letter_pending, was_held_at_close; promotion_waves.no_chair_letter_sent, versions_fixed | Разовая проверка A, сравнение схем AC-9 | fixed | 4764e08c |

## Manual verification (optional)

Нет: все критерии проверяются автоматически.
