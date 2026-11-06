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
| 3 | bb3836fe | delta since entry 2 (`git diff c1325443...HEAD`, без `.asd/sprints`): `ReviewReader.cs`, `tours.ts`, `deploy/README.md`, `TestEnvironment.cs`, `PromotionConfirmedLevelTests.cs` |
| 4 | df622873 | delta since entry 3 (`git diff bb3836fe...HEAD`, без `.asd/sprints`): `PromotionConfirmedLevelTests.cs` |

## Risk → check decisions

Строки записи 1 — в `test-plan.entry-01.md` (там же результаты разовых проверок B, C, D и первый прогон A), записи 2 — в `test-plan.entry-02.md` (повтор проверки A после D-1), записи 3 — в `test-plan.entry-03.md` (удаление дубликата в `ReviewParts.test.tsx`, решения `none` по `ReviewReader`, `tours.ts`, `deploy/README.md`).

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `PromotionConfirmedLevelTests.PublishVersionAsync`: возвращаемое значение убрано (`93d7a278`) | поведение хелпера не менялось: оба вызова результат не использовали, оба `Ac4`-теста строят те же версии матрицы | интеграционный | none | Сигнатура приватного тестового хелпера: `Task<Guid>` → `Task`, ни одна строка кода продукта не тронута, новых рисков нет. Отдельный тест на хелпер закреплял бы реализацию теста; прогон класса `PromotionConfirmedLevelTests` (шаги 3 и 8) подтверждает, что оба `Ac4`-теста собираются и проходят. Строка `Added tests` и мутационное доказательство записи 2 (`test-plan.entry-02.md`) остаются в силе |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|

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

- Command: `dotnet test --solution Kalibr.slnx` затем `npm --prefix web test` из корня репозитория, без фильтров, конфигурация Debug; запуски по очереди, не рядом. "Handshake failures" не было, повторов не было
- Scope: full
- Result: pass — backend 954 пройдено, 0 сбоев, 0 пропущено (exit 0, 3 мин 28 с); web 84 файла и 854 теста пройдено, 0 сбоев (exit 0, 64.5 с)
- Lint / build: pass — `npm --prefix web run lint` (biome ci + typecheck) exit 0; `dotnet build Kalibr.slnx --tl:off` 0 предупреждений 0 ошибок, exit 0; `npm --prefix web run build` exit 0
- HEAD: 7d3b3725620f3d9f70d1be0a392ccd09416d799f

## Defects

Code defects found by the suite. Resolved in `impl` test-fix mode. `Entry` through `Failing test` are never edited once written — the stalemate check compares them (`.asd/workflows/asd-phase-impl-test.md` step 9). One table only: append rows, never a second `## Defects` section — the check fails on one.

| ID | Entry | Location | Symptom | Failing test | Status | Fix commit |
|---|---|---|---|---|---|---|
| D-1 | 1 | src/Kalibr.Platform/Migrations/20261105155028_InitialCreate.cs | Шесть колонок без DEFAULT false, в старой линии он есть: promotion_applications.artifacts_missing, is_sent_to_review, review_letter_pending, was_held_at_close; promotion_waves.no_chair_letter_sent, versions_fixed | Разовая проверка A, сравнение схем AC-9 | fixed | 4764e08c |

## Manual verification (optional)

Нет: все критерии проверяются автоматически.
