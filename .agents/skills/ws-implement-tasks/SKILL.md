---













name: ws-implement-tasks
description: Task implementation & fix executor — builds planned features following task DAGs or applies surgical defect fixes from code review findings.
version: 0.0.109
disable-model-invocation: true
invocation_names:
  - implement-tasks
  - ws-implement-tasks
---

# ws-implement-tasks

Execute the coding and testing steps from the plan (build mode) or correct defects from a review or test report (fix mode). Act as a Senior Software Developer: clean code, SOLID, surgical edits, stack-consistent, no duplication.

**Reads:** execution plan (`step-03-*.plan.exec.md`), refined plan (`step-02-*.plan.refined.md`), or draft plan (`step-01-*.plan.md`); `config.json` for layer patterns; `{sharedDir}/MEMORY.md` (Grep task keywords before coding — expand per [`tools.md`](../ws-shared/tools.md) § Path tokens; [`ws-self-learning`](../ws-self-learning/SKILL.md) § Pre-work consult).

## Invocation

Standalone:

```
/implement-tasks <plan-path> [mode=build|fix] [findings=<path>]
```

Workflow (ws-spec-to-pr Step 4 build; Step 6 / lite Step 3 fix → re-review; Step 7 test failures): orchestrator passes `planPath`, `mode`, and optional `findings` path.

| Parameter | Default | Notes |
|-----------|---------|-------|
| `<plan-path>` | required | Execution, refined, or draft plan path |
| `mode` | `build` | `build` or `fix` |
| `findings` | (optional) | Findings report or review comments path |
   - Done when: every planned file is created or modified per its AC.

5. **Validate** — Run the build and unit tests for modified layers (backend/frontend).
   - Done when: build/test results are captured (pass or fail).

6. **Report** — Return the modified/created file lists and test output details.
   - Done when: the step-output below is populated.

## Fix mode

1. **Intake gaps** — Load findings from `step-06-*.review.md` / `step-06-*.fix.report.md`, `step-07-*.testing.report.md`, or review comment threads.
   - Done when: every finding is enumerated.

2. **Consult MEMORY** — Grep `{sharedDir}/MEMORY.md` for the defect class / paths; reuse known Solutions before inventing fixes.
   - Done when: relevant entries noted or none found.

3. **Correct** — Apply minimal, targeted fixes per [ws-karpathy-guidelines](../ws-karpathy-guidelines/SKILL.md).
   - Done when: every enumerated finding has a corresponding edit.

4. **Sweep siblings** — Search modified directories for the same defect class and fix simultaneously.
   - Done when: no sibling occurrence of the fixed defect class remains in modified directories.

5. **Anti-regression test** — Write a unit test covering the corrected defect scenario.
   - Done when: each fixed finding has a covering test.

6. **Validate** — Run the project build and test suites to confirm no regressions were introduced.
   - Done when: build/test results are captured (pass or fail).

## Output (both modes)

Modify the working tree directly; never commit or push.

### step-output (workflow mode)

```yaml
status: success | partial | failed | needs_user
files_touched:
  created: []
  modified: []
  deleted: []
verification:
  files_on_disk: pass | fail
  build: pass | fail | skipped
  tests: pass | fail | skipped
summary: |
  (Summary text of changes and verifications)
```

## Rules

No commit/push (orch/user owns staging). Surgical scope only. Schema migrations via project CLI only. Language: en-us.
