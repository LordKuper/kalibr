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
- 2026-11-06 — route impl-test entry 1: standard, dispatch HEAD 08d374fe
- 2026-11-06 — impl-test entry 1: backend 953/953, web 855/855 (first web run after the backend suite: 6 timeouts in load-sensitive tests outside the change surface, rerun green), build, lint, check:api, ef pending clean; 2 backend and 5 web tests added, 1 removed with its migration (Task 7).
- 2026-11-06 — impl-test memory hit: .claude/agent-memory/asd-architect/feedback_contract-removal-classification.md repeats the removed rule; left as is per plan.md «Вне Task-блоков»: asd-architect is not on the roster, no owner to dispatch.
- 2026-11-06 — impl-test: docs/architecture/audit.md, career-framework.md, org-structure.md still name the old migration line; added to the design-promote list beside the plan.md items.
- 2026-11-06 — impl-test: AC-9 demands an equal schema; dropping it is a scope change, not an adaptive acceptance (checkpoints.md Gate policy), so the six DEFAULT false columns are fixed in InitialCreate.
- 2026-11-06 — impl-test: defects D-1 → impl test-fix (digest d4da786c512ab64646683fb05a3edb751bbd18dc09a7de6342f86c32e96d0334)
- 2026-11-06 — route D-1: standard (risk:migration), dispatch HEAD 80394ba4
- 2026-11-06 — impl test-fix: defects D-1 resolved
- 2026-11-06 — route impl-test entry 2: standard, dispatch HEAD 52371598
- 2026-11-06 — impl-test: impacted set green (full: backend 953/953, web 855/855; AC-9 comparison with no difference), 0/0 tests
- 2026-11-06 — impl-review wave-1/iter-01: combined CONCERNS (F1-F6), external APPROVE; → impl review-fix. F1 reverses the Task 2 flagged choice accepted in this sprint (skip of a final score absent from its version): the reviewer showed the case is impossible by contract.
- 2026-11-06 — route F1, F2, F6: standard, dispatch HEAD 5f29bf77
- 2026-11-06 — route F3, F4, F5: standard, dispatch HEAD 4bd3c30b
- 2026-11-06 — impl fix for wave-1/iter-01: findings resolved (F1 ca5bba6c, F2 84b0cdfe, F6 4bd3c30b, F3 and F4 5cf024fb). F5 is a test removal, which a review-fix tester may not perform (artifact-layout.md Test plan); recorded as a remove row for impl-test entry 3.
