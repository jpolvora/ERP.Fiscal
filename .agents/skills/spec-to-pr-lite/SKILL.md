---
name: spec-to-pr-lite
description: >-
  Spec-to-PR lite delivery orchestrator FSM. Fast sequential plan → implement → review → deliver → optional ship.
  Invoke: /spec-to-pr-lite | @[spec-to-pr-lite]. Entry: GitHub issue | Azure DevOps work item | *.spec.md.
  Flags: dry-run, auto, skip-tests, full, --model, --model-chain. Delegates via Task tool.
  Dual-mode compatible with spec-to-pr (shared skills, shared/config.json, shared/gates.md).
upstream: jpolvora/workflow-skills — this skill is a workflow owned by workflow-skills. Improvements must be submitted upstream to https://github.com/jpolvora/workflow-skills
---

# Spec-to-PR Lite — Orchestrator

Deterministic FSM for sequential plan-to-ship delivery. Reuses the **same** pipeline skills as [`spec-to-pr`](../spec-to-pr/SKILL.md). Dual-mode contract: [`gates.md`](../shared/gates.md), [`config-resolution.md`](../shared/config-resolution.md), [`setup.md`](../shared/setup.md). Do **not** load [`spec-to-pr/STEP-DISPATCH.md`](../spec-to-pr/STEP-DISPATCH.md) for lite step numbers — that file is standard-orch only (0–13).

## Core Goals

1. **Faster Turnaround:** Skip Step 0 brainstorm, Step 2 interview, Step 3 DAG, Step 6 verify, Step 11 integration.
2. **Compatible gates:** Same slim AskQuestion UX, one delivery gate, one ship gate as full orch.
3. **Portability:** Config only from `.agents/skills/shared/config.json`.

## Invariants

- **Entry:** Requires existing spec (issue ID, ADO WI, or `*.spec.md`). No free-text brainstorm (use full `spec-to-pr` for that).
- **`workflowType: lite`** in state — resume never cross-mixes with `standard`.
- **State hygiene:** `python .agents/skills/spec-to-pr-lite/scripts/update_state.py` after every step (same fields as full; shared artifact names).
- **Artifacts:** Plan `step-01-{slug}.plan.md`; result `step-12-{slug}.result.md` (shared naming with full — do not invent `step-04-*.result.md`).
- **Commits:** Code only during Steps 2–3. Plan + result only at Step 4 delivery gate.
- **Ship:** Orch asks once; `11-ship-pr` with `workflowMode: true` does not re-ask.
- **Branch-direct** default (same worktree fallback as full).

---

## Steps 1–5 Index

| Step | Label | Task? | Skill | Notes |
|------|-------|-------|-------|-------|
| 1 | Planning | ✓ | `01-write-plan` | No interview / DAG |
| 2 | Implementation | ✓ | `04-implement-tasks` build | Verify build/tests unless `skipTests` |
| 3 | Code Review & Fix | ✓ | `06-code-review` (+ fix loop) | Findings gate |
| 4 | Consolidation & Delivery | ✓ | orch + shell | **One** delivery gate |
| 5 | Ship & PR | ✓ | `11-ship-pr` | **One** ship gate |

---

## Transition menu (every step boundary)

Per [`gates.md`](../shared/gates.md):

1. **Advance** (Recommended)
2. **More options…** → Switch model / Repeat / Pause or cancel

No 5-option primary menus. Progress Board: bootstrap, after each step summary, pause, `/status`, final.

---

## FSM Step Instructions

### Step 1: Planning

- Dispatch `Task` → `01-write-plan` with spec input.
- Output: `step-01-{slug}.plan.md`.
- Gate: Advance → Step 2.

### Step 2: Implementation

- Dispatch `Task` → `04-implement-tasks` `mode=build` with `step-01-{slug}.plan.md`.
- Run verification (build + tests unless `skipTests`).
- Gate: Advance → Step 3.

### Step 3: Code Review & Fix

- Dispatch `Task` → `06-code-review` vs base branch.
- **Findings gate** (if Critical/Warning):
  1. **Apply fixes now** (Recommended)
  2. **Proceed without fixing**
  3. **Pause**
- Apply fixes → `04-implement-tasks` `mode=fix` → re-verify → re-review once (cap 2 fix rounds in auto).
- No findings or Proceed → Advance → Step 4.

### Step 4: Consolidation & Delivery

1. Mark checklists in `step-01-{slug}.plan.md` `[x]` where done.
2. Write `step-12-{slug}.result.md` (summary, files, telemetry).
3. **One delivery gate** ([`gates.md`](../shared/gates.md)):
   - Commit plan + result, keep artifacts (Recommended)
   - Commit plan + result, delete temps
   - Skip delivery commit
   - Pause
4. On commit: `docs({slug}): delivery plan and result`; self-learning MEMORY sweep.
5. **No push here.** Advance → Step 5.

### Step 5: Ship & PR

**One ship gate** ([`gates.md`](../shared/gates.md)):

1. **Create PR, monitor, and merge** (Recommended when `fullMode`)
2. **Push only**
3. **Skip shipping** (Recommended when not `fullMode`)
4. **Pause**

Dispatch `11-ship-pr` with `workflowMode: true`, `shipAction`, `workflowType: lite`. Never delete `project.workingBranch` after merge.

---

## AskQuestion / Auto-gate defaults

| Context | Index 0 |
|---------|---------|
| Transitions | Advance |
| Step 3 findings | Apply fixes once; if still failing → Proceed without fixing |
| Step 4 delivery | Commit plan + result, keep artifacts |
| Step 5 ship (`fullMode`) | Create PR, monitor, merge |
| Step 5 ship (not `fullMode`) | Skip shipping |

---

## State & dispatch

### Base Prompt Prefix (`Task` body)

```markdown
# Subagent — Step {STEP} — {Label}
Read state: `.cursor/plans/{slug}/{workflow-id}.state.md`
Skill: {SKILL.md path} — read full.
Orch: spec-to-pr-lite · model {currentModel} · {modeFlags} · workflowType: lite · workflowMode: true
Enhancing skills (mandatory): karpathy-guidelines, caveman, self-learning, gabarito
Read: state workflow memory + decisions; MEMORY.md index; `config.json.rules.stackFile` from `.agents/skills/shared/config.json`.
Config/SCM: `.agents/skills/shared/config-resolution.md`
Anchor: uswf/{workflow-id}/before-step-{STEP} @ {sha} · CWD: {repo-root}
Role: fresh; no resume. files_touched required. model: {currentModel}.
Rules: no `.cursor/plans/` in git-add except Step 4 G2-delivery; needs_user: ≥2 choices, recommended first.
End with ```step-output(...)```
```

---

## Triggers

```
@[spec-to-pr-lite] [auto|dry-run|skip-tests|full] [--model {name}] [--model-chain step:model,...] [US {issue_id} | {org}/{project}#{id} | {name}.spec.md]
/spec-to-pr-lite [flags] [US {issue_id} | {org}/{project}#{id} | {name}.spec.md]
```

For free-text brainstorm or full refine/DAG/verify/integration: use `/spec-to-pr` instead.
