---















name: ws-spec-to-pr
version: 0.0.109
description: End-to-end Spec-to-PR delivery orchestrator FSM (Steps 0–9). Coordinates planning, DAG execution, verification, review loops, testing, and SCM PR delivery with subagent isolation.

invocation_names:
  - spec-to-pr
  - ws-spec-to-pr
---

## Audience & load

| Audience | Doc |
|----------|-----|
| **Orchestrator (this file)** | FSM + tool bindings + asserts |
| **Humans** | [`README.md`](README.md), [`docs/faq.md`](docs/faq.md), [`DIAGRAM.md`](DIAGRAM.md) |

**Always-on load:** this file + current step skill. **On demand:** [`setup.md`](../ws-shared/setup.md) · [`gates.md`](../ws-shared/gates.md) · [`config-resolution.md`](../ws-shared/config-resolution.md) · [`tools.md`](../ws-shared/tools.md) · [`ARTIFACTS.md`](ARTIFACTS.md) · [`STEP-DISPATCH.md`](STEP-DISPATCH.md) (when advancing) · [`PROTOCOLS.md`](PROTOCOLS.md) (gates, auth ladder, step protocols, state YAML, errors) · [`protocols/`](protocols/) · stack file (steps 4/6/7) · [`ws-shared/AGENTS.md`](../ws-shared/AGENTS.md). Dual-mode with [`ws-spec-to-pr-lite`](../ws-spec-to-pr-lite/SKILL.md): shared skills stay interchangeable. Language: **en-us** only.

## Native tool contract

Canonical aliases: [`tools.md`](../ws-shared/tools.md). Params: `{sharedDir}/config.json`. Never narrate undone work. Orch never edits code — `dispatch-agent` only.

| Intent | Alias | Rule |
|--------|-------|------|
| Step work | `dispatch-agent` | `generalPurpose`\|`shell`; `description: "STP step {N} — {Label}"`; readonly step 5; no resume across steps; step 4 DAG ≤3 parallel |
| User gate | `user-gate` / `user-gate-auto` | **At every step boundary:** prefer `AskQuestion` when available; else markdown fallback [`gates.md`](../ws-shared/gates.md); ≥2 options; cancelled → HS-1; auto → index 0 |
| Build/test / SCM | `Shell` | `config.json.verification`; cite real `gh`/`git` output |
| State | `read-state` / `write-state` | Hygiene before board |
| Browser (7) | `browser-mcp` | Normal, non-dry-run, non-skip, gated |

Subagents end with parseable `step-output`. User output: summaries + Progress Board + banners. Gate contexts: transitions, entry/resume/config, refinement 2c, G2-code, Step 8 delivery+ship, Step 9 fix-pr.

# Spec-to-PR — Orchestrator

Deterministic FSM; step content via **`dispatch-agent`**.

## Goals

1. Spec → PR + thread resolution (steps 0–9).
2. Isolated subagents + valid `state.md` / MEMORY hygiene.
3. Explicit gates before code, fix, test, ship.
4. Portable: all project metadata from `config.json` / stack file.
5. Optional `config.json.fable.enabled` → [`ws-fable-domain`](../ws-fable-domain/SKILL.md) (Step 1), [`ws-fable-judge`](../ws-fable-judge/SKILL.md) (Steps 5/6/8).

## Invariants

| Topic | Rule |
|-------|------|
| Scope | 0–7 local; 8 = delivery+ship; 9 = fix-pr after PR. No push before Step 8 ship action. |
| Auth | G1+ needs gate. Cancel → HS-1. Commit → G2 + menu (HS-2). |
| Isolation | Fresh `dispatch-agent`/step; checkpoint tag `uswf/{id}/before-step-{N}`; branch-direct default; worktree when `plans.useWorktrees=true`. |
| State / Memory | Hygiene → asserts → board (fail → HS-5). `state.md` short-term; `{sharedDir}/MEMORY.md` generalizable. |
| Dual-mode | `workflowType: standard`. Shared skills interchangeable with lite. |
| `dryRun` | No src/web writes, commit, push, worktree, browser, MEMORY mutate. Prefix `[DRY-RUN]`. |
| `autoMode` | Auto-gate 0; `[AUTO]`; HS-3/4/5 pause; no browser; telemetry still mandatory. |
| `skipTesting` / `skipTests` | Skip Step 7 vs skip test suites (build required). |
| `fullMode` | Step 8 Recommended = commit plan+result then create PR. |
| `scoreAndRefine` | Pass 1 task scoring + 2nd pass refinement loop (`[SCORE-REFINE]`). |
| `skipQualityGates` | `--skip-gates` or `invariants.skipQualityGates`; `[GATES BYPASSED]` banner; quality gates only — see § Quality gate bypass. |
| Artifacts | **Never commit `{plansDir}/` in Steps 0–7.** Delivery commit Step 8: plan + `step-08-{slug}.result.md` only. |
| Pause / model | Pause keeps artifacts (`status: active`). `currentModel` = session; switch via Pause → host → Resume. |
| Revert | Manifest + checkpoint only — no global hard reset. |

