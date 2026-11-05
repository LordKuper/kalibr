---
# ASD generated. Edit .asd/skills/asd-phase-design-review/SKILL.md. source_digest=sha256:dbd1a637132f8453a24217bc8991d83584f5bf10ca4d3b40c713010c06fe1bc9 content_digest=sha256:9c39dd663cdd0c52443dded369723b1467cf8c623b125096487a1e0d1667455d asd_version=14.3.0 schema=1
name: asd-phase-design-review
description: "Runs the ASD design-review phase iteratively until DoD met, scoped to whichever drafts documents.* actually enabled this sprint: always dispatches asd-reviewer-combined for any non-empty draft set (and asd-external-review when enabled) — its UI conformance is n/a unless a ux-spec/design-system draft is in scope, never an agent-level skip — aggregates verdicts, fixes CONCERNS inline, and escalates FAIL to the user after an advisor consult. Never dispatched after a design-block collapse (every design documents.* disabled, effective c4 included); this phase's own no-op path is only a defensive fallback for a direct/explicit re-dispatch. Use when asd-sprint dispatches design-review, or when the user explicitly asks to run or re-run design-review for the active sprint."
---

Operation mapping: see `.asd/rules/providers.md`.

Execute workflow `.asd/workflows/asd-phase-design-review.md`.
