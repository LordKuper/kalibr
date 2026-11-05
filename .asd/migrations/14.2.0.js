#!/usr/bin/env node
/*
 * ASD migration -> 14.2.0. The external-review negative cache moved from `.asd/project/` into the
 * scratch directory (`.asd/tmp/`, self-ignoring), so the legacy file is dead state. Consumer
 * projects only - this repo has no legacy cache to remove, and never runs this script.
 *
 * Contract (see .asd/skills/asd-update/update.js's own header comment): filename (minus .js) is
 * the target asd_version; module.exports = (ctx) => MigrationReport | Promise<MigrationReport>
 * with ctx.repoRoot = the consumer project root; zero-dependency Node; idempotent - re-running an
 * already-applied migration is a no-op, never an error. This script's MigrationReport shape is
 * `{ deleted, missing }`.
 *
 * Scope: delete `.asd/project/external-cache.json` when present. It is machine-local retry state,
 * so its loss costs at most one retry. Touches nothing else, including the consumer's `.gitignore`
 * (a leftover ignore line is harmless).
 */
'use strict';

const fs = require('fs');
const path = require('path');

const LEGACY_CACHE_PARTS = ['.asd', 'project', 'external-cache.json'];

/** Removes the legacy external-review cache file from the consumer project. */
module.exports = function migrate(ctx) {
  const report = { deleted: [], missing: [] };
  const absPath = path.join(ctx.repoRoot, ...LEGACY_CACHE_PARTS);
  const rel = LEGACY_CACHE_PARTS.join('/');
  if (!fs.existsSync(absPath)) {
    report.missing.push(rel);
    return report;
  }
  fs.rmSync(absPath, { force: true });
  report.deleted.push(rel);
  return report;
};
