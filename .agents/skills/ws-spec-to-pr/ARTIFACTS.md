# Artifact Registry (canonical)

**Sole source of truth** for workflow artifact names and paths. `SKILL.md`, FAQ, DIAGRAM, and pipeline skills must reference this file; do not invent alternate names.

**Related (not plan-dir artifacts):** Step 0–9 dispatch actions and Step 8/9 gate protocols for **standard** orch live in [`STEP-DISPATCH.md`](STEP-DISPATCH.md) — load only when advancing/dispatching. Step 8 delivery templates: [`protocols/delivery-result.md`](protocols/delivery-result.md), optional cleanup [`protocols/artifact-cleanup.md`](protocols/artifact-cleanup.md). Lite orch keeps its own Steps 0–5 table; shared gates stay in [`../ws-shared/gates.md`](../ws-shared/gates.md).

## Path resolution

| Token | Resolution |
|-------|------------|
| `{plansDir}` | **Token** (not a config key). Resolve from `config.json` → `plans.dir` (default value: `.agents/plans`) |
| `{specs-dir}` | **Token**. Resolve from `config.json` → `plans.specsDir` (default: `.agents/specs`; prefer existing repo-root `specs/` when present) — optional mirror only |
| `{us-dir}` | `{plansDir}/{slug}/` |
| `{slug}` | `us-{id}` for issues; basename or frontmatter `slug:` for local specs |
| `{workflow-id}` | Unique run id; state file basename without `.state.md`. **Format:** `{slug}-{YYYYMMDDTHHMMSSZ}[-{suffix}]` (issue runs: `us-{id}-{YYYYMMDDTHHMMSSZ}`). Examples: `us-2416-20260621T214006`, `spec-provider-skills-20260713T142006Z-7cdbef`. **Not** `step-*` (reserved for step artifacts below) and **not** invented abbreviations (`stp-`, `uswf-` as basename). |
| `{worktrees-dir}` | **Token**. Resolve from `config.json` → `plans.worktreesDir` with `{slug}` substituted (default: `{us-dir}/worktrees`) |
| `{reviewsDir}` | **Token** (not a config key). Resolve from `config.json` → `reviews.dir` (default value: `.agents/codereviews`) |

Never write workflow state under `.agents/`.

## Canonical artifacts (under `{us-dir}`)

| Artifact | Filename | Produced by | Committable |
|----------|----------|-------------|-------------|
| State | `{workflow-id}.state.md` | Orchestrator | No |
| Issue snapshot | `step-00-{slug}.issue.json` | Step 0 / issue fetch | No |
| **Spec (canonical)** | `step-00-{slug}.spec.md` | Step 0 / issue→spec / local register | No |
| Complexity classification | `step-00-{slug}.classify.md` | Step 0 (`ws-classify-complexity`) | No |
| Plan | `step-01-{slug}.plan.md` | Step 1 | **Yes (Step 8)** if no refined plan |
| Refined plan | `step-02-{slug}.plan.refined.md` | Step 2 | **Yes (Step 8)** if present (replaces plan) |
| Exec plan | `step-03-{slug}.plan.exec.md` | Step 3 | No |
| DAG | `step-03-{slug}.exec.dag.json` | Step 3 | No |
| Check-implementation report | `step-05-{slug}.plan.report.md` | Step 5 | No |
| Code review | `step-06-{slug}.review.md` | Step 6 | No |
| Review fix report | `step-06-{slug}.fix.report.md` | Step 6 fix → re-review loop | No |
| Testing plan | `step-07-{slug}.testing.plan.md` | Step 7 | No |
| Testing report | `step-07-{slug}.testing.report.md` | Step 7 | No |
| Delivery result | `step-08-{slug}.result.md` | Step 8 | **Yes (Step 8)** |

**Do not write obsolete names:** `step-06-*.plan.report.md`, `step-10-*.report.md`, `step-11-*.integration-test.*`, `step-12-*.result.md`.

## Step input prerequisites

Minimum on-disk artifacts required before **advance to step N** (standard FSM). Consumed by `validate_state.py --pre-advance <N>` after checkpoint tag `uswf/{workflow-id}/before-step-{N}`.