Runtime tokens: `uswf/` tags/worktrees; slugs `us-{id}`.

## Deps (pointers)

| Need | Load |
|------|------|
| Filenames / `{us-dir}` | [`ARTIFACTS.md`](ARTIFACTS.md) |
| Bootstrap / flags / resume | [`setup.md`](../ws-shared/setup.md) |
| Providers | [`ws-github-provider`](../ws-github-provider/SKILL.md) · [`ws-azure-devops-provider`](../ws-azure-devops-provider/SKILL.md) · [`ws-local-spec-provider`](../ws-local-spec-provider/SKILL.md) |
| Step map | `ws-write-spec`→0 … `ws-ship-pr`→8 · `ws-fix-pr`/`ws-goal-fix-pr`→9 · `ws-update-plan-implementation` Post |
| Auth ladder, step protocols, state YAML, errors | [`PROTOCOLS.md`](PROTOCOLS.md) |
| Dispatch bodies | [`STEP-DISPATCH.md`](STEP-DISPATCH.md) |
| Pipeline classify | [`ws-classify-complexity`](../ws-classify-complexity/SKILL.md) — Step 0 after spec, before Step 1 |

## Phases F0–F6 ↔ steps 0–9

```mermaid
flowchart LR
  F0[F0 Bootstrap] --> F1[F1 Planning]
  F1 --> F2[F2 Implementation]
  F2 --> F3[F3 Check-implementation]
  F3 --> F4[F4 Review + Fix]
  F4 --> F5[F5 Testing]
  F5 --> F6[F6 Ship + Fix-PR]
```

| Phase | Steps | Executor |
|-------|-------|----------|
| F0 | 0 | Orch + spec subagent |
| F1 | 1–3 | Planner |
| F2 | 4 | Coder |
| F3 | 5 | Verifier (readonly) |
| F4 | 6 (+ fix) | Reviewer + Coder |
| F5 | 7 | Verifier + optional browser |
| F6 | 8–9 | Orch + shell (+ fix-pr) |

| `completedSteps` | Phase done |
|------------------|------------|
| 0 | F0 |
| 1–3 | F1 |
| 4 | F2 |
| 5 | F3 |
| 6 | F4 |
| 7 | F5 |
| 8 | F6 (may continue to 9) |
| 9 | F6 fix-pr complete |

## Step index

| N | Label | `subagent_type` | Worktree | RO |
|---|-------|-----------------|----------|-----|
| 0 | Spec Creation | GP | opt‡ | — |
| 1 | Planning and Brainstorm | GP | opt‡ | — |
| 2 | Plan Refinement (conditional) | GP | opt‡ | — |
| 3 | Execution Plan and DAG | GP | opt‡ | — |
| 4 | Implementation (DAG) | GP | step-4‡ | — |
| 5 | Check-implementation | GP | opt‡ | ✓ |
| 6 | Code Review (+ fix) | GP+shell | step-6‡ | — |
| 7 | Testing | GP+shell | step-7‡ | — |
| 8 | Ship (delivery + push/PR) | shell+GP | cleanup | — |
| 9 | Fix-PR | GP+shell | — | — |

‡ Worktree + complexity rules: [`PROTOCOLS.md`](PROTOCOLS.md). GP = `generalPurpose`. All steps dispatch. Fixed labels for board/banners.

## Step 0 — Pipeline classify (before Step 1)

After `step-00-{slug}.spec.md` exists and **before** the transition gate to Step 1, invoke [`ws-classify-complexity`](../ws-classify-complexity/SKILL.md):

