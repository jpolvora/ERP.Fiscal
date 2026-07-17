# Spec-to-PR

> **Human audience.** Orchestrator FSM lives in [`SKILL.md`](SKILL.md) — English agent contract; do not use it for onboarding. Use this README + FAQ + diagrams.
>
> **v10.0:** English-only output. Native tools (`Task`, `AskQuestion`, `Shell`, MCP). Tools via [`tools.md`](../shared/tools.md). Config via [`config.json`](../shared/config.json) (repo-root: `.agents/skills/shared/config.json`). Project-agnostic — skills detect stack from config. Steps delegate skills `00`–`07`, `09`, `11`. Step 0 Spec Creation. `--full` flag activates Step 13 (Ship & PR). Per-step model recording.
>
> **Identity:** Primary invoke `/spec-to-pr` / `@[spec-to-pr]`. Legacy aliases: `/us-workflow`, `/us-delivery-workflow`. Runtime git tags still use `uswf/`; plan slugs still use `us-{id}`.

End-to-end Spec → PR pipeline using **orchestrator + sub-agents** with clean context, shared state, and confirmation gates (including LLM model switching).

The **`spec-to-pr`** workflow coordinates execution steps through composable skills. Each step runs in isolated sub-agent contexts, consuming and updating shared state (`state.md` and `MEMORY.md`). Each step receives input and produces precise output, allowing subsequent steps to reuse acquired knowledge and accumulated decisions.

## Core Goals
1. **End-to-End Delivery:** Automate the complete feature/US lifecycle from specification to PR/Merge (steps 0 to 13).
2. **Context Isolation & State Hygiene:** Execute each step in a clean, isolated Task with step-specific worktrees, maintaining shared state integrity (`state.md` + `MEMORY.md`).
3. **Safety & Gates:** Require explicit transition gates and model readiness checks before coding and review phases to prevent accidental commits.
4. **Portability:** Keep the orchestration FSM stack-agnostic and configuration-driven, resolving all project metadata and commands dynamically from `config.json` and `stack.md`.

| Document | Audience | Content |
|----------|----------|---------|
| **This README** | Humans + agents | Overview, phases, steps, gates, happy path |
| [`docs/faq.md`](docs/faq.md) | Humans + clients | FAQ in execution order — what each step does, input/output, common questions |
| [`SKILL.md`](SKILL.md) | **Agent (FSM)** | English agent contract; no plan-dir commits until Step 12; `{slug}.result.md` delivery |
| [`stack.md`](../shared/stack.md) | Orchestrator + sub-agents | Stack definition — build/test commands, paths, rules, diff scope |
| [`DIAGRAM.md`](DIAGRAM.md) | Visual reference | Mermaid flowcharts |

**Project entry:** [`AGENTS.md`](../../../AGENTS.md).

---

## Phases

| Phase | Name | Steps | Executor |
|-------|------|-------|----------|
| **F0** | Bootstrap | 0 | Orchestrator |
| **F1** | Specification | 1, 2, 3 | Sub-agent (Planner) |
| **F2** | Implementation | 4†, 5 | Sub-agent (Coder) |
| **F3** | Verification + 1st commit | 6, 7 | Sub-agent (Verifier) + Orchestrator + shell |
| **F4** | Review + fixes | 8†, 9, 10 | Sub-agent (Reviewer + Coder) |
| **F5** | Pre-PR integration | 11 | Sub-agent (Verifier) + optional browser |
| **F6** | Closure | 12, 13 | Orchestrator + shell (+ ship sub-agent when `--full`) |

† Steps **4 and 8** are **model sub-gates** (F1→F2 and F3→F4), never in `completedSteps`.

### Happy path

```text
/spec-to-pr 2416
  → F0: bootstrap, issue fetch (gh), state, gate → F1
  → F1: plan (1) → refinement FSM if blocking (2) → DAG (3) → model sub-gate → F2
  → F2: implement DAG on branch (worktree if stable, else branch-direct) → verify files/build → gate → F3
  → F3: readonly report (6) → explicit G2 commit gate (7) → model sub-gate → F4
  → F4: scoped diff review (9) → gate → fix sub-agent Coder (10) → G2 commit gate → F5
  → F5: integration plan → gate → test battery (+ browser if approved) → F6
  → F6: one delivery gate → optional Step 13 ship gate → completed
```

Pause at any gate → "Pause workflow" → state saved; resume with `/spec-to-pr 2416`.

---

## How to start

