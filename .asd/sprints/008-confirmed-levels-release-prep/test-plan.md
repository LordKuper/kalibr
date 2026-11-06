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
| 3 |  | delta since entry 2 (`git diff c1325443...HEAD`, без `.asd/sprints`): `ReviewReader.cs`, `tours.ts`, `deploy/README.md`, `TestEnvironment.cs`, `PromotionConfirmedLevelTests.cs` |

## Risk → check decisions

Строки записи 1 — в `test-plan.entry-01.md` (там же результаты разовых проверок B, C, D и первый прогон A), записи 2 — в `test-plan.entry-02.md` (повтор проверки A после D-1).

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `ReviewParts.test.tsx`: `takes the top of the scale of the application it was confirmed in` (замечание F5 `wave-1/iter-01`; строка `remove` записи 2) | отдельного риска нет: компонент видит только `level.scalePoints`, «из 4» уже проверяет первый тест, оба падают от одной мутации `at(0)` | компонентный (vitest) | remove | Выполнено, причина и доказательство — в `Removed tests`. Строка `Added tests` записи 1 об этом тесте (`test-plan.entry-01.md`, сегмент не правится) этим удалением снята; её доказательство относится теперь только к `is one line with the score out of the top of its scale, …` |
| `ReviewReader`: убран фильтр `scale.ContainsKey` (замечание «упасть, а не пропустить», `ca5bba6c`) | оценка компетенции вне шкалы своей версии раньше молча пропускалась, теперь `KeyNotFoundException` | интеграционный | none | Ветка недостижима через API: `PUT .../review` проверяет итоговые оценки по версии заявки (`ScoreRules.Problems`), заявка и оценки одной версии. Тест на неё строил бы состояние прямой записью в базу — гипотетический риск (`code-style.md` §17). Цикл выбора подтверждения на реальных данных идёт во всех тестах `PromotionConfirmedLevelTests` (три версии матрицы, профильная компетенция вне шкалы последней заявки), весь backend зелёный |
| `tours.ts`: фраза о подтверждённом уровне на шагах `review-card` (форма члена; карточка общая, `MemberReviewView`) и `review-scores` (форма председателя, `ChairReviewView`) (AC-8, `84b0cdfe`) | фраза пропала или осталась на одной форме | статическая (сборка, lint) | none | Ни один тест не читает текст справки экрана `promotion`: `ScreenTour.test.tsx` обходит только мастер матрицы. Тест текста закреплял бы формулировку, а не контракт (`code-style.md` §17: утверждение формулировки, а не токена, — не контракт); наблюдаемое поведение AC-8 — строка «Подтверждено: …» на обеих формах — уже закреплено `ReviewParts.test.tsx` и `DemoPage.test.tsx` (оба шага демо). Якоря `data-tour="review-card"` и `tour="review-scores"` на месте в `MemberReviewView.tsx` и `ChairReviewView.tsx`, `lint` и `build` чисты. Меняется только строка литерала |
| `deploy/README.md` раздел 5: порядок шагов обновления, строка «Обновление» (AC-14, `4bd3c30b`) | команда изменилась или потеряна при перестановке | сравнение по `git diff` | none | Блоки кода файла у `c1325443` и `HEAD` дают одинаковый отсортированный md5 строк (`bd5b742b…`), а в изменённых строках `kubectl` и `pg_dump` стоят только в прозе шага: блоки кода не тронуты, переставлены и переписаны шаги 1–2 и пункт списка. Разовые проверки B и C записи 1 не повторялись: их входы (команды) не менялись. `DeploymentTests.Ac6` в полном наборе зелёный |
| `TestEnvironment.CreateDatabaseAsync`: `private` (после удаления теста `Ac63`) | общий для всех интеграционных тестов фундамент | предохранитель общей инфраструктуры | keep | Шаг 8 — полный набор. Побочных вызовов метода не осталось: сборка и 954 теста зелёные |
| `PromotionConfirmedLevelTests`: тест `Ac4_…ComesFromTheLatestOneThatHadIt` и хелпер `PublishVersionAsync` (`5cf024fb`) | выбор подтверждения только из последней заявки | интеграционный | keep | Строка `Added tests` и мутационное доказательство записи 2 (`test-plan.entry-02.md`) остаются в силе: код `ReviewReader` в цикле выбора не менялся с тех пор, строка цикла `ReviewReader.cs:254` та же. Хелпер не покрыт отдельным тестом: он только публикует версию и вызывается обоими `Ac4`-тестами |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| `web/src/features/promotion/ReviewParts.test.tsx`: `takes the top of the scale of the application it was confirmed in` | дубликат первого теста (`is one line with the score out of the top of its scale, …`): компонент читает только `level.scalePoints.at(-1)`, «из 4» первый тест уже закрепляет, а мутация `at(-1)` → `at(0)` роняет оба; шкалу по версии матрицы проверяет сервер (`Ac4`), компоненту она не видна. Доказательство, что первый тест покрывает риск без второго: мутация `at(-1)` → `at(0)` в `ReviewParts.tsx` на месте (восстановлена `git checkout` в той же команде) и `npm --prefix web test -- src/features/promotion/ReviewParts.test.tsx` → exit 1, `1 failed \| 1 passed (2)`, упал `is one line with the score out of the top of its scale, …`. Коммит `857f4f15` | yes |

