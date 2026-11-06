# Memory index

- [ASD self-hosting test-plan conventions](project_asd-self-hosting-testing.md) — this repo IS the ASD framework; `tests/run.js` covers Node sources plus content contracts (rules, templates, skills, workflows, README, agent memory); sweep exemption shape.
- [Web test edits keep LF](feedback_web-test-edits-eol.md) — CRLF from scripted rewrites fails biome ci
- [antd test quirks in web](project_web-antd-test-quirks.md) — duplicate modal title ids, Empty svg title, closed modals stay in DOM
- [Web harness quirks](project_web-test-harness-quirks.md) — leave guard needs data router, RowActions split, unit filter combobox, mutation runner method
- [Backend tests scratch build](project_backend-tests-scratch-build.md) — run API tests under locks; ContractTests root copy; kit helpers (positions, cascade tag)
- [Threshold is contract](feedback_threshold-is-contract.md) — architecture-pinned test threshold: red run is code defect D-N, threshold stays
- [Suite timing operation](feedback_suite-timing-operation.md) — open `suite` before every run, close right after, separate calls
- [Promotion tests runtime](project_promotion-tests-runtime.md) — backend suite alone (not beside web); 1 s tick, NewSceneAsync per test; race tests hold advisory/wave locks
- [Commit subject count](feedback_commit-subject-count.md) — measure the subject (max 50) with node before committing; no amend after
- [One-off docker and mutation recipes](project_one-off-docker-and-mutation-recipes.md) — scratch-copy mutations, old-vs-new schema diff, windows docker quirks
