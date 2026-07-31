---




name: ws-spec-to-pr-lite
version: 0.0.109
description: Fast sequential Spec-to-PR lite delivery orchestrator FSM (Steps 0–5). Streamlined spec → plan → implement → review → ship → fix-pr pipeline for fast feature delivery.

invocation_names:
  - spec-to-pr-lite
  - ws-spec-to-pr-lite
---

# Spec-to-PR Lite — Orchestrator

Sequential spec→ship using the **same** pipeline skills as [`ws-spec-to-pr`](../ws-spec-to-pr/SKILL.md). Dual-mode: [`gates.md`](../ws-shared/gates.md) · [`config-resolution.md`](../ws-shared/config-resolution.md) · [`setup.md`](../ws-shared/setup.md) · [`ARTIFACTS.md`](../ws-spec-to-pr/ARTIFACTS.md). Do **not** use [`STEP-DISPATCH.md`](../ws-spec-to-pr/STEP-DISPATCH.md) for lite step numbers (standard 0–9 only).

## Native tool contract

Canonical aliases: [`tools.md`](../ws-shared/tools.md). At **every step boundary** in normal mode: prefer `AskQuestion` (host structured choice) with ≥2 options per [`gates.md`](../ws-shared/gates.md); markdown fallback when unavailable; `autoMode` → auto-gate index 0; cancelled → HS-1.

## Invariants

| Topic | Rule |
|-------|------|
| Entry | Same matrix as standard (`setup.md`) — GitHub, ADO, local-spec, free-text |
| Type | `workflowType: lite` — never cross-resume with `standard` |
| Exec | Inline in main session (no subagent dispatch) |
| State | `python .agents/skills/ws-spec-to-pr-lite/scripts/update_state.py` each step; measured `--elapsed` required; missing telemetry = hygiene fail (**HS-5**) |
| Telemetry | Always pass `--jsonl-out {plansDir}/{slug}/telemetry/step-{NN}.jsonl` on every `update_state.py` call (`NN` = zero-padded completed step, e.g. `step-02.jsonl` after Step 2) |
| `skipQualityGates` | `--skip-gates` flag or `config.json` → `invariants.skipQualityGates` — see § Quality gate bypass |
| Artifacts | `step-00` spec · `step-01` plan · `step-08` result (shared names with standard) |
| Commits | Code in implement/review-fix; plan+result at Step 4 G2-delivery |
| Ship / Fix-PR | Step 4 combined gate + `ws-ship-pr` (`workflowMode`, `stopBeforeFixPr`); Step 5 `ws-goal-fix-pr` / `ws-fix-pr` |
| Worktree | Branch-direct default; worktree when `plans.useWorktrees=true` |
| Fable | When `config.json.fable.enabled`: domain@1, judge@3, verify before PR@4 |
| `scoreAndRefine` | Score plan tasks (0–10) in `step-05-{slug}.score-analysis.md`, run 2nd pass implementation, and write `step-08-{slug}.second-pass-report.md` |

Skips interview, DAG, check-implementation, Testing vs standard.

## Steps 0–5

| Step | Label | Skill | Notes |
|------|-------|-------|-------|
| 0 | Spec | providers / `ws-write-spec` | Soft clarify if AC empty; classify before Step 1 |
| 1 | Planning | `ws-write-plan` | No interview/DAG |
| 2 | Implementation | `ws-implement-tasks` build | Build+tests unless `skipTests`; 2nd pass re-run when `scoreAndRefine` |
| 3 | Code Review | `ws-code-review` (+ fix → re-review, max 3) | Artifact `step-06-{slug}.review.md` |
| 4 | Ship | orch + `ws-ship-pr` | Combined delivery+ship gate; 2nd pass comparative report if `scoreAndRefine` |
| 5 | Fix-PR | `ws-goal-fix-pr` / `ws-fix-pr` | After PR; merge only when checks green |

