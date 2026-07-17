# US Workflow — Dry-Run Test

## Purpose

Verify the spec-to-pr FSM executes without error in simulated mode, covering all 7 phases (F0–F6) and steps 0–13 including `fullMode` ship-pr.

## Test Spec

| Field | Value |
|-------|-------|
| **File** | `specs/test-workflow.spec.md` |
| **Slug** | `test-workflow` |
| **Change** | Add `test-workflow` row to AGENTS.md Layer 5 table |
| **Type** | Documentation-only, single-file edit |

## Prerequisites

1. **BOOTSTRAP** section in [`setup.md`](../shared/setup.md) — ensure `.agents/skills/shared/config.json` points at this repo. If absent, create one:

    ```json
    {
      "project": {
        "name": "workflow-skills",
        "baseBranch": "main",
        "workingBranch": "develop",
        "gitRemote": "origin"
      },
      "plans": {
        "dir": ".cursor/plans",
        "specsDir": "specs",
        "worktreesDir": ".cursor/plans/{slug}/worktrees"
      },
      "stack": {
        "id": "markdown-only",
        "description": "Documentation-only repo (markdown)",
        "srcDir": ".",
        "rules": {
          "stackFile": ".agents/skills/spec-to-pr/stack.md"
        },
        "layers": [{ "name": "docs", "path": ".", "role": "skills, specs, docs" }]
      },
      "verification": {}
    }
    ```

   > **Note:** In `dry-run` mode, verification commands are never executed — an empty `verification` block suffices.
   > Canonical artifact names: [`ARTIFACTS.md`](ARTIFACTS.md).

2. **Git status clean** — no dirty files before test:
   ```bash
   git status --porcelain
   ```

3. **Contract self-check** (automated, no agent run):
   ```bash
   npm run tests -- --local
   ```
   This asserts self-overwrite guard, ARTIFACTS/schema presence, Step 11 ownership in AGENTS.md, install tree match, and `update` config preserve.
## Invocation

```text
/spec-to-pr auto full dry-run specs/test-workflow.spec.md
```

### Flag breakdown

| Flag | Effect |
|------|--------|
| `auto` | Auto-selects every gate (no interactive menus) |
| `full` | Enables Step 13 (Ship & PR simulation) |
| `dry-run` | Simulates all steps; no code edits, no commits, no push, no worktrees, no MEMORY.md writes |

## Expected Flow

| Step | Label | Expected action |
|------|-------|----------------|
| **0** | Spec Creation | Skipped — spec file provided directly |
| **1** | Planning | Complexity gate; if simple → stub plan + skip to 5; else `01-write-plan` |
| **2** | Refinement | Conditional skip or `02-interview` (End refinement auto-confirms 2e) |
| **3** | Execution Plan & DAG | `03-plan-to-tasks` — sequential skips empty DAG artifacts |
| **4†** | Phase hint | Folded into Advance (no dedicated menu) |
| **5** | Implementation | `04-implement-tasks` mode build |
| **6** | Verification | `05-verify-plan` quick-score default |
| **7** | Decision & First Commit | Auto-gate: approve → simulate commit |
| **8†** | Phase hint | Folded into Advance |
| **9** | Code Review | `06-code-review` |
| **10** | Fixes & Second Commit | `04-implement-tasks` mode fix |
| **11** | Integration | May auto-skip; else `07-integration-validation` |
| **12** | Delivery | **One** delivery gate → simulate plan+result commit |
| **13** | Ship (`full`) | **One** ship gate → simulate `11-ship-pr` (`workflowMode`) |

## Verification Points

After the workflow completes (`status: completed`), confirm:

### Artifacts created (under `.cursor/plans/test-workflow/`)

| Artifact | Expected |
|----------|----------|
| `{workflow-id}.state.md` | Status: `completed`, `dryRun: true`, completed steps 0–13 |
| `step-00-test-workflow.spec.md` | Copied from `specs/` |
| `step-01-test-workflow.plan.md` | Present |
| `step-02-test-workflow.plan.refined.md` | Present (or skipped if Step 2 bypassed) |
| `step-03-test-workflow.plan.exec.md` | Present |
| `step-03-test-workflow.exec.dag.json` | Present (or skipped if `execMode: sequential`) |
| `step-06-test-workflow.plan.report.md` | Present |
| `step-10-test-workflow.report.md` | Present |
| `step-12-test-workflow.result.md` | Present (dry-run: simulated) |
| `step-11-test-workflow.integration-test.plan.md` | Present |
| `step-11-test-workflow.integration-test.report.md` | Present |

### Files NOT modified

- `AGENTS.md` — must NOT have `test-workflow` row (dry-run)
- `MEMORY.md` — must NOT have changes (dry-run)
- No git commits on branch — `git log --oneline -5` shows no `uswf/` or `feat(test-workflow)` commits
- No worktrees — `git worktree list` shows only main worktree

### State file checks

```yaml
# From {workflow-id}.state.md
dryRun: true
autoMode: true
fullMode: true
status: completed
completedSteps: [0, 1, 2, 3, 5, 6, 7, 9, 10, 11, 12, 13]
stepStatus:
  13: completed            # fullMode
telemetry:
  totalElapsedSec: <int>  # non-null
  loc:
    baseline: <int>
    final: null            # dry-run: no real LOC
```

### No git mutations

```bash
# Verify no commits were made
git log --oneline --all --grep="test-workflow" | head -5

# Verify no tags were pushed (tags should be local only)
git tag -l "uswf/*"
```

### Dry-run specific

- All banners prefixed with `[DRY-RUN]`
- No writes to `src/`, `web/`, or any source paths
- `MEMORY.md` changes logged in `## Doc consolidation log` only
- Step 12 benchmark: `final LOC: null`
- Step 13 PR: simulated, no real `gh pr create`

## Cleanup

After test, remove test artifacts:

```bash
rm -rf .cursor/plans/test-workflow/
git tag -l "uswf/test-workflow*" | xargs git tag -d
```

Or reuse for the next dry-run iteration (workflow auto-detects and resumes).

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| `.agents/skills/shared/config.json` not found | Config not in skill directory | Create minimal config (see prerequisites) |
| Step 1/2 not skipped with simple spec | Dynamic Execution threshold not met | Expected — workflow still runs correctly, just slower |
| Step 11 tries browser | `dry-run` not parsed | Confirm `dry-run` flag is before spec path |
| Step 13 not reached | `full` flag absent | Add `full` before spec path |
| Files written to disk despite dry-run | Dry-run assertion failed | Check `## Gate history` for `dryRun: true` — report as bug |
