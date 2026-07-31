# Shared Gate Contract — Dual-Mode

Canonical gate UX for [`ws-spec-to-pr`](../ws-spec-to-pr/SKILL.md) and [`ws-spec-to-pr-lite`](../ws-spec-to-pr-lite/SKILL.md).
Both orchestrators MUST follow this file so shared pipeline skills stay interchangeable.

Artifact paths: [`../ws-spec-to-pr/ARTIFACTS.md`](../ws-spec-to-pr/ARTIFACTS.md).
Config: [`.agents/skills/ws-shared/config.json`](config.json) only — see [`config-resolution.md`](config-resolution.md).

---

## Dual-mode rules (mandatory)

| Rule | Detail |
|------|--------|
| **Shared skills are workflow-agnostic** | Pipeline `ws-*` skills (`ws-write-spec`…`ws-fix-pr`, `ws-goal-fix-pr`, `ws-update-plan-implementation`), providers, `ws-goal-loop` never assume full vs lite step numbers. Orch passes mode, paths, and flags. |
| **`workflowType`** | `standard` (full) or `lite`. Resume filters by type — never cross-resume. |
| **One combined delivery + ship ask** | Orchestrator presents the combined gate once at standard Step 8 / lite Step 4. [`ws-ship-pr`](../ws-ship-pr/SKILL.md) in workflow mode **executes** the chosen option — does **not** re-ask at user-gate. Standalone `/ship-pr` may ask. |
| **Fix-PR is separate** | Standard Step 9 / lite Step 5 — **not** inside ship. `ws-ship-pr` receives `stopBeforeFixPr: true`. |
| **Artifact names** | Delivery result is `step-08-{slug}.result.md` for **both** workflows. Plan is `step-01-{slug}.plan.md`. |
| **Step ranges** | Standard: Steps 0–9. Lite: Steps 0–5. |
| **Config** | Only `.agents/skills/ws-shared/config.json`. No `ws-spec-to-pr/config.json` / `ws-spec-to-pr-lite/config.json`. |
| **User gates** | Prefer native structured choice UI when available; markdown fallback when not; HS-1 on cancel. |

---

## User gates (`user-gate`)

Portable alias: `user-gate`. **Native tool:** when the host exposes a structured multiple-choice tool (e.g. `AskQuestion`), **invoke it at every orchestration gate** — step transitions, entry/resume/config, refinement, G2-code, delivery+ship, fix-pr. Do not skip gates in normal mode.

1. Every normal-mode gate: **prefer** `AskQuestion` (or host equivalent) with ≥2 options; recommended first. Map to portable `user-gate` vocabulary in logs.
2. If `AskQuestion` / structured choice is unavailable or returns tool-not-found → present the **same options** as a short markdown list; wait for user reply. Log: `user-gate-fallback | {gate} | ISO`.
3. Cancelled / dismissed → **HS-1** (STOP; re-present; never infer yes).
4. `autoMode` → no user-gate prompt; use orch auto-gate table (index 0).

**Orchestrator obligation:** both [`ws-spec-to-pr`](../ws-spec-to-pr/SKILL.md) and [`ws-spec-to-pr-lite`](../ws-spec-to-pr-lite/SKILL.md) MUST call `AskQuestion` (when available) at **each** step boundary before advancing, replaying, refining, committing, shipping, or fix-pr — not only at entry or ship.

---

## Universal step controls (every step boundary)

Both workflows expose the same control vocabulary at **every** step transition. Primary menu stays slim; extended controls live under **More options…**.

**Banner (always, before options):**

````text
Current model: {currentModel}
To use a different model for the next step: Pause → switch model in IDE/agent host → resume workflow.
````

Resolve `{currentModel}` from the **executing session model** (agent identity / runtime). If unknown, use `unknown` and still show the Pause path. Log `model | step {N} | {name} | ISO`. On change vs prior state value, also log `model-change | step {N} | {old} → {new} | ISO`.

