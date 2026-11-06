---
name: one-off-docker-and-mutation-recipes
description: How to run temporary postgres checks, old-vs-new schema comparison and src mutation proofs on Windows without touching the repo tree or the dev stack
metadata:
  type: project
---

**Mutation proofs when `src/` is off-limits:** `git archive HEAD | tar -x -C <scratch>/new`, copy the new test file in, `dotnet restore Kalibr.slnx`, then per mutation: patch the scratch production file (python, keep EOL), `dotnet test --solution Kalibr.slnx --filter-class "*Class"`, copy the pristine file back. Web: `robocopy` `web/` without `node_modules`/`dist`, `New-Item -ItemType Junction` for `node_modules`, run `node_modules/.bin/vitest run <files> -t "<name>"`. Remove the junction with `cmd /c rmdir` before `rm -rf` (rm follows junctions). The repo tree stays untouched.

**Old-vs-new schema:** `dotnet ef migrations script` of an old line breaks on raw `Sql()` without `;` and writes a BOM plus CRLF; apply the old line with `dotnet ef database update --no-build --connection "Host=127.0.0.1;Port=<mapped>;Database=..;Username=postgres;Password=.."` to a container started with `-p 127.0.0.1::5432` (`docker port` shows the port). Sort the column lines of each `CREATE TABLE` in both `pg_dump --schema-only` outputs (python, `-I`), strip `\restrict`/`\unrestrict` lines (random token), then `diff`. A `grep -v` with unbalanced `\(` silently empties a dump: check line counts. `python -I` from Git Bash needs `cygpath -m` paths, else the normalizer writes an empty file and `diff` of two empty files passes: check the normalized line count (about 2000) before trusting it. A repeat run reuses the scratch `norm.py`; build both archives with `dotnet build src/Kalibr.Api/Kalibr.Api.csproj` and apply with `--no-build`.

**Docker on Windows from Git Bash:** `--env-file` and bind-mount sources need Windows paths (`cygpath -m`), `export MSYS_NO_PATHCONV=1`; a wait loop on `pg_isready` must be bounded (`seq`), an unbounded `until` outlives its command and has to be killed by PID. A non-executable init script: `docker create`, then `tar -c --mode=0644 … | docker cp - c:/docker-entrypoint-initdb.d/`, then `docker start` (a plain `docker cp` or a bind mount yields mode 755, which the entrypoint executes instead of sourcing). Remove containers with `docker rm -f -v <exact name>`; without `-v` an anonymous volume stays behind.

**Web suite right after the backend suite:** 5 s timeouts in unrelated files can appear while Testcontainers and builds still load the machine; rerun alone and record both runs.
