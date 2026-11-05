---
responsibility:
  owns: project-owner custom rules read by all agents in all phases
  excludes: phase-specific rules (design-only, coding-only)
  delegates_to: custom-design-rules.md (design/design-review), custom-coding-rules.md (impl/impl-review), .asd/rules/ (workflow rules)
---

# Custom Common Rules

Universal project rules read by every ASD agent in every phase. Put here: domain glossary, naming conventions, compliance requirements, project-wide vocabulary, anything holding across design AND code.

Phase-scoped rules go elsewhere — design constraints to `custom-design-rules.md`, code/test constraints to `custom-coding-rules.md`. ASD never overwrites this file.

## Docker safety

- Agents never run destructive Docker commands (`down -v`, `volume rm`, `volume prune`) against the user's dev stack or volumes with a fixed name. Temporary runs use only their own named volumes and delete them by exact name.

## Agent memory edits

- Agents change an existing memory file with a targeted edit and never overwrite a memory file whole. (`002-mail-invite-password-reset#A-2`)

## No financial data

- The system stores no financial information: salaries, salary ranges, budgets. No field, column, endpoint or screen carries it.

## Release compatibility

- The API keeps no backward compatibility: the SPA, its only consumer, ships in the same image. The contract version stays 1.0.0 and the path `/api/v1`; obsolete surface is deleted at once, never marked deprecated. This overrides `backward_compat: migration` for the API: a breaking API change needs no migration path, deprecation notice or version bump.
- Data survives every release: the DB schema changes only forward, by incremental EF migrations over the baseline `InitialCreate`, which is never regenerated. A migration preserves existing data and transfers it where a change moves it or alters its meaning; the transfer is SQL in `Up` after the new structure is created and before the old one is dropped (`docs/architecture/stack.html` «Контракт из кода»).
- An upgrade starts with a database backup; a rollback restores the backup and the previous image. Expand/contract is not used: one replica, `strategy: Recreate`. (`008-confirmed-levels-release-prep#AC-13`, replaces `004-role-desktop-user-journeys#AC-58`, `#AC-63`)

## Blocking project processes

- An agent may stop a user-started process of this project that blocks a build, a test run or a file, and only these: Kalibr.Api (`Kalibr.Api.exe`, or `dotnet` running this repository's `Kalibr.Api.dll`); the Visual Studio instance with this repository's `Kalibr.slnx` open (`devenv.exe`) and its build processes (`MSBuild.exe`, `VBCSCompiler.exe`, `ServiceHub.*`); `testhost.exe` and `dotnet test` of this repository; the frontend dev server (`node` running `vite` from `web/`).
- The process is stopped by PID, found by this repository's path or command line, never by whole image name. The browser, Docker and any other process are never stopped. (`005-org-structure-excel-import#AC-25`, replaces rejected `004-role-desktop-user-journeys#A-4`, `#A-7`)

## Multi-role screen mockups

- A sprint adding new screens for more than one role agrees at scope, before the plan, on mockups of each role's screens and an end-to-end walkthrough of the process stage by stage. (`006-promotion-waves#P-1`)