**Primary options (always shown):**

1. **Next** — Advance to Step N+1 (Recommended)
2. **More options…**

**Under More options…** (second user-gate only if user picked More):

| Control | Action |
|---------|--------|
| **Previous** | Go back to an earlier completed step (sub-menu by phase / step list) |
| **Replay** | Re-run the current step from its checkpoint |
| **Refine** | Alias → **Replay** (same behavior; preferred label when user wants iteration) |
| **Commit** | G2-code: commit code changes only (`src/` / `web/` / `tests/`) when the step produced them |
| **Undo** | Checkpoint revert to `uswf/{workflow-id}/before-step-{N}` for the current step |
| **Pause workflow** | Keeps all artifacts — after pause, switch model in IDE/agent host, then resume |
| Cancel without revert / Cancel and revert | HS-1 / revert per orch policy |

Do **not** offer Switch model / Choose model / concrete model-name menus. Model changes happen only via Pause → IDE/agent host model picker → Resume.

**Optional soft tip (standard orch only):** When advancing **into Step 6** (code review), add one hint line under the banner (no picker):

- `Hint: review ahead — consider a Reviewer/Thinking-class model (Pause → switch model in IDE/agent host → Resume).`

Log `model-hint | before-step-6 | current={currentModel} | ISO`. Lite: banner only (no phase soft tips).

Routine steps must not bury **Next** under five peers.

---

## Mode selection (entry)

After bootstrap / resume, when starting a **new** workflow (not resume), present once if the user did not pass an explicit density flag:

| Option | Effect |
|--------|--------|
| **Full pipeline** (ws-spec-to-pr default) | Steps 0–9 |
| **Fast (lite-like)** | Prefer `/ws-spec-to-pr-lite` or, inside full orch, complexity gate that skips 1–2–3 when eligible |
| **Auto** | `autoMode` — auto-gate index 0 |

If user invoked `/ws-spec-to-pr-lite`, skip Full vs Fast — already Fast. If `--full` / `auto` / `dry-run` already parsed, do not re-ask density.

---

## Complexity gate (full orch only)

Before Step 1, evaluate complexity (same spirit as Dynamic Execution):

| Class | Criteria | Path |
|-------|----------|------|
| **simple** | Docs-only, single-file text, no cascading side effects | Skip Steps 1–2–3; write stub `step-01-{slug}.plan.md` (goal + files + AC checklist); `execMode: sequential`; jump to Step 4 |
| **standard** | Normal feature | Steps 1 → conditional 2 → 3 → … |
| **complex** | Multi-domain, schema, tenancy, API surface | Enforce 1 + 2 + 3 |

Always write a real plan stub for **simple** (never blank plan reference). Log `complexity | simple|standard|complex | ISO`.

---

## Conditional interview (Step 2, full orch)

Skip Step 2 (mark skipped, log) when **all** hold:

- Complexity ≠ complex
- Plan Open Questions section empty or all marked resolved
- No `blocking` gaps from a 30s orch skim / prior step-output

Otherwise run `ws-interview`. Choosing **End refinement and advance** at 2c **auto-sets** `shared_understanding: confirmed` (skip separate 2e). Only show 2e when 2c was not used to exit.

---

## Check-implementation gate (standard Step 5)

Eval implemented code vs **refined spec when present, else `step-00-{slug}.spec.md`**. Publish integer **score 0–10** in Progress Board + `step-05-{slug}.plan.report.md`.

| Score | Behavior |
|-------|----------|
| ≥ 7 | Complete Step 5; **Next** → Step 6 |
| < 7 | User-gate: **Refine** (replay implement + re-check) / **Replan** (back to 1) / **Respec** (back to 0) / **Approve and continue** (log `check-approve-below-7`) |

`autoMode`: do **not** auto-approve below 7 — Pause with score (fail closed).

---

## Combined delivery + ship gate (one user-gate)

Replaces the old separate delivery (Step 12) and ship (Step 13) gates. Presented by the orchestrator at **standard Step 8** / **lite Step 4**:

