---
responsibility:
  owns: per-sprint log of workflow friction — a rule, phase, gate, agent, skill, template or provider tool that malfunctioned or could not be followed
  excludes: code defects (test-plan.md D-N), artifact-quality findings and verdicts (reviews/), human operational actions (manual-steps.md MS-N), decisions taken (decisions-log.md)
  delegates_to: test-plan.md (defects), reviews/ (verdicts), manual-steps.md (manual actions), decisions-log.md (decisions), retrospective.html (analysis and recommendations)
---

# Friction log — sprint 008-confirmed-levels-release-prep

<!--
Lifecycle, what qualifies, what never does, the F-N id scheme and who appends:
.asd/rules/sprint-lifecycle.md "Friction log" — not restated here.
Entry content is language.docs.
Consumed by the retro phase (.asd/workflows/asd-phase-retro.md).
-->

## Summary

| ID | Phase | Problem | Refs |
|---|---|---|---|
| F-1 | impl | `timing-recover` посреди фазы закрыл только что открытую операцию, в леджере остался разрыв | D-1 |
| F-2 | impl-test | Тестировщик оценил расхождение с критерием приёмки как допустимое вместо записи дефекта | D-1 |
| F-3 | impl | Находку на удаление теста нельзя выполнить в review-fix, она перешла в следующий вход impl-test | reviews/impl/wave-1/iter-01/combined |
| F-4 | impl-review | Оценка `critical` у мелкой находки в тесте закрыла минорный путь и запустила полный цикл исправления | reviews/impl/wave-1/iter-02/combined |
| F-5 | impl | Темы коммитов превышают 50 символов, в том числе заданная шаблоном фреймворка | — |
| F-6 | impl-test | `dotnet test` однократно завершился с «Handshake failures», код 5 | — |
| F-7 | impl-test | Память агента вне ростера повторяет удалённое правило, поправить её некому | — |

## F-1 — `timing-recover` посреди фазы закрыл только что открытую операцию, в леджере остался разрыв

- **Phase**: impl
- **Surface**: provider tool — `.asd/runtime.js` `timing-recover`, `asd-sprint` Step 2B
- **What happened**: Оркестратор запустил `timing-recover` вне потока возобновления, сразу после открытия `impl#2`. Команда закрыла фазу как `interrupted`, поэтому первая запись `D-1` открылась вне фазы; пришлось закрыть её и открыть `impl#3` и `D-1` заново.
- **Impact**: 7 секунд и две прерванные операции; в `timing-summary` раздел `gaps.outside_phase` содержит `D-1`.
- **Refs**: D-1

## F-2 — Тестировщик оценил расхождение с критерием приёмки как допустимое вместо записи дефекта

- **Phase**: impl-test
- **Surface**: agent — `.asd/agents/asd-tester.md`, раздел «Failure triage»
- **What happened**: Разовая проверка показала расхождение, которое AC запрещает, но упавшего теста не было; тестировщик вернул его как приемлемое. Оркестратор отклонил оценку по правилу `checkpoints.md` «Gate policy»: отказ от требования меняет скоуп.
- **Impact**: Оркестратору понадобилось отдельное решение; без него требование молча выпало бы из спринта.
- **Refs**: D-1

## F-3 — Находку на удаление теста нельзя выполнить в review-fix, она перешла в следующий вход impl-test

- **Phase**: impl
- **Surface**: rule — `.asd/rules/artifact-layout.md` «Test plan», `.asd/workflows/asd-phase-impl.md` review-fix
- **What happened**: Находка требовала удалить тест, а тестировщик review-fix удалять тесты не вправе; цепочка исправлений дошла до этой находки и остановилась, строка удаления перенесена в третий вход impl-test.
- **Impact**: Находка закрыта на шаг позже остальных из той же итерации, маршрут пришлось решать вручную.
- **Refs**: reviews/impl/wave-1/iter-01/combined

## F-4 — Оценка `critical` у мелкой находки в тесте закрыла минорный путь и запустила полный цикл исправления

- **Phase**: impl-review
- **Surface**: rule — `.asd/rules/review-policy.md`, «Over-engineering checklist» и «Minor test-only findings»
- **What happened**: Находка чек-листа over-engineering всегда `critical`, а минорный путь допускает только `low` и `medium`. Правка одной сигнатуры в тесте, не меняющая поведения, прошла через impl, impl-test и третью итерацию ревью.
- **Impact**: 15 мин 16 с: `impl#5` 258 с, `impl-test#4` 140 с, `impl-review#3` 518 с.
- **Refs**: reviews/impl/wave-1/iter-02/combined

## F-5 — Темы коммитов превышают 50 символов, в том числе заданная шаблоном фреймворка

- **Phase**: impl
- **Surface**: rule — `.asd/rules/git-strategy.md` «Commits», `asd-sprint` правило `rotate-decisions`
- **What happened**: Четыре темы агентов длиннее 50 символов (52–58), дополнить их агент не может. Шаблон `chore(sprint): rotate decisions log for <phase>` для `design-promote` даёт 54 символа, шаблон закрытия спринта с длинным slug — 52, поэтому правило нельзя соблюсти дословно.
- **Impact**: Нарушения остаются в истории ветки до squash при мерже; агенту `asd-tester` пришлось завести отдельную запись памяти.
- **Refs**: —

## F-6 — `dotnet test` однократно завершился с «Handshake failures», код 5

- **Phase**: impl-test
- **Surface**: provider tool — `dotnet test --solution Kalibr.slnx`
- **What happened**: В одном запуске тестировщика команда завершилась с «Handshake failures» и кодом 5 без упавших тестов. У оркестратора и в последующих запусках сбой не повторился, причина неизвестна.
- **Impact**: Один лишний запуск и ручная проверка, что причина не в коде спринта.
- **Refs**: —

## F-7 — Память агента вне ростера повторяет удалённое правило, поправить её некому

- **Phase**: impl-test
- **Surface**: agent — `.claude/agent-memory/asd-architect/feedback_contract-removal-classification.md`
- **What happened**: Запись памяти повторяет правило, заменённое в этом спринте, а агента `asd-architect` больше нет в ростере; правка через владельца невозможна. Такие же каталоги без владельца есть и у других снятых агентов.
- **Impact**: Устаревший текст оставлен на месте, решение записано в `decisions-log`.
- **Refs**: —