**Transitions** ([`gates.md`](../ws-shared/gates.md)): Advance (Recommended) · More options… (Previous / Repeat / Refine→Replay / Commit / Undo / Pause / Cancel). Banner: model + Pause→host→Resume. When `skipQualityGates` is active, show **`[GATES BYPASSED]`** on Progress Board and in `step-08-{slug}.result.md`. No phase soft tips.

### Post-mutating transition (Steps 0–4 → 1–5)

After step **N** completes (N = 0..4), before Progress Board / Transition Gate / inline work for step **N+1**, run this **shell sequence** (not a subagent):

1. **State hygiene** — `update_state.py` with measured `--elapsed`, file lists, `--gate-choice`, **`--jsonl-out {plansDir}/{slug}/telemetry/step-{NN}.jsonl`**, and **`--bypassed`** when `skipQualityGates` is active.
2. **Checkpoint** — `git tag uswf/{workflow-id}/before-step-{N+1}` (create tag first).
3. **Pre-advance CI** — unless `skipQualityGates`:

```bash
python .agents/skills/ws-spec-to-pr-lite/scripts/validate_state.py \
  {plansDir}/{slug}/{workflow-id}.state.md \
  --pre-advance {N+1}
```

On non-zero exit → **HS-5** (STOP; do not Advance or start step N+1). Applies for **N+1 ∈ {1,2,3,4,5}** (lite steps 1–5).

4. **Progress Board** → Transition Gate → inline step N+1.

`autoMode`: same hygiene + checkpoint + pre-advance; then auto-gate index 0 and continue.

### Step 0 — Pipeline classifier (`--classify`)

After `step-00-{slug}.spec.md` exists and **before** advancing to Step 1:

1. Load and run [`ws-classify-complexity`](../ws-classify-complexity/SKILL.md) (or standalone `--classify <spec-path>`).
2. Writes `{us-dir}/step-00-{slug}.classify.md` (advisory; not a Step 8 delivery artifact).
3. **User gate** unless `autoMode` or `skipQualityGates`: Accept recommendation (Recommended) · Override to standard · Override to lite.
4. Log `classify | recommended={lite|standard} | choice={…} | ISO`. Never silently switch `workflowType` mid-flight.

**Orthogonality:** Classifier recommends `lite` | `standard` orchestrator choice. Full-orch **Complexity gate** (`simple` | `standard` | `complex`) does not apply on lite — do not conflate the axes.

**`scoreAndRefine`:** First Step 0 classify is threshold-only (`deferred` in classify.md). After `step-05-{slug}.score-analysis.md` exists (Step 2 when `scoreAndRefine`), orch may re-invoke classify with `--score-analysis` — advisory only unless user re-gates.

### Step details (done when)

- **0:** `step-00-{slug}.spec.md` via [`setup.md`](../ws-shared/setup.md) § Shared entry → classify (unless `skipQualityGates`) → gate Advance.
- **1:** `step-01-{slug}.plan.md` → Advance.
- **2:** build mode + verification → Advance. When `scoreAndRefine` is active, score plan tasks (0–10) into `step-05-{slug}.score-analysis.md` before/after implementation; optional classify re-invoke after score-analysis (advisory).
- **3:** review file; on Critical/Warning run fix → re-review (max 3; `autoMode` autofix); state/memory each round; Advance only when clean (Pause on residual).
- **4:** checklist `[x]`; write `step-08-{slug}.result.md` with Benchmark Total time (prefix **`[GATES BYPASSED]`** when active) and `step-08-{slug}.second-pass-report.md` if `scoreAndRefine`; combined ship gate; `ws-ship-pr` inline (`workflowType: lite`); auto-run [`ws-spec-index`](../ws-spec-index/SKILL.md) `sync` → Advance when PR/skip done.
- **5:** ≥300s settle; loop until `activeThreads == 0`; `merge-pr` via scm; never delete `project.workingBranch`.

## Quality gate bypass (`skipQualityGates`)

Resolved from `--skip-gates` or `config.json` → `invariants.skipQualityGates` (see [`setup.md`](../ws-shared/setup.md) § Parse flags).