1. **Commit plan + result, then create PR** (Recommended when `fullMode`)
2. **Commit plan + result, push only**
3. **Commit plan + result, skip PR**
4. **Skip delivery commit and skip shipping**
5. **Pause** (to change model: switch in IDE/agent host, then resume)

When `fullMode` is false, Recommended = **Skip delivery commit and skip shipping** (option 4) unless user explicitly wants push-only. When `fullMode` is true, Recommended = **Commit plan + result, then create PR** (option 1).

G2-delivery stages `step-02-{slug}.plan.refined.md` if present, else `step-01-{slug}.plan.md`, plus `step-08-{slug}.result.md` only.

MEMORY.md / ws-self-learning sweep runs automatically after a successful delivery commit (no separate §Doc gate).

Pass the selected ship intent into `ws-ship-pr` as `shipAction: create-pr|push-only|skip` with `workflowMode: true`, `stopBeforeFixPr: true`.

---

## Fix-PR gate (standard Step 9 / lite Step 5)

Separate from ship. After Step 8 / lite Step 4 when `shipAction: create-pr` and a PR exists:

1. Dispatch `ws-goal-fix-pr` (default loop) or `ws-fix-pr` (one-shot).
2. Merge policy per goal-fix / provider helpers.

Stop: max exhausted · merge blocked · cancelled · PR closed.

---

## Score & Refine gate (`scoreAndRefine`)

When `scoreAndRefine` mode is active (or triggered at bootstrap on completed workflows):

1. **Pass 1 Score Analysis Gate:** After scoring plan tasks against acceptance criteria (`step-05-{slug}.score-analysis.md`), present findings via `user-gate`:
   ```text
   Score Analysis Complete:
   - Overall Score: {score}/10
   - Tasks Flagged for Improvement: {N} tasks

   Options:
   1. Proceed with Second Pass Refinement (Recommended)
   2. Accept First Pass As-Is & Ship
   3. Selective Refinement (choose specific tasks)
   ```
2. **Second Pass Execution:** Re-run implementation for flagged tasks with Pass 1 scoring context, followed by 2nd pass verification and comparative reporting (`step-08-{slug}.second-pass-report.md`).
3. **Comparative Delivery Gate:** Final delivery gate comparing Pass 1 vs Pass 2 scores, LOC deltas, and test metrics before ship/commit.

---

## Safety gates to keep

| Gate | Where |
|------|-------|
| HS-1 / HS-2 / HS-2a | Both orch |
| G2-code | Full Steps 4 / 6 fix; lite after implement if committing mid-flow |
| G2-delivery | Inside combined delivery + ship gate above |
| Review findings | Lite Step 3; full Step 6 — fix → re-review until clean (max 3); Pause on residual Critical/Warning |
| Active Resume | `setup.md` |

---

## Auto-gate defaults (shared intent)

| Context | Index 0 |
|---------|---------|
| Transition | Next (Advance) |
| Combined delivery + ship (`fullMode`) | Commit plan + result, then create PR |
| Combined delivery + ship (not `fullMode`) | Skip delivery commit and skip shipping |
| Completed workflow bootstrap | Run Score & Second Pass (score-and-refine) |
| Score Analysis gate (`scoreAndRefine`) | Proceed with Second Pass Refinement |
| Check-implementation < 7 | Pause (no auto-approve) |
| Review findings (full Step 6 / lite Step 3) | Autofix → re-review (max 3); Pause on residual Critical/Warning |
| Testing plan (full Step 7) | Approve without browser (or skip if `skipTesting`) |

---

## Flags

| Flag | Meaning |
|------|---------|
| `skipTesting` | Skip Step 7 Testing (auto-skip when no test surface + unit tests green) |
| `scoreAndRefine` | Enable Pass 1 Task Scoring & 2nd Pass Refinement loop (aliases: `analyze-second-pass`, `score-refine`) |

