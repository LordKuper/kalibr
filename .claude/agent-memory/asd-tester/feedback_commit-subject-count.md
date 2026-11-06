---
name: commit-subject-count
description: Count the commit subject (max 50 chars) with node before git commit; a dispatched tester cannot amend afterwards
metadata:
  type: feedback
---

Count the subject before `git commit`: `node -e "console.log([...process.argv[1]].length)" "<subject>"`, at most 50 characters including `type(scope): `. A natural subject like `test(promotion): cover the confirmed level on the card` is 54.

**Why:** a dispatched agent never amends or rebases, so an overlong subject stays and goes to the orchestrator as a flagged item for its pre-push squash.

**How to apply:** draft the subject first, measure it, shorten it, then run the stage-check-commit command once.