| Advance to step N | Required on disk (minimum) |
|-------------------|----------------------------|
| 1 | `step-00-{slug}.spec.md` |
| 2 | `step-00-{slug}.spec.md` + `step-01-{slug}.plan.md` |
| 3 | `step-00-{slug}.spec.md` + `step-02-{slug}.plan.refined.md` if interview ran, else `step-01-{slug}.plan.md` |
| 4 | `step-02-{slug}.plan.refined.md` if present, else `step-01-{slug}.plan.md` |
| 5 | plan or refined plan + implementation tree (state manifest `created` / `artifacts` non-empty, or `dryRun`) |
| 6 | `step-05-{slug}.plan.report.md` |
| 7 | `step-06-{slug}.review.md` when code review ran |
| 8 | `step-07-{slug}.testing.report.md` when Step 7 completed (not skipped) |
| 9 | `step-08-{slug}.result.md` + PR exists (ship evidence) |

**Lite orch:** mirrors advance-to steps **1–5** with the same artifact names where those steps exist; lite ship still uses `step-08-{slug}.result.md` at delivery.

`step-00-{slug}.classify.md` is advisory (Step 0); it is **not** a prerequisite for advance-to.

## Step 8 delivery commit

Stage **only**:

1. `step-02-{slug}.plan.refined.md` if it exists, else `step-01-{slug}.plan.md`
2. `step-08-{slug}.result.md`

**Not staged:** `step-00-{slug}.classify.md`, `{us-dir}/telemetry/`, and other runtime artifacts.

## Spec entry rules

| Input | Action | Canonical write | `source` frontmatter |
|-------|--------|-----------------|----------------------|
| GitHub `{n}` / `US {n}` | [`ws-github-provider`](../ws-github-provider/SKILL.md) `fetch-to-spec` | `{us-dir}/step-00-{slug}.spec.md` | `github` |
| ADO `{org}/{project}#{id}` or `ADO {id}` / `WI {id}` | [`ws-azure-devops-provider`](../ws-azure-devops-provider/SKILL.md) `fetch-to-spec` | `{us-dir}/step-00-{slug}.spec.md` | `azure-devops` |
| Hand-written `*.spec.md` (any path) | [`ws-local-spec-provider`](../ws-local-spec-provider/SKILL.md) `fetch-to-spec` | `{us-dir}/step-00-{slug}.spec.md` | `local` |
| Free-text brainstorm | `ws-write-spec` (optional mirror via `ws-local-spec-provider`) | `{us-dir}/step-00-{slug}.spec.md` | `local` |

Optional: copy a read-only mirror to `{specs-dir}/{slug}.spec.md` for human browsing. Downstream skills **always** read `## Artifacts.specPath` (must point at the `step-00-` file under `{us-dir}`).

**Snapshot (audit-only):** tracker fetches also write `step-00-{slug}.issue.json` (GitHub issue JSON or ADO WIT JSON). Never treat the snapshot as the canonical spec.

## Forbidden aliases

Do **not** use these as canonical paths (legacy FAQ drift):

- `{us-dir}/{slug}.spec.md`
- `{us-dir}/{slug}.plan.md`
- `{specs-dir}/{slug}.spec.md` as the only copy (mirror OK)
- Bare `{slug}.result.md` without `step-08-` prefix
- `stp-*.state.md` or any invented prefix for state (use `{workflow-id}.state.md` with `{slug}-{ISO}` form)
- `step-*.state.md` — `step-NN-` is **only** for step deliverables in the table above, never for state/archive/baseline

## Runtime (portability)

| Path | Purpose |
|------|---------|
| `{us-dir}/.runtime/` | Sentinels, PIDs, temp wake signals (not `/tmp`) |
| `{us-dir}/telemetry/` | Per-step JSONL telemetry (`step-{NN}.jsonl`); append on each `update_state` invocation for that step |
| `{plansDir}/telemetry/` | Project-wide aggregate output (`aggregate.json`; not per-workflow) |
| `{worktrees-dir}/step-{N}/` | Step isolation (code steps preferred) |
| `{us-dir}/{workflow-id}.archive/` | Archived stale workflows |
| `{us-dir}/{workflow-id}.baseline/` | Baseline snapshots |

## Skill → step ownership (standard FSM)

| Step | Skill |
|------|-------|
| 0 | `ws-write-spec` (brainstorm only) |
| 1 | `ws-write-plan` |
| 2 | `ws-interview` |
| 3 | `ws-plan-to-tasks` |
| 4 | `ws-implement-tasks` (build) |
| 5 | `ws-verify-plan` |
| 6 | `ws-code-review` (+ `ws-implement-tasks` fix → re-review, max 3) |
| 7 | `ws-testing` (Testing) |
| 8 | `ws-ship-pr` (delivery + push/PR; no terminal goal-fix) |
| 9 | `ws-fix-pr` / `ws-goal-fix-pr` |
