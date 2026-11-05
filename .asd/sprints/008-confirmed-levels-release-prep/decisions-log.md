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
- 2026-11-05 — settings: backward_compat none → migration (Task 1, asd-init sprint-mediated)
- 2026-11-05 — route Task 1: mechanical, dispatch HEAD cf7b2c49
- 2026-11-05 — route Task 2: standard, dispatch HEAD 8531ed96
- 2026-11-05 — route Task 3, Task 5, Task 6: standard (Task 5 risk:security), dispatch HEAD 2abb5fd8
- 2026-11-05 — route Task 4: standard (risk:migration), dispatch HEAD 88d91e79
- 2026-11-05 — impl flagged accepted: Task 2 skips a final score whose competency is absent from its own version and breaks DecidedAt ties by application id; Task 5 adds #!/bin/sh to both db-init scripts, leaves docker-compose.dcproj unchanged, renumbers deploy/README.md sections and folds the sign-out note into the recreation bullet. None changes a stated predicate or contract.
- 2026-11-05 — impl assessment approved (adaptive): 7 Tasks done, code and deploy parts of AC-1..AC-19 covered; doc parts of AC-13, AC-17 go to design-promote, AC-20 tag goes after merge; build, lint, check:api, ef pending clean at 213eb654; no stubs. Open for impl-test: Task 5 upgrade and rollback commands in deploy/README.md not run on a cluster; app role not yet run live.
