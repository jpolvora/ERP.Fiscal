# Step dispatch (canonical)

**Sole source of truth** for **`ws-spec-to-pr` (standard)** step 0–9 dispatch actions, post-mutating merge notes, and Step 8/9 gate protocols. Load from `SKILL.md` only when advancing or dispatching a step. FSM, invariants, and gates overview stay in `SKILL.md`.

**Dual-mode (mandatory):** This file is **not** the lite step index. [`ws-spec-to-pr-lite`](../ws-spec-to-pr-lite/SKILL.md) keeps its own Steps 0–5 table. Shared gate/ship UX and artifact names stay in [`gates.md`](../ws-shared/gates.md) / [`config-resolution.md`](../ws-shared/config-resolution.md). Pipeline `ws-*` folders (folder == frontmatter `name:`; FSM steps stay 0–9 / Post) stay orch-agnostic: never assume full vs lite step numbers; orch passes `workflowType`, paths, and flags.

## Step instructions

> **Consistency:** the Skill map in `SKILL.md` (`ws-verify-plan` → Step 5, etc.) is authoritative. Keep this table aligned — never dispatch retired ids (`05-verify-sync-plan-us`, `implement-plan`, `plan-us`, …).

| Step | Action | Artifact |
|------|--------|----------|
| 0 | Entry gate (user-gate). US/spec provided → provider or skip to write. No args → free-text → `dispatch-agent` `ws-write-spec`. Optional soft clarify if AC empty. | `step-00-{slug}.spec.md` |
| 1 | Complexity gate → if simple: stub plan + skip to 4. Else `dispatch-agent` `ws-write-plan`. | `step-01-{slug}.plan.md` |
| 2 | Conditional: skip if eligible; else `dispatch-agent` `ws-interview`; 2c End auto-confirms 2e | `step-02-{slug}.plan.refined.md` |
| 3 | `dispatch-agent` `ws-plan-to-tasks`; sequential → skip empty DAG artifacts (log only). Parallel → DAG. | `step-03-*` when parallel |
| 4 | `dispatch-agent` `ws-implement-tasks` mode build; branch-direct default | verification |
| 5 | `dispatch-agent` `ws-verify-plan` **quick-score default** vs refined spec ‖ spec; full matrix if score < 7 or `--strict`; **&lt;7 gate** (refine/replan/respec/approve) | `step-05-{slug}.plan.report.md` |
| 6 | `dispatch-agent` `ws-code-review`; Critical/Warning → **fix → re-review** via `ws-implement-tasks` (max 3; not a separate step); soft model tip for stronger review LLM | `step-06-{slug}.review.md` (+ optional `.fix.report.md`) |
| 7 | Auto-skip if `skipTesting` or (no test surface + unit tests green); else `dispatch-agent` `ws-testing` (Testing) | `step-07-{slug}.testing.*` |
| 8 | Delivery result + **combined ship gate** ([`gates.md`](../ws-shared/gates.md)) → `ws-ship-pr` (`workflowMode: true`, `stopBeforeFixPr: true`). MEMORY sweep after delivery commit. | `step-08-{slug}.result.md` |
| 9 | `dispatch-agent` `ws-goal-fix-pr` (default) or `ws-fix-pr` (one-shot) after PR exists | PR threads / merge |

### Post-mutating transition (after step N completes)

**Order (mandatory):**

1. **`update_state.py`** — merge `files_touched` → Step file log; record telemetry; advance `currentStep`. Always pass `--jsonl-out {plansDir}/{slug}/telemetry/step-{NN}.jsonl` (zero-padded `NN`; lazy-create `telemetry/`). When `--skip-gates` or `config.json.invariants.skipQualityGates` is active, add `--bypassed` on this call.
2. **Checkpoint** — `Shell` tag `uswf/{workflow-id}/before-step-{N+1}` @ HEAD (skip tag write in `dryRun`; log only). Pre-advance soft-passes missing tags when `dryRun: true`.
3. **Pre-advance validation** — **shell command** (not `dispatch-agent`):

```bash
python {skillsRoot}/ws-spec-to-pr/scripts/validate_state.py \
  {plansDir}/{slug}/{workflow-id}.state.md \
  --pre-advance {N+1}
```

On exit ≠ 0 → **HS-5**; **STOP** — no Progress Board, no Transition Gate, no dispatch to step N+1.

**Skip (pre-advance gate only):** When `--skip-gates` or `skipQualityGates` is active, **omit** step 3; log gate-bypass in JSONL (`type: gate-bypass`, `gate: pre-advance`, `reason: skip-gates|config`). Does **not** skip `update_state`, checkpoint, build/test/security, or HS-1–HS-4.