```text
@[spec-to-pr] 2338
@[spec-to-pr] contoso/MyProject#2338
@[spec-to-pr] ADO 2338
@[spec-to-pr] specs/my-feature.spec.md
@[spec-to-pr] dry-run 2338
@[spec-to-pr] auto 2338
@[spec-to-pr] auto dry-run 2338
@[spec-to-pr] auto skip-integration 2338
@[spec-to-pr] auto skip-tests skip-integration 2338
@[spec-to-pr] soft-delete for suppliers
@[spec-to-pr] --model sonnet-4 auto US 567
@[spec-to-pr] --model-chain 5:sonnet-4,9:gemini-3-pro,10:sonnet-4 US 123
```

Persistent state: `.cursor/plans/us-{id}/{workflow-id}.state.md` (fields `dryRun`, `autoMode`, `skipIntegration`, `skipTests`, `fullMode`). **Everything** workflow lives under `.cursor/plans/us-{id}/` — nothing written to `.agents/`.

On **normal mode** start, the workflow checks for existing active state in `.cursor/plans/*/*.state.md`. If found, presents a menu to resume, start new, or cancel.

### Auto mode (`auto`)

Pipeline **without interactive menus**: the orchestrator always picks the **recommended option** at each gate and dispatches the next step in the same turn.

- **Resumes** only an active `autoMode: true` workflow for the same US (continues from `currentStep`).
- **Ignores** other active workflows (any US/mode) — starts fresh if no active auto for that US.
- **Combines with dry-run** → full simulation without commits or code edits; **browser always skipped** at Step 11.
- **Hard stop** on unrecoverable failures (3 retries, build/test exhausted) — pauses and warns; re-invoke `auto US {id}` resumes.

Prefix on messages: `[AUTO]` (and `[DRY-RUN]` if applicable).

In **auto** and/or **dry-run**, each step displays required banners:

```text
**Starting step 5 Implementation**
…
**Finished step 5 Implementation**
```

### Dry-run (simulation)

Simulates the full flow (gates, plans, exec, verify, review, **integration validation**) **without**:

- commits or push
- editing `src/` / `web/` / `tests/` (steps 5, 10, and 11 fixes)
- browser automation / data seed
- worktrees
- changes to `MEMORY.md` (root)

Useful for validating plan and DAG before implementing. Details in [`SKILL.md`](SKILL.md).

### Skip flags (`skip-integration` / `skip-tests`)

Independent flags, combinable with `auto` and `dry-run`, in any order:

- **`skip-integration`** → `skipIntegration: true`: skips **Step 11 entirely** — no integration test plan, no battery, no browser. Marks `11` in `skippedSteps`/`completedSteps`, logs in `## Gate history`, advances to Step 12.
- **`skip-tests`** → `skipTests: true`: skips **test suite execution** in the Build & Test Validation Protocol (Steps 7 and 10) and Step 11 §3. **Build still runs** — commit never happens with broken build. `verification.tests: skipped`; warns user once.

### Full mode (`--full`)

Activates Step 13 (Ship & PR): push → create PR → goal-fix-pr monitoring loop → merge. Default: off.

### Model flags

| Flag | Effect |
|------|--------|
| `--model {name}` | Set `currentModel` at workflow start. Overrides default. |
| `--model-chain "{step}:{model},{step}:{model}"` | Pre-specify per-step models. Only way to switch models in auto mode. Overrides `--model` at matching steps. |

---

## Steps

Canonical filenames: [`ARTIFACTS.md`](ARTIFACTS.md).


Each step ends with a **git checkpoint** and a **slim Transition Gate** (Advance / More…). Phase model hints fold into Advance at F1→F2 and F3→F4. Shared gate contract: [`gates.md`](../shared/gates.md). Dual-mode with [`spec-to-pr-lite`](../spec-to-pr-lite/SKILL.md).