## Added tests

| Test | Regression proof |
|---|---|

## Suite run

Written twice per cycle: `impl-test`'s suite gate records an **impacted-set** run here each entry
(`.asd/rules/sprint-lifecycle.md` "Impacted test set"); `impl-review`'s terminal step overwrites
it with the cycle's one **full-suite** run once the last review wave's reviewers are
APPROVE/latched. The `pr` gate always reads whatever is recorded here last — the full-suite
record, by the time `pr` runs. Each per-entry record measures only the tree that entry
analysed, not any tree produced later
(`.asd/rules/sprint-lifecycle.md` "Impacted test set").

- Command: `dotnet test --solution Kalibr.slnx` и `npm --prefix web test` (предохранитель общей инфраструктуры: дельта касается `TestEnvironment`, который применяется в каждом интеграционном тесте, — полный набор). Backend запущен один раз на `6c528f7b` (шаг 3 и шаг 8 совпадают: удаление теста backend не меняет), web — шаг 3 до удаления (`ReviewParts`, `DemoPage`, `ScreenTour`: 9 тестов в 3 файлах, exit 0), шаг 8 после удаления целиком. Backend и web запускались по очереди, не вместе
- Scope: full
- Result: pass — backend 954 пройдено, 0 сбоев, 0 пропущено (exit 0, 3 м 15 с, предупреждений 0); web 854 пройдено в 84 файлах, 0 сбоев (exit 0, 49 с). Число backend выросло на 1 (тест записи 2), web снизилось на 1 (удаление F5)
- Lint / build: pass — `dotnet build Kalibr.slnx --tl:off` 0 предупреждений 0 ошибок; `npm --prefix web run lint` (biome ci + typecheck) exit 0; `npm --prefix web run build` exit 0; `npm --prefix web run check:api` exit 0; `dotnet ef migrations has-pending-model-changes` — изменений нет
- HEAD: 857f4f153762acae2109d4c814c2f948ea47f3b4 — web-прогон на нём; backend — на родителе `6c528f7b` (тот же backend-код). Последующий коммит меняет только `test-plan.md` и `test-plan.entry-02.md`

## Defects

Code defects found by the suite. Resolved in `impl` test-fix mode. `Entry` through `Failing test` are never edited once written — the stalemate check compares them (`.asd/workflows/asd-phase-impl-test.md` step 9). One table only: append rows, never a second `## Defects` section — the check fails on one.

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Kalibr.Platform/Migrations/20261105155028_InitialCreate.cs | Шесть колонок без DEFAULT false, в старой линии он есть: promotion_applications.artifacts_missing, is_sent_to_review, review_letter_pending, was_held_at_close; promotion_waves.no_chair_letter_sent, versions_fixed | Разовая проверка A, сравнение схем AC-9 | fixed | 4764e08c |

## Manual verification (optional)

Нет: все критерии проверяются автоматически.
