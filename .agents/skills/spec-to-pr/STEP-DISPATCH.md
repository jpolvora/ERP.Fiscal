# Step dispatch (canonical)

**Sole source of truth** for **`spec-to-pr` (standard)** step 0–13 dispatch actions, post-mutating merge notes, and Step 12/13 gate protocols. Load from `SKILL.md` only when advancing or dispatching a step. FSM, invariants, and gates overview stay in `SKILL.md`.

**Dual-mode (mandatory):** This file is **not** the lite step index. [`spec-to-pr-lite`](../spec-to-pr-lite/SKILL.md) keeps its own Steps 1–5 table. Shared gate/delivery/ship UX and artifact names stay in [`gates.md`](../shared/gates.md) / [`config-resolution.md`](../shared/config-resolution.md). Pipeline skills `00`–`11` stay orch-agnostic: never assume full vs lite step numbers; orch passes `workflowType`, paths, and flags.

## Step instructions

> **Consistency:** the Skill map in `SKILL.md` (`05-verify-plan` → Step 6, etc.) is authoritative. Keep this table aligned — never dispatch retired ids (`05-verify-sync-plan-us`, `implement-plan`, `plan-us`, …).

| Step | Action | Artifact |
|------|--------|----------|
| 0 | Entry gate (AskQuestion). US/spec provided → skip to Step 1. No args → free-text → `Task` `00-write-spec`. | `step-00-{slug}.spec.md` |
| 1 | Complexity gate → if simple: stub plan + skip to 5. Else `Task` `01-write-plan`. | `step-01-{slug}.plan.md` |
| 2 | Conditional: skip if eligible; else `Task` `02-interview`; 2c End auto-confirms 2e | `step-02-{slug}.plan.refined.md` |
| 3 | `Task` `03-plan-to-tasks`; sequential → skip empty DAG artifacts (log only). Parallel → DAG. | `step-03-*` when parallel |
| 4† | (internal) phase soft tip on Advance to 5 — no menu | not in completedSteps |
| 5 | `Task` `04-implement-tasks` mode build; branch-direct default | verification |
| 6 | `Task` `05-verify-plan` **quick-score default**; full US matrix if score < 7 or `--strict` | `step-06-{slug}.plan.report.md` |
| 7 | AskQuestion G2-code → Shell build/test → `git commit` code | commit; no `.cursor/plans/` |
| 8† | (internal) phase soft tip on Advance to 9 — no menu | not in completedSteps |
| 9 | `Task` `06-code-review`; findings gate if Critical/Warning | score |
| 10 | `Task` `04-implement-tasks` mode fix; G2-code only | `step-10-{slug}.report.md` |
| 11 | Auto-skip if `skipIntegration` or (no API/UI surface + unit tests green); else `Task` `07-integration-validation` | reports |
| 12 | Delivery Result + **one delivery gate** ([`gates.md`](../shared/gates.md)). MEMORY sweep after commit. No push. `status: completed` unless advancing to 13. | `step-12-{slug}.result.md` |
| 13 | **One ship gate** → pass `shipAction` to `11-ship-pr` (`workflowMode: true`). Always offered; `fullMode` changes Recommended. | PR URL, merge |

Post-mutating: merge files_touched → Step file log; backup preExistingDirty; checkpoint `before-step-{N+1}`.

### Step 12 — Delivery (one gate)

**Order:** [`protocols/delivery-result.md`](protocols/delivery-result.md) → **one delivery AskQuestion** → on commit: MEMORY sweep → optional temp delete per [`protocols/artifact-cleanup.md`](protocols/artifact-cleanup.md).

**Delivery AskQuestion** ([`gates.md`](../shared/gates.md)):

1. **Commit plan and result, keep artifacts** (Recommended)
2. **Commit plan and result, delete temps**
3. **Skip delivery commit**
4. **Pause**

G2-delivery stages `step-01-{slug}.plan.md` (or refined) + `step-12-{slug}.result.md` only. **No push consent at Step 12.**

### Step 13 — Ship & PR

After Step 12, orch presents the **single ship gate** ([`gates.md`](../shared/gates.md)). Recommended = Create PR… when `fullMode`, else Skip.

**Pipeline (`shipAction: create-pr`):**
1. `git push -u origin {branch}` (skip if pushed).
2. Resolve `providers.scm` via [`config-resolution.md`](../shared/config-resolution.md).
3. Dispatch `11-ship-pr` with `workflowMode: true`, `shipAction`, `workflowType` from state (`standard` here; lite orch passes `lite`) — **no re-AskQuestion inside skill**.
4. goal-fix-pr loop (heartbeat configurable; default 5m, max 10) → merge.

**`shipAction: push-only`:** push only. **`skip`:** done.

Stop: max exhausted · merge blocked · cancelled · PR closed.