| # | Name | Who executes | Objective |
|---|------|--------------|-----------|
| **0** | Bootstrap | Orchestrator | State, issue snapshot, MEMORY context |
| **1** | Plan | Sub-agent | `step-01-{slug}.plan.md` (Conditional — skipped if Dynamic Execution active) |
| **2** | Refinement | Sub-agent | Plan grilling (Conditional — skipped if Dynamic Execution active) |
| **3** | Exec + DAG | Sub-agent | `*.plan.exec.md` + `*.exec.dag.json` + memory-conflict |
| **4†** | (internal) Coder phase hint | On Advance 3→5 | No dedicated menu |
| **5** | Implement | Sub-agent(s) Coder | Code per DAG level (parallel up to 3) + learning |
| **6** | Verify | Sub-agent (readonly) | Quick-score default; full matrix if score < 7 or `--strict` |
| **7** | Decide + commit | Orchestrator + sub-agent + shell | G2 gate; may trigger validation/fix before commit, or return to Step 5 |
| **8†** | (internal) Reviewer phase hint | On Advance 7→9 | No dedicated menu |
| **9** | Code review | Sub-agent | Critical / Warning (scoped diff vs base branch) |
| **10** | Fix + close | Sub-agent + shell | 2nd commit + `step-10-{slug}.report.md` + learning |
| **11** | **Integration validation** | Sub-agent + browser + shell | Skip when no API/UI + tests green, or `skip-integration` |
| **12** | Consolidation & Delivery | Orchestrator + shell | **One** delivery gate (plan + result); no push |
| **13** | Ship & PR | Sub-agent + shell | **One** ship gate → push, PR, goal-fix-pr, merge |

### Step 11 — integration validation (summary)

Before executing integration tests and opening a PR (manual), the workflow:

1. Runs the **Integration Validation Protocol** and generates `step-11-{slug}.integration-test.plan.md`.
2. Shows the plan (or summary) and asks whether to continue or skip (auto mode skips browser).
3. If **approve and execute** (normal mode): build, tests, seed, API/permissions — **browser only if not auto nor dry-run**.
4. In **auto or dry-run:** **§6 browser always skipped** — report marks UI ACs as `⏭ skipped`; validate UI manually before PR.
5. **Skip validation:** advances directly to Step 12.
6. Failures: fixes → revalidate (up to 3 iterations).

### Golden rule

After confirming in the Transition Gate, the orchestrator **dispatches the next step in the same turn**. Steps 4 and 8 embed model switching in the advance gate (Step 3→5 and Step 7→9).

---

## Git checkpoints

Local tags **never pushed**:

```text
uswf/{workflow-id}/before-step-1   # baseline (Step 0)
uswf/{workflow-id}/before-step-2   # before Step 1 mutates
…
uswf/{workflow-id}/before-step-13  # after Step 12
```

- Created after each step completes; mirrored in `state.md` → `checkpoints[]`.
- **Backward Navigation** and **Repeat Step N** use the **Checkpoint Revert Algorithm** anchored on the target tag.
- Removed at Step 12 (or full reset); commits go in push, tags do not.

---

## Gates — next / repeat / previous / skip

### Transition Gate (after each step)

| Action | Menu option |
|--------|-------------|
| **Next** | Advance to Step N+1 |
| **Repeat** | Repeat Step N (partial revert if `files_touched` exists) |
| **Previous** | Go back to earlier step — sub-menu by phase (Planning / Implementation / Review / Validation) |
| **Pause** | Pause / cancel without revert / cancel and revert all |

**Step 11 — skip:** **Skip validation** in the test plan confirmation gate (advances directly to Step 12).

**Step 2 — refinement:** one question per round; **End refinement and advance** applies defaults and leads to **Shared Understanding** gate; Step 3 only after **I confirm shared understanding**.

### Backward Navigation (Previous)

Returns to **any completed step** (1–3, 5–7, 9–11), not just the current one:

1. Choose phase → target step `M`
2. Preview: *Will be undone* (Steps M–N) vs *Will be preserved*
3. Confirm → Checkpoint Revert + immediate redispatch of Step M

Shortcut: Step 7 **Re-implement with different Coder model** = return to Step 5.

---

## Internal protocols (SKILL.md)