| When active, **skipped** (quality gates only) | **Not skipped** (always enforced) |
|-----------------------------------------------|-----------------------------------|
| Classifier user-gate / recommendation enforcement | Build, test suites, security/leak scan |
| Fable-judge **quality** visibility (PREPARE may ⏭) | SCM resolution, commit/push/PR gates |
| Pre-advance CI (`validate_state.py --pre-advance`) | State Hygiene + required `--elapsed` |
| Telemetry soft gates (`scoreAndRefine` advisory reclassify prompts) | HS-1..HS-4 |

**Safety floor (never bypassed):** When `auditVerdictsBlockShip` is set and fable verdict is **REFUTED**, STOP — same as standard orch.

**Bypass telemetry:** Append to the current step JSONL file:

```json
{"type":"gate-bypass","gate":"{name}","reason":"skip-gates|config","timestamp":"ISO"}
```

**Banner:** Progress Board and `step-08-{slug}.result.md` show **`[GATES BYPASSED]`** when `skipQualityGates` is true.

## State hygiene recipe

Every completed/failed/skipped step:

```bash
python .agents/skills/ws-spec-to-pr-lite/scripts/update_state.py \
  {plansDir}/{slug}/{workflow-id}.state.md \
  --step {N} \
  --status {completed|failed|skipped} \
  --elapsed {elapsedSec} \
  --tokens {promptTokens}:{completionTokens} \
  --model {modelName} \
  --created "{comma_separated_created_files}" \
  --modified "{comma_separated_modified_files}" \
  --deleted "{comma_separated_deleted_files}" \
  --gate-choice "{gateChoice}" \
  --jsonl-out {plansDir}/{slug}/telemetry/step-{NN}.jsonl \
  [--bypassed]
```

`--elapsed` mandatory on `completed`/`failed`. Omitting telemetry or hygiene fail → **HS-5**.

## Auto-gate defaults (`autoMode` → index 0)

| Context | Index 0 |
|---------|---------|
| Transitions | Advance |
| Step 0 classify | Accept recommendation |
| Step 3 fix | Autofix → re-review (max 3); Pause on residual Critical/Warning |
| Step 4 (`fullMode`) | Commit plan+result, create PR |
| Step 4 (not full) | Skip delivery + skip shipping |
| Step 5 | Run ws-goal-fix-pr loop |

## Inline prefix

```markdown
# Inline — Step {STEP} — {Label}
Read state: `{us-dir}/{workflow-id}.state.md`
Skill: {SKILL.md path} — read full.
Orch: ws-spec-to-pr-lite · model {currentModel} · {modeFlags} · workflowType: lite · workflowMode: true
Enhancing skills (mandatory): ws-karpathy-guidelines, ws-senior-developer, ws-tdah, ws-self-learning
Read: state workflow memory + decisions; MEMORY.md index; `config.json.rules.stackFile`.
Config/SCM: `.agents/skills/ws-shared/config-resolution.md`
Anchor: uswf/{workflow-id}/before-step-{STEP} @ {sha} · CWD: {repo-root}
Role: fresh; no resume. files_touched required. model: {currentModel}.
Rules: no `{plansDir}/` in git-add except Step 4 G2-delivery; needs_user: ≥2 choices, recommended first.
End with ```step-output(...)```
```

## Triggers

```
@[ws-spec-to-pr-lite] [auto|dry-run|skip-tests|skip-gates|full|score-and-refine] [--classify <spec-path>] [US {issue_id} | {org}/{project}#{id} | {name}.spec.md | "feature description"]
/ws-spec-to-pr-lite [flags] [--classify <spec-path>] [US {issue_id} | {org}/{project}#{id} | {name}.spec.md | "feature description"]
```

Flags: `auto`, `dry-run`, `skip-tests`, `skip-gates`, `full`, `score-and-refine`. Standalone `--classify <spec-path>` runs [`ws-classify-complexity`](../ws-classify-complexity/SKILL.md) without starting a full workflow.

For interview, DAG, check-implementation, or Testing: use `/ws-spec-to-pr`.
