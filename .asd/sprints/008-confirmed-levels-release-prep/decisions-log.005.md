---
responsibility:
  owns: approved decisions for THIS sprint
  excludes: cross-sprint/durable decisions, sprint state, review notes
  delegates_to: docs/** + adr fold targets (durable design decisions), CHANGELOG.md (releases), .asd/project/stubs.md (standing open defects), .asd/project/retro-backlog.md (retro row dispositions), state.json (state), reviews/ (verdicts)
---

# Decisions Log

Per-sprint, append-only. Never edited or removed. Created at `scope`, rotated at phase entry (`.asd/rules/artifact-layout.md` "Decisions log"), archived with the sprint.

## Entry format

```markdown
## YYYY-MM-DD — <one-line summary>

- **Decision**: <what was decided> (≤3 sentences)
- **Rationale**: <why> (≤3 sentences)
- **Affected docs**: <links> (unrestricted)
```

A no-op skip, other zero-content decision, dispatch routing line or failed-dispatch reconstruction uses the one-line form instead:

```markdown
- YYYY-MM-DD — <phase> skipped: <reason>
- YYYY-MM-DD — route <taskIds>: <tier>, dispatch HEAD <sha>[; risk <declaration>]
- YYYY-MM-DD — reconstruction: landed <ids>; re-dispatched <ids>
- YYYY-MM-DD — stall: <agent> <dispatch ids>
- YYYY-MM-DD — cleanup checkpoint <sha>
- YYYY-MM-DD — cleanup: removed <paths/symbols>; reverted <ids or none>; verified <build, lint, tests @sha>
```

## Durability rule

A decision whose value must survive this sprint's archival is ALSO written into an existing persistent home — a `docs/` fold target, `CHANGELOG.md`, `.asd/project/stubs.md`, or `.asd/project/retro-backlog.md` (retro row dispositions). Never invent a new document type for this. This log records that the decision was made; the persistent home is what a later sprint can still read.

## Entries

<!-- entries appended below this line -->

- 2026-11-06 — design-promote decomposition (gate adaptive, оркестратор): новой подсистемы и изменений границ нет; `subsystems.md` и диаграмма mermaid без изменений.
- 2026-11-06 — design-promote design-system gate: новых токенов нет, `DESIGN.md`, `design-system.html`, `accessibility.html` без изменений; ux-spec выключен.
- 2026-11-06 — design-promote: AC-1…AC-8 спринта вошли в `promotion.html` как AC-96…AC-103 с историей комиссии; релизные решения — в `stack.html` «Контракт из кода» (ревизия 22), «Учётная запись БД приложения», Q5, `efcore-10.0.12.md`, `postgres-image-18.6-trixie.md`, `desk.html` AC-121; подтверждённый уровень — в `promotion.md`.
- 2026-11-06 — design-promote сверх списка плана: `audit.md`, `career-framework.md`, `org-structure.md` ссылались на удалённые миграции и схлопывание, исправлены; `audit.md` — и на прежнего суперпользователя. Требования AC-149, AC-152, AC-41 (отозванные) и история ревизий `stack.html` не менялись: это история.