4. **Progress Board** → **Transition Gate** → dispatch step N+1 (or auto-gate + dispatch in `autoMode`).

### Step 5 — Check-implementation (score gate)

Eval implemented code vs **refined spec when present, else `step-00-{slug}.spec.md`**. Publish integer **score 0–10** in Progress Board + report.

When `scoreAndRefine` mode is active (or triggered at bootstrap on completed workflows):
- Evaluates each plan task in `step-01-{slug}.plan.md` on criteria fulfillment, code quality, edge-cases, and test coverage.
- Outputs `step-05-{slug}.score-analysis.md` containing task-by-task scores (0–10) and specific enhancement recommendations.
- **Optional (AC6):** When `step-05-{slug}.score-analysis.md` exists, re-invoke `ws-classify-complexity` with `--score-analysis` before the score gate — advisory only; does not block Advance.
- Prompts **Pass 1 Score Analysis Gate** via `user-gate` (Option 1: Proceed with Second Pass Refinement; Option 2: Accept Pass 1 As-Is & Ship; Option 3: Selective Refinement).
- Option 1 or 3 re-dispatches `ws-implement-tasks` for flagged tasks with scoring feedback, followed by 2nd pass verification.

| Score | Behavior |
|-------|----------|
| ≥ 7 | Complete step 5; Advance to 6 |
| &lt; 7 | User-gate: **Refine** (replay implement + re-check) / **Replan** (back to 1) / **Respec** (back to 0) / **Approve and continue** (log `check-approve-below-7`) |

`autoMode`: do **not** auto-approve below 7 — Pause with score (fail closed).

### Step 6 — Code-review + fix → re-review loop (substep)

| Case | Behavior |
|------|----------|
| Clean (no Critical/Warning) | Complete step 6; Advance to 7 |
| Critical/Warning findings | Fix → re-review rounds via `ws-implement-tasks` mode fix (max **3**); state/memory each round; Advance only when clean |
| Residual after 3 rounds | **Pause** (fail closed) — do not Advance with open Critical/Warning |
| `autoMode` | Autofix (no ask); same max 3; Pause on residual |

Fix is **not** its own `completedSteps` entry — log `review-fix | round={n}/3` in gate history. Contract: [`ws-code-review`](../ws-code-review/SKILL.md) § Fix → re-review loop.

### Step 8 — Ship (delivery + push/PR)

**Order:** [`protocols/delivery-result.md`](protocols/delivery-result.md) (writes `step-08-{slug}.result.md` **with Benchmark Total wall-clock time**) → render Step 8 final board Telemetry ([`progress-board.md`](protocols/progress-board.md)) → **combined delivery + ship user-gate** → on delivery commit: MEMORY sweep → optional temp delete per [`protocols/artifact-cleanup.md`](protocols/artifact-cleanup.md).

When `scoreAndRefine` was executed, generate `step-08-{slug}.second-pass-report.md` comparing Pass 1 vs Pass 2 scores, LOC deltas, quality gains, and test metrics. Include Pass 1 vs Pass 2 comparative summary table in `step-08-{slug}.result.md`.

Telemetry/`--elapsed` still required under `autoMode`/`fullMode` (State Hygiene → HS-5 if missing).

**Combined gate** ([`gates.md`](../ws-shared/gates.md)):

1. **Commit plan + result, then create PR** (Recommended when `fullMode`)
2. **Commit plan + result, push only**
3. **Commit plan + result, skip PR**
4. **Skip delivery commit and skip shipping**
5. **Pause**

G2-delivery stages plan (refined if present) + `step-08-{slug}.result.md` only.

Dispatch `ws-ship-pr` with `workflowMode: true`, `shipAction`, `stopBeforeFixPr: true` — **no goal-fix loop inside ship**; orch Advance to 9 when PR created.
After delivery commit / PR creation, auto-run [`ws-spec-index`](../ws-spec-index/SKILL.md) `sync` with `{slug}` and `shipEvidence`.

### Step 9 — Fix-PR

After Step 8 when `shipAction: create-pr` and PR exists:

1. **Wait for code-review / CI feedback** (same policy as `ws-ship-pr` Step 6 settle: wait 30s post-PR creation for code-review action to start on GitHub infrastructure, then default 300s post-push heartbeats, then poll checks + threads). Do not merge yet.
2. Dispatch `ws-goal-fix-pr` (default loop) or `ws-fix-pr` (one-shot) until **no open issues to fix** (`activeThreads == 0`).
3. **Merge** via SCM provider `merge-pr` only after step 2 converges and required checks are green. Never merge with open review threads or failing required checks.

Stop: max exhausted · escalate · merge blocked · cancelled · PR closed · checks red after convergence attempts.