| Protocol | Used in | Function |
|----------|---------|----------|
| **Authorization Ladder** | all | G0–G3 gates enforced + hard stops HS-1..5 (no commit/push without gate) |
| **Transition Discipline** | transitions | Single rule: normal dispatches after gate; auto dispatches same turn |
| **Refinement FSM** | 2 | Audit → Resolve → Escalate → Exit; registry persisted; blocking/non-blocking |
| **Dynamic Execution (Simplicity First)** | 1, 2 | Orchestrator evaluates complexity — bypasses planning/refinement for surgical changes |
| **Worktree Fallback** | 5, 10, 11 | branch-direct on Windows/long path + post-step verification |
| **State Hygiene** | all | Mandatory state sync post-step + asserts (`validate_state.py`) |
| **Learning & Memory** | all (start + end + Step 12) | Read and update state (`state.md`) and `MEMORY.md` to reuse technical learnings and avoid repeating errors |
| **Context loading** | 1, 2, 5 | Docs, rules, domain glossary |
| **Specification** | 0 (fetch/resolve), 1–2/6/11 (read) | Entry: **US id**, ADO id, or **`*.spec.md`**. Resolve `providers.active` → provider skill `fetch-to-spec` → **`step-00-{slug}.spec.md`** (canonical). Downstream skills read **spec**, not raw tracker JSON |
| **Memory-conflict** | 2, 3 | Python script vs `MEMORY.md` (root) |
| **Integration validation** | 11 | Plan + browser/API/seed execution |
| **Step checkpoint** | 0–12 | Git tags `uswf/{id}/before-step-{N}` |
| **Step dispatch** | 1–11 | Dedicated sub-agent + anchor tag + step-scoped worktree (5/10/11) |
| **Checkpoint revert** | reset / previous / repeat | Scoped revert via `## Step file log` |

Orchestrator scripts: `.agents/skills/spec-to-pr/scripts/check_memory_conflict.py`, `validate_state.py`. Issue/WI converters: canonical under `github-provider/scripts/` and `azure-devops-provider/scripts/` (shims under `spec-to-pr/scripts/`).

---

## Documentation consolidation (§Doc)

§Doc runs as a **silent log** during steps (no per-step AskQuestion). Step 12 delivery commit triggers the final MEMORY.md / self-learning sweep automatically.

---

## Parallelization (DAG)

- Step 3 generates `us-{id}.exec.dag.json` with `levels` and `parallelGroup`.
- Step 5 executes **level by level**; within a level, up to **3** sub-agents in parallel.
- **Isolation:** one git worktree **per code step** (5, 10, 11) — not per DAG task. See **Step Dispatch & Isolation Protocol** in `SKILL.md`.

Example levels: `[T1] → [T2, T3] → [T4]`

## Per-step isolation

| Step | Sub-agent | Worktree | Anchor tag |
|------|-----------|----------|------------|
| 1–3, 6, 9 | `generalPurpose` | — (repo root) | `before-step-{N}` |
| 5, 10, 11 | `generalPurpose` | `.cursor/plans/us-{id}/worktrees/step-{N}/` | `before-step-{N}` |
| 7, 12 (shell) | `shell` | — | — |
| All | **fresh Task** — never `resume` between steps | max. 1 active worktree | local checkpoint |

---

## Main artifacts

Everything under `.cursor/plans/{slug}/` (one per feature/US). `MEMORY.md` is shared at root.

| Artifact | Path |
|----------|------|
| State | `.cursor/plans/{slug}/{workflow-id}.state.md` |
| **Spec (canonical)** | `.cursor/plans/{slug}/step-00-{slug}.spec.md` |
| GitHub issue (audit, optional) | `.cursor/plans/{slug}/step-00-{slug}.issue.json` |
| Plan (initial draft) | `.cursor/plans/{slug}/step-01-{slug}.plan.md` |
| Plan (refined ok) | `.cursor/plans/{slug}/step-02-{slug}.plan.refined.md` |
| Exec | `.cursor/plans/{slug}/step-03-{slug}.plan.exec.md` |
| DAG | `.cursor/plans/{slug}/step-03-{slug}.exec.dag.json` |
| Verification | `.cursor/plans/{slug}/step-06-{slug}.plan.report.md` |
| Delivery | `.cursor/plans/{slug}/step-10-{slug}.report.md` |
| Integration test plan | `.cursor/plans/{slug}/step-11-{slug}.integration-test.plan.md` |
| Integration test report | `.cursor/plans/{slug}/step-11-{slug}.integration-test.report.md` |
| Delivery result | `.cursor/plans/{slug}/step-12-{slug}.result.md` |
| Worktrees (git-ignored) | `.cursor/plans/{slug}/worktrees/step-{N}/` |
| Baseline (git-ignored) | `.cursor/plans/{slug}/{workflow-id}.baseline/` |
| Technical memory (root, shared) | `MEMORY.md` |

---

## Out of scope

- Opening/updating Pull Request (without `--full` flag) — manual after Step 12
- Push without explicit consent in Step 12 gate

---

## Portability

The `spec-to-pr` skill is designed to be fully **generic and portable**. All configuration, metadata, file paths, and commands specific to a project must be defined in `config.json` or `stack.md`. Never add hardcoded paths or project-specific commands directly in the skill instructions.
