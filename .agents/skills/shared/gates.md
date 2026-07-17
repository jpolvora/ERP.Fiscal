# Shared Gate Contract — Dual-Mode

Canonical gate UX for [`spec-to-pr`](../spec-to-pr/SKILL.md) and [`spec-to-pr-lite`](../spec-to-pr-lite/SKILL.md).
Both orchestrators MUST follow this file so shared pipeline skills stay interchangeable.

Artifact paths: [`../spec-to-pr/ARTIFACTS.md`](../spec-to-pr/ARTIFACTS.md).
Config: [`.agents/skills/shared/config.json`](config.json) only — see [`config-resolution.md`](config-resolution.md).

---

## Dual-mode rules (mandatory)

| Rule | Detail |
|------|--------|
| **Shared skills are workflow-agnostic** | Skills `00`–`11`, providers, `goal-loop` never assume full vs lite step numbers. Orch passes mode, paths, and flags. |
| **`workflowType`** | `standard` (full) or `lite`. Resume filters by type — never cross-resume. |
| **One ship ask** | Orchestrator presents the ship gate once. [`11-ship-pr`](../11-ship-pr/SKILL.md) in workflow mode **executes** the chosen option — does **not** re-AskQuestion. Standalone `/ship-pr` may ask. |
| **Artifact names** | Delivery result stays `step-12-{slug}.result.md` for **both** workflows (lite Step 4 writes the same filename). Plan is `step-01-{slug}.plan.md`. |
| **Config** | Only `.agents/skills/shared/config.json`. No `spec-to-pr/config.json` / `spec-to-pr-lite/config.json`. |
| **AskQuestion** | Prefer native tool when available; markdown fallback when not; HS-1 on cancel. |

---

## AskQuestion (compact)

1. Every normal-mode gate: **prefer** `AskQuestion` with ≥2 options; recommended first.
2. If AskQuestion is unavailable or returns tool-not-found → present the **same options** as a short markdown list; wait for user reply. Optional log: `askquestion-fallback | {gate} | ISO`.
3. Cancelled / dismissed → **HS-1** (STOP; re-present; never infer yes).
4. `autoMode` → no AskQuestion; use orch auto-gate table (index 0).

---

## Default transition menu (slim)

**Banner (always, before options):**

````text
Current model: {currentModel}
To use a different model for the next step: Pause → switch model in Cursor → resume workflow.
````

Resolve `{currentModel}` from the **executing session model** (agent identity / runtime). If unknown, use `unknown` and still show the Pause path. Log `model | step {N} | {name} | ISO`. On change vs prior state value, also log `model-change | step {N} | {old} → {new} | ISO`.

**Primary options (always shown):**

1. **Advance to Step N+1** (Recommended)
2. **More options…**

**Under More options…** (second AskQuestion only if user picked More):

- Repeat current step
- Go back to earlier step (full FSM only; lite: Pause instead if no backward nav)
- **Pause workflow** (keeps all artifacts) — after pause, switch model in Cursor, then resume the workflow; orch re-reads the session model
- Cancel without revert / Cancel and revert

Do **not** offer Switch model / Choose model / concrete model-name menus. Model changes happen only via Pause → Cursor model picker → Resume.

**Phase soft tips (full orch only):** When the next step crosses F1→F2 (after Step 3, before Step 5) or F3→F4 (after Step 7, before Step 9), add one hint line under the banner (no picker):

- F1→F2: `Hint: implementation ahead — consider a Coder-class model (Pause → switch → Resume).`
- F3→F4: `Hint: review ahead — consider a Reviewer/Thinking-class model (Pause → switch → Resume).`

Log `model-hint | F1→F2|F3→F4 | current={currentModel} | ISO`. Lite: banner only (no phase soft tips). See full orch Model readiness for tags `before-step-5` / `before-step-9` (telemetry only; not user menus).

Routine steps must not bury Advance under five peers.

---

## Mode selection (entry)

After bootstrap / resume, when starting a **new** workflow (not resume), present once if the user did not pass an explicit density flag:

| Option | Effect |
|--------|--------|
| **Full pipeline** (spec-to-pr default) | Steps 0–12; Step 13 only if `fullMode` / `--full` |
| **Fast (lite-like)** | Prefer `/spec-to-pr-lite` or, inside full orch, complexity gate that skips 1–2–3 when eligible |
| **Auto** | `autoMode` — auto-gate index 0 |

If user invoked `/spec-to-pr-lite`, skip Full vs Fast — already Fast. If `--full` / `auto` / `dry-run` already parsed, do not re-ask density.

---

## Complexity gate (full orch only)

Before Step 1, evaluate complexity (same spirit as Dynamic Execution):

| Class | Criteria | Path |
|-------|----------|------|
| **simple** | Docs-only, single-file text, no cascading side effects | Skip Steps 1–2–3; write stub `step-01-{slug}.plan.md` (goal + files + AC checklist); `execMode: sequential`; jump to Step 5 |
| **standard** | Normal feature | Steps 1 → conditional 2 → 3 → … |
| **complex** | Multi-domain, schema, tenancy, API surface | Enforce 1 + 2 + 3 |

Always write a real plan stub for **simple** (never blank plan reference). Log `complexity | simple|standard|complex | ISO`.

---

## Conditional interview (Step 2, full orch)

Skip Step 2 (mark skipped, log) when **all** hold:

- Complexity ≠ complex
- Plan Open Questions section empty or all marked resolved
- No `blocking` gaps from a 30s orch skim / prior step-output

Otherwise run `02-interview`. Choosing **End refinement and advance** at 2c **auto-sets** `shared_understanding: confirmed` (skip separate 2e). Only show 2e when 2c was not used to exit.

---

## Delivery gate (one AskQuestion)

Replaces multi-gate Step 12 / lite Step 4 cluster:

1. **Commit plan + result, keep artifacts** (Recommended)
2. **Commit plan + result, delete temps**
3. **Skip delivery commit**
4. **Pause** (to change model: switch in Cursor, then resume)

MEMORY.md / self-learning sweep runs automatically after a successful delivery commit (no separate §Doc gate). Push is **not** asked here when the next step is Ship.

---

## Ship gate (one AskQuestion)

Presented by the orchestrator only (full Step 13 / lite Step 5):

1. **Create PR, monitor, and merge** (Recommended when `fullMode`)
2. **Push only**
3. **Skip shipping**
4. **Pause** (to change model: switch in Cursor, then resume)

When `fullMode` is false, Recommended = **Skip shipping**. When `fullMode` is true, Recommended = **Create PR…**. Never ask push at delivery if Ship is next.

Pass the selected option into `11-ship-pr` as `shipAction: create-pr|push-only|skip`.

---

## Safety gates to keep

| Gate | Where |
|------|-------|
| HS-1 / HS-2 / HS-2a | Both orch |
| G2-code | Full Steps 7 / 10 / 11 fix; lite after implement if committing mid-flow |
| G2-delivery | Inside delivery gate above |
| Review findings | Lite Step 3; full after Step 9 when findings |
| Active Resume | `setup.md` |

---

## Auto-gate defaults (shared intent)

| Context | Index 0 |
|---------|---------|
| Transition | Advance |
| Delivery | Commit plan + result, keep artifacts |
| Ship (`fullMode`) | Create PR, monitor, merge |
| Ship (not `fullMode`) | Skip shipping |
| Review findings (lite) | Apply fixes once, else Proceed without fixing |
| Integration plan (full) | Approve without browser (or skip if `skipIntegration`) |