1. Run `classify.cjs` → `step-00-{slug}.classify.md` (advisory artifact; not a delivery-stage file).
2. **User gate** — unless `autoMode` or `skipQualityGates`:

   | # | Option |
   |---|--------|
   | 1 | **Accept recommendation** (Recommended) |
   | 2 | **Override to standard** |
   | 3 | **Override to lite** |

   `autoMode` / `user-gate-auto`: index **0** (accept recommendation). Log: `classify | recommended={lite|standard} | choice={accept|override-standard|override-lite} | ISO`.
3. **Apply** `finalPipeline`. Mid-flight on standard orch: if recommendation is `lite`, **stay on standard** unless user chooses **Override to lite** — never silently switch `workflowType`.

**Orthogonal to Complexity gate:** Pipeline classifier (`lite` | `standard`) recommends orchestrator choice. Full-orch **Complexity gate** ([`gates.md`](../ws-shared/gates.md) § Complexity gate; [`PROTOCOLS.md`](PROTOCOLS.md) § Complexity / Dynamic Execution) uses `simple` | `standard` | `complex` to skip or enforce Steps 1–2–3. Do not merge axes — `lite` ≠ `simple`; `standard` recommendation ≠ `complex`.

When `skipQualityGates` is active: skip classifier **enforcement** (user gate); advisory `classify.md` may still be written. Standalone: `/classify-complexity <spec-path>` per classify skill.

## Quality gate bypass (`skipQualityGates`)

`--skip-gates` → `skipQualityGates: true` in state (or `config.json` → `invariants.skipQualityGates`). Parsed at bootstrap — [`setup.md`](../ws-shared/setup.md) § Parse flags.

When active:

| Skipped (quality gates only) | Never skipped |
|------------------------------|---------------|
| Classifier user-gate / recommendation enforcement | Build, test, security/leak scan |
| Fable quality visibility (PREPARE may ⏭) — **except** `auditVerdictsBlockShip` + **REFUTED** (safety floor still STOP) | SCM resolution |
| Pre-advance CI (`validate_state --pre-advance`; [`STEP-DISPATCH.md`](STEP-DISPATCH.md)) | HS-1–HS-4 |
| Telemetry soft gates (e.g. `scoreAndRefine` advisory reclassify prompts) | Required `--elapsed` / State Hygiene; `update_state` + `--jsonl-out` |

**Banner:** Progress Board and Step 8 delivery result show `[GATES BYPASSED]` alongside mode flags (`[AUTO]`, `[DRY-RUN]`, etc.).

**Bypass JSONL event** — append to current step file `{plansDir}/{slug}/telemetry/step-{NN}.jsonl`:

```json
{"type":"gate-bypass","gate":"{name}","reason":"skip-gates|config","timestamp":"ISO"}
```

Pass `--bypassed` on `update_state` when `skipQualityGates` is active ([`protocols/state-hygiene.md`](protocols/state-hygiene.md)).

## Load when advancing

1. [`PROTOCOLS.md`](PROTOCOLS.md) — Authorization Ladder, transition discipline, step-specific protocols, state schema, base prompt prefix, HS-* stops.
2. [`STEP-DISPATCH.md`](STEP-DISPATCH.md) — step instruction bodies.
3. [`gates.md`](../ws-shared/gates.md) — user-gate menus.
4. Step skill from index above.

Bootstrap/entry → [`setup.md`](../ws-shared/setup.md). Post-workflow QA → [`ws-update-plan-implementation`](../ws-update-plan-implementation/SKILL.md) (outside this orch).

## Triggers

```
@[ws-spec-to-pr] [auto|dry-run|skip-testing|skip-tests|skip-gates|full|strict] [US {issue_id} | {org}/{project}#{id} | {name}.spec.md | "feature description"]
/ws-spec-to-pr [flags] [US {issue_id} | {org}/{project}#{id} | {name}.spec.md | "feature description"]
/status | progress | where am I? → Progress Board only
go back | change plan | back to step X → Backward Nav (not in auto)
switch model | change model → Pause → IDE/agent host → Resume
```

Flags: `auto`, `dry-run`, `skip-testing`, `skip-tests`, `skip-gates`, `full`, `strict`. Standalone classify: `/classify-complexity <spec-path>` ([`ws-classify-complexity`](../ws-classify-complexity/SKILL.md)). Model = session only.
