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
- 2026-11-05 — workflow: lite (выбор пользователя; рекомендован был standard)

## 2026-11-05 — Выбор подхода спринта

- **Decision**: подтверждённый уровень вычисляется из итоговых баллов заявок с решением и считается подтверждённым с публикации результатов; приложение работает одной ролью БД без прав суперпользователя, миграции остаются при старте; режим миграций после релиза — только вперёд с резервной копией перед обновлением; контракт остаётся 1.0.0 и `/api/v1`.
- **Rationale**: выбор пользователя на стадии (c). Рекомендованный вариант В (две роли, init-контейнер) отклонён: пользователь выбрал меньшую правку, DDL из рантайма остаётся доступным.
- **Affected docs**: research.md
- 2026-11-05 — retro intake: кандидатов на решение нет; 007#A-1, 007#P-1, 007#P-2 — только upstream, показаны на гейте scope; записей в retro-backlog нет

## 2026-11-05 — Скоуп спринта принят

- **Decision**: скоуп AC-1…AC-20 принят пользователем одним спринтом, без разделения; принятие разрешает удаление 15 файлов миграций (AC-9) и отправку тега v1.0.0 в origin после мержа (AC-20). Стоимость критериев: 0 итераций, 0 раундов правок.
- **Rationale**: блоки слабо связаны, но подтверждения не меняют схему и не мешают заморозке; один PR.
- **Affected docs**: sprint.md
