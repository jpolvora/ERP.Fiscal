---
name: spec-to-pr
description: >-
  Spec-to-PR delivery orchestrator FSM (F0–F6, steps 0–12; 13 with `--full`). Agent contract only — not human docs.
  Invoke: /spec-to-pr | @[spec-to-pr]. Entry: GitHub issue | Azure DevOps work item | *.spec.md | feature description.
  Flags: dry-run, auto, skip-integration, skip-tests, full, strict. Delegates via Task tool.
  Legacy aliases: /us-workflow, /us-delivery-workflow.
upstream: jpolvora/workflow-skills — this skill is a workflow owned by workflow-skills. Improvements must be submitted upstream to https://github.com/jpolvora/workflow-skills
---

## Audience & load

| Audience | Doc |
|----------|-----|
| **Orchestrator (this file)** | FSM + tool bindings + asserts |
| **Humans** | [`README.md`](README.md), [`docs/faq.md`](docs/faq.md), [`DIAGRAM.md`](DIAGRAM.md) |

**Load:** current step + linked protocols only. Setup → [`setup.md`](../shared/setup.md). Gates (dual-mode) → [`gates.md`](../shared/gates.md). Config/SCM → [`config-resolution.md`](../shared/config-resolution.md). Artifacts → [`ARTIFACTS.md`](ARTIFACTS.md). Dispatch → [`STEP-DISPATCH.md`](STEP-DISPATCH.md) (load only when advancing/dispatching). Protocols → [`protocols/`](protocols/) (on demand). Stack → `config.json.rules.stackFile` (auto-loaded steps 5,7,9–11). Hub → [`AGENTS.md`](../../../AGENTS.md). Step 2 → [`02-interview`](../02-interview/SKILL.md). Tools → [`tools.md`](../shared/tools.md). Dual-mode with [`spec-to-pr-lite`](../spec-to-pr-lite/SKILL.md): shared skills must stay interchangeable.

## Language

**All skill content and user-facing output: English.** No PT/PT-BR in instructions, gates, banners, Progress Board.

## Native tool contract

Canonical tool names from [`tools.md`](../shared/tools.md). Project params from [`config.json`](../shared/config.json) (repo-root path: `.agents/skills/shared/config.json`). Never narrate undone work.

| Intent | Tool alias | Native | Rule |
|--------|------------|--------|------|
| Step work | `dispatch-agent` | `Task` | `subagent_type: generalPurpose\|shell`; `description: "STP step {N} — {Label}"`; `readonly: true` step 6 only; no resume across steps; step 5 DAG ≤3 parallel |
| User gate | `user-gate` / `user-gate-auto` | `AskQuestion` | Prefer when available; markdown fallback per [`gates.md`](../shared/gates.md); ≥2 options; recommended first; cancelled → HS-1; auto → auto-gate |
| Build/test | `build-backend`, `test-backend`, etc. | `Shell` | values from `config.json.verification` |
| Source control | `commit-code`, `push-branch`, etc. | `Shell` | `gh`, `git`; cite real output |
| State | `read-state` / `write-state` | `Read` + `Write`/`StrReplace` | truth source; hygiene before board |
| Search | `search-code` | `Grep`/`Glob` | MEMORY.md index; `{config.plans.dir}/*/*.state.md` resume |
| Browser (step 11) | `browser-mcp` | `CallMcpTool` | normal mode only, non-dry-run, non-skip, gated |
| State check | `run-script validate_state` | `Shell` | optional |
| Code edits | `dispatch-agent` Coder | `Task` | orch never edits — hard stop |

Subagents: native tools for evidence; end with parseable `step-output` block.

User output: post-tool summaries + Progress Board + banners.

### User gates (AskQuestion)

Prefer native `AskQuestion` for normal-mode decisions; if unavailable, same options as markdown list (Recommended first). Full contract: [`gates.md`](../shared/gates.md) — slim transitions, one delivery, one ship. Applies to transitions, entry/resume/config, refinement 2c (2e only if needed), G2-code, delivery, ship. **No separate 4†/8† menus** — phase soft tips at F1→F2 / F3→F4 (Pause → Cursor → Resume to switch). Cancelled → **HS-1**. `autoMode` → auto-gate index 0.

---

# Spec-to-PR — Orchestrator

Deterministic FSM; step content delegated to skills via **`Task`**.

## Core Goals
1. **End-to-End Delivery:** Automate the entire feature/US lifecycle from specification bootstrap to PR/Merge (steps 0 to 13).
2. **Context Isolation & State Hygiene:** Run each step in a clean, isolated subagent Task with step-specific worktrees, while keeping state sync (`state.md` + `MEMORY.md`) strictly valid.
3. **Safety & Gates:** Enforce transition gates and model check readiness explicitly before coding and reviewing phases to prevent accidental/incorrect commits.
4. **Portability:** Keep the orchestrator FSM stack-agnostic and configuration-driven, resolving all project metadata and commands dynamically from `config.json` and `stack.md`.

## Invariants

| Topic | Rule |
|-------|------|
| Scope | Steps 0–12 deliver locally (code + plan/result). Push/PR/merge only at Step 13 via the **single ship gate** (required when `fullMode`; optional ask otherwise). No push at Step 12. |
| Auth | G1+ needs gate. AskQuestion cancelled → HS-1. Commit → G2 + explicit menu (HS-2). |
| Isolation | Fresh `Task`/step; `Shell` tag `uswf/{id}/before-step-{N}`; **branch-direct default**; worktree opt-in via config / non-win32 when beneficial (5/10/11). |
| State | Hygiene `Write`/`StrReplace` → asserts → board. Fail → HS-5. |
| Memory | `state.md` short-term (`## Workflow memory`, `## Accumulated decisions`, `## Doc consolidation log`). Root `MEMORY.md` = generalizable patterns. |
| Dual-mode | Shared skills interchangeable with `spec-to-pr-lite`. Config/gates: [`config-resolution.md`](../shared/config-resolution.md), [`gates.md`](../shared/gates.md). `workflowType: standard`. |
| `dryRun` | No `Write` `src/`/`web/`, no commit/push/worktree/browser/MEMORY `Shell`/`Write`. Prefix `[DRY-RUN]`. |
| `autoMode` | No AskQuestion; auto-gate option 0. Prefix `[AUTO]`. HS-3/4/5 pause. No browser MCP. |
| `skipIntegration` | Skip Step 11 → `skippedSteps`+`completedSteps`, log, Step 12. Prefer when no API/UI surface and unit tests green. |
| `skipTests` | Skip test suites in stack.md; build required. `verification.tests: skipped`. |
| `fullMode` | After Step 12 delivery, present Step 13 ship gate (rec: Create PR, monitor, merge). Default: off (ship gate still offered; rec = Skip). |
| Banners | `autoMode` or `dryRun` → Step Output Banner every step. |
| Revert | Workflow manifest + checkpoint only — no global `reset --hard` / `restore .`. |
| Checkpoints | Local tag `uswf/{workflow-id}/before-step-{N}` every boundary. |
| **Workflow artifacts** | **Never `git commit` `.cursor/plans/` files during Steps 0–11.** Code commits (7/10/11 fix) stage `src/`/`web/`/`tests/` only. Delivery commit at Step 12: `step-01-{slug}.plan.md` + `step-12-{slug}.result.md` only. |
| **Pause** | **Pause workflow** keeps **all** artifacts on disk — no cleanup, no delete. `status: active`. |
| Session model | `currentModel` = executing session model. Switch via Pause → Cursor → Resume ([`gates.md`](../shared/gates.md)). |
| Portability | Keep spec-to-pr fully generic and portable. No hardcoded project-specific metadata, paths, solution names, or commands. All dynamic options and metadata must be resolved from `config.json` or `stack.md`. |

**Legacy aliases** (still accepted): `/us-workflow`, `@[us-workflow]`, `/us-delivery-workflow`, `@[us-delivery-workflow]`.

**Runtime tokens (unchanged):** git tags/worktrees use prefix `uswf/`; plan slugs use `us-{id}`. These are historical tokens, not the skill name.

## Allowed deps

| Resource | Path |
|----------|------|
| Orchestrator | `SKILL.md` |
| **Artifacts** | [`ARTIFACTS.md`](ARTIFACTS.md) — canonical filenames + path resolution |
| **Setup** | `setup.md` — initialization, config bootstrap, flags, resume, stack file generation |
| **Config** | `.agents/skills/shared/config.json` — project identity, stack, issue trackers, verification commands, invariants |
| **Tools** | `tools.md` — canonical tool aliases |
| Stack | `config.json.rules.stackFile` — project-specific stack reference; derived from config.json and auto-loaded for code review & optimization |
| Scripts | Orchestrator: `check_memory_conflict.py`, `validate_state.py` under `spec-to-pr/scripts/`. Converters + thread helpers: **canonical** under `github-provider/scripts/` and `azure-devops-provider/scripts/` (thin shims remain at `spec-to-pr/scripts/` and `08-fix-pr/scripts/` for canonicity). Local register/mirror: `local-spec-provider/scripts/`. |
| Providers | [`github-provider`](../github-provider/SKILL.md) · [`azure-devops-provider`](../azure-devops-provider/SKILL.md) · [`local-spec-provider`](../local-spec-provider/SKILL.md) — `providers.active` owns `fetch-to-spec`; `providers.scm` owns PR/thread/merge intents |
| SCM CLIs | Via provider skills only (`gh` / `az`); orchestrator does not embed platform CLI recipes |
| State | `{config.plans.dir}/{slug}/{workflow-id}.state.md` |
| Skills | `00-write-spec`→0 · `01-write-plan`→1 · `02-interview`→2 · `03-plan-to-tasks`→3 · `04-implement-tasks`→5 build, 10 fix · `05-verify-plan`→6 · `06-code-review`→9 · `07-integration-validation`→11 · `11-ship-pr`→13 |
| Spec | `spec-format` |

Filesystem paths use numeric prefix; skill `name:` unprefixed. Post-12 PR: [`code-review`](../06-code-review/SKILL.md) / [`fix-pr`](../08-fix-pr/SKILL.md).

### Work dir `{us-dir}` = `{config.plans.dir}/{slug}/` (default `.cursor/plans/{slug}/`)

| Entry | `slug` |
|-------|--------|
| Issue `{id}` | `us-{id}` |
| `*.spec.md` | basename or frontmatter `slug:` |

State: `{us-dir}/{workflow-id}.state.md` · Canonical spec: `{us-dir}/step-00-{slug}.spec.md`.

Artifacts: `step-00-{slug}.issue.json`, `step-00-{slug}.spec.md`, `step-01-{slug}.plan.md`, `step-02-{slug}.plan.refined.md`, `step-03-{slug}.plan.exec.md`, `step-03-{slug}.exec.dag.json`, `step-06-{slug}.plan.report.md`, `step-10-{slug}.report.md`, `step-11-{slug}.integration-test.plan.md`, `step-11-{slug}.integration-test.report.md`, `step-12-{slug}.result.md` (Step 12 delivery summary — committable).

**Committable (Step 12 only):** `step-01-{slug}.plan.md` (or `step-02-{slug}.plan.refined.md` if generated), `step-12-{slug}.result.md`. Other plan-dir files stay uncommitted unless user explicitly asks.

Git-ignored: `worktrees/step-{N}/`, `{workflow-id}.baseline/`, `{workflow-id}.archive/`. Never write state under `.agents/`.

---

## Phases F0–F6 ↔ steps 0–13

```mermaid
flowchart LR
  F0[F0 Bootstrap] --> F1[F1 Specification]
  F1 --> F2[F2 Implementation]
  F2 --> F3[F3 Verify + 1st Commit]
  F3 --> F4[F4 Review + Fix]
  F4 --> F5[F5 Integration]
  F5 --> F6[F6 Closure]
```

| Phase | Steps | Executor |
|-------|-------|----------|
| F0 | 0 | Orchestrator + spec subagent |
| F1 | 1,2,3 | Planner subagent |
| F2 | 4†,5 | Coder subagent |
| F3 | 6,7 | Verifier + orch + shell |
| F4 | 8†,9,10 | Reviewer + Coder |
| F5 | 11 | Verifier + optional browser |
| F6 | 12, 13 | Orchestrator + shell (+ ship subagent when fullMode) |

† Steps **4,8** = internal phase soft tips on Advance (no dedicated menus) — never in `completedSteps`; log `model-hint` in `## Gate history`.

| `completedSteps` | Phase done |
|------------------|------------|
| 0 | F0 |
| 1–3 | F1 |
| 5 | F2 |
| 6–7 | F3 |
| 9–10 | F4 |
| 11 | F5 |
| 12 | F6 (may continue to 13) |
| 13 | F6 ship complete |

## Step index

| N | Label | Task? | `subagent_type` | Worktree | RO |
|---|-------|-------|-----------------|----------|-----|
| 0 | Spec Creation | ✓ | GP | — | — |
| 1 | Planning and Brainstorm | ✓ | GP | — | — |
| 2 | Refinement (conditional) | ✓ | GP | — | — |
| 3 | Execution Plan and DAG | ✓ | GP | — | — |
| 4† | (internal) Coder phase hint | — | — | — | — |
| 5 | Implementation (DAG) | ✓ | GP | step-5‡ | — |
| 6 | Verification and Report | ✓ | GP | — | ✓ |
| 7 | Decision and First Commit | ✓ | GP+shell | — | — |
| 8† | (internal) Reviewer phase hint | — | — | — | — |
| 9 | Code Review | ✓ | GP | — | — |
| 10 | Fixes, Second Commit and Report | ✓ | GP+shell | step-10‡ | — |
| 11 | Integration Validation and Pre-PR | ✓ | GP+shell | step-11‡ | — |
| 12 | Consolidation and Delivery | ✓ | shell | cleanup | — |
| 13 | Ship & PR | ✓ | GP+shell | — | — |

‡ [Worktree Fallback](#worktree-fallback). GP = `generalPurpose`. Fixed labels for board/banners. Steps 1–3 conditional per [Complexity / Dynamic Execution](#complexity--dynamic-execution).

---

## Protocols

### Authorization Ladder

| Level | Ops | Gate |
|-------|-----|------|
| G0 | Read, RO reports | — |
| G1 | Edit WT, plans, impl (no commit) | Transition gate |
| G2-code | `git commit` **code only** (`src/`, `web/`, `tests/`) | Step 7 / 10 / 11 fix |
| G2-delivery | `git commit` **`{slug}.plan.md` + `{slug}.result.md` only** | Step 12 delivery gate |
| G3 | `git push`, PR create/merge | Step 13 **ship gate only** (not Step 12) |

```text
HS-1: AskQuestion cancelled → STOP; re-present gate. Never infer "yes".
HS-2: Commit without explicit gate menu selection → STOP.
HS-2a: `git add` or commit any `.cursor/plans/` path during Steps 0–11 → STOP (workflow artifacts forbidden until Step 12 delivery commit).
HS-3: Mutating step success + empty files_touched → FAILED.
HS-4: Step 5/10/11 success without expected files on state.branch → FAILED.
HS-5: State Hygiene failed → STOP before Progress Board.
```
Auto: HS-3/4/5 apply; HS-1/2 N/A.

### Transition Discipline

**Normal:** N done → Hygiene → checkpoint `before-step-{N+1}` → Board → summary → Transition Gate → dispatch N+1.

**Auto:** auto-gate + dispatch N+1 same turn.

**Forbidden:** mutating step or commit without gate.

### Refinement FSM (Step 2)

2a/2b/2d → `02-interview`. Orch: 2c Escalate, 2e Shared Understanding, redispatch.

| State | Owner | Output |
|-------|-------|--------|
| 2a Audit | refine | `gap_registry[]` by design-tree |
| 2b Resolve | refine | Close with evidence; codebase before escalate |
| 2c Escalate | orch | AskQuestion — **one** question; max 3 rounds; always **End refinement and advance** |
| 2d Exit | refine | §8 empty or `assumed-default`; `shared_understanding: pending` |
| 2e Shared Understanding | orch | Only if 2c did **not** exit via End refinement. Else auto-confirm. |

Rules: multiple `needs_user` → one by design-tree priority. **End refinement and advance** → log `assumed-default`, set `shared_understanding: confirmed`, skip 2e. Block Step 3 only if interview ran and `refine.shared_understanding !== confirmed`.

**Conditional skip:** See [`gates.md`](../shared/gates.md) § Conditional interview.

### Complexity / Dynamic Execution

Before Step 1, classify per [`gates.md`](../shared/gates.md) § Complexity gate. User may override when ambiguous: **Simple path** / **Standard path** (rec) / **Full grill**.

### Worktree Fallback

```text
dryRun → no worktree
default → branch-direct (preferred on win32 and most consumers)
worktree only when config.plans.useWorktrees=true AND not win32 AND path≤180 AND git worktree add succeeds
```

branch-direct: edits on `state.branch`; subagent `wip(us-{id}): step-{N}` or dirty WT. Post-step 5/10/11: files exist, expected diff, build/tests per stack.md.

### State Hygiene

→ [`protocols/state-hygiene.md`](protocols/state-hygiene.md)

### Model readiness (no separate 4†/8† menus)

No in-gate model picker. At every transition, show the gates.md banner (`Current model` + Pause → Cursor → Resume).

When Advance crosses **F1→F2** (after Step 3, before Step 5) or **F3→F4** (after Step 7, before Step 9), add the soft hint from [`gates.md`](../shared/gates.md) (Coder / Reviewer class). Log `model-hint | F1→F2|F3→F4 | current={currentModel} | ISO`. Tags `before-step-5`, `before-step-9` remain for telemetry only.

Steps **4 and 8** are **not** user-facing menus and stay out of `completedSteps` / Progress Board.

### Step Dispatch & Isolation

Orch calls **`Task`** — never inline step impl.

```yaml
Task:
  subagent_type: generalPurpose | shell
  description: "STP step {N} — {Label}"
  readonly: true   # step 6 only
  run_in_background: false   # step 5 parallel (DAG): ≤3 parallel, same worktree, no file overlap
```

Anchor (`Shell` tag): `uswf/{workflow-id}/before-step-{N} @ {sha}`. Worktree 5/10/11 via `Shell`: `worktree add` → merge → `worktree remove` → `branch -d`. Max 1 active. Audit: `Write` `stepDispatches[]`. No per-DAG-task worktree.

**Step 5 dispatch:**
- `execMode: sequential` → single `Task` `04-implement-tasks` mode `build` with `step-01-*.plan.md` directly (no DAG).
- `execMode: parallel` → DAG: `Task` per level, ≤3 concurrent, no file overlap within level.

### Learning & Memory Protocol

At step start, subagent reads `state.md` (`## Workflow memory`, `## Accumulated decisions`, `## Step outputs`) and `.agents/skills/shared/MEMORY.md` index. After step, record `step-output.learning` → orchestrator appends to `## Workflow memory`.

**Step 12 sweep:** Promote generalizable patterns to `shared/memory/*.md` + run `self_learning.py --compile`. Criteria: technical, generalizable, non-duplicate, concise. `dryRun`: log in `## Doc consolidation log` only.

### Specification Protocol

[`spec-format`](../spec-format/SKILL.md). Canonical spec: `{us-dir}/step-00-{slug}.spec.md` — never live tracker APIs and never `*.issue.json` after Step 0.

| Input | Tracker / provider | Action | Uses Step 0? |
|-------|--------------------|--------|--------------|
| `{n}` or `US {n}` | `providers.active` | `fetch-to-spec` → `{us-dir}/step-00-us-{n}.spec.md` | No — skip to Step 1 |
| `{org}/{project}#{id}` / `ADO {id}` / `WI {id}` | `azure-devops-provider` | `fetch-to-spec` | No — skip to Step 1 |
| `*.spec.md` | `local-spec-provider` | `fetch-to-spec` | No — skip to Step 1 |
| free-text / no args | none | `00-write-spec` → spec file | Yes — `Task` `00-write-spec` |

Provider resolution and `fetch-to-spec` dispatch: load active provider skill; auth failure → STOP (no silent fallback). Details in each provider `SKILL.md`.

### Step 0 Entry Gate

1. **Tracker id** → provider `fetch-to-spec` → skip Step 0 → Step 1 gate.
2. **Local `*.spec.md`** → `local-spec-provider` → skip Step 0 → Step 1 gate.
3. **No args / free-text** → Entry menu: issue/spec path / brainstorm (`00-write-spec` only path).

Store `specPath` in state `## Artifacts`.

### Build & Test Validation (7, 10)

Before G2-code commit: `config.json.rules.stackFile` → build (+ tests unless skip) → Coder fix loop. Stage **only** `src/`, `web/`, `tests/` — never `.cursor/plans/`. `skipTests`: `verification.tests: skipped`.

### Integration Validation (11)

`07-integration-validation` via **`Task`**. `skipIntegration` → skip to Step 12. `autoMode`/`dryRun` → Task without browser.

Gates (normal): **Approve and run test battery** (rec) / **Run without browser** / **Adjust test plan** / **Skip validation** / **Pause workflow**.

Failure (max 3): **Apply fixes and revalidate** (rec) / **Accept with reservations** / **Re-run without fixes** / **Pause**. Fix: G2-code commit only.

### Workflow Artifact Commit Protocol

| When | Allowed |
|------|---------|
| Steps 0–11 | **Code only** under `src/`, `web/`, `tests/` at Steps 7, 10, 11 fix |
| Steps 0–11 | **Forbidden:** `.cursor/plans/**`, exec/dag/report/state/issue files |
| Step 12 | Plan + result — delivery commit via G2-delivery gate |
| Pause | No commit; no delete |

Orch `git add` must be path-scoped — never `git add .` on steps 7/10/11.

### Delivery Result (Step 12)

→ [`protocols/delivery-result.md`](protocols/delivery-result.md)

### Optional Artifact Cleanup (Step 12)

→ [`protocols/artifact-cleanup.md`](protocols/artifact-cleanup.md)

### Progress Board & banners

→ [`protocols/progress-board.md`](protocols/progress-board.md)

### Automatic Mode

Parse: `auto` + combinable `dry-run`, `skip-integration`, `skip-tests`, US/spec entry. Normalize legacy `automatico`/`automático` → `auto` internally.

Resume: active `autoMode` same US → continue `currentStep`; else new `workflow-id`.

| Context | Auto choice (index 0) |
|---------|----------------------|
| Step 0 entry gate | **I have a US/issue number** (user must provide in invocation) |
| Complexity ambiguous | **Standard path** |
| Transition 1–6, 9–11 | **Advance to Step N+1** |
| Transition / phase model | **Advance** with session `currentModel` (no `--model-chain`) |
| Step 2 needs_user | first option; early → **End refinement and advance** (auto-confirms 2e) |
| Step 2e (only if shown) | **I confirm shared understanding — advance to Step 3** |
| Step 7 | **Approve, validate build/tests and commit code** |
| Step 11 skipIntegration / no API-UI | skip step |
| Step 11 plan | **Approve and run test battery without browser** |
| Step 11 failure | **Apply fixes and revalidate** |
| Step 12 delivery | **Commit plan and result, keep artifacts** |
| Step 13 ship (`fullMode`) | **Create PR, monitor, and merge when ready** |
| Step 13 ship (not `fullMode`) | **Skip shipping** |

Shared defaults: [`gates.md`](../shared/gates.md) § Auto-gate defaults. Log `auto-gate | step {N} | {choice} | ISO`. Disabled: backward/repeat/pause menus; Step 3 without shared understanding.

### Checkpoints

Tag `uswf/{workflow-id}/before-step-{N}` = HEAD before step N first mutation. `before-step-1` = `baselineCommit`. Mirror in `checkpoints[]`. Delete on Step 12/reset. Dry-run: log only.

### Safe Revert & Backward Navigation

**Revert** = manifest to checkpoint M. Scope: `reset --mixed` → per-path restore from `## Step file log` → remove worktrees ≥M → truncate state <M. Verify `preExistingDirty`. Forbidden: global `reset --hard`, `checkout -- .`, `restore .`, `clean -fd`, stash, push `uswf/*` tags.

**Bootstrap revert data:** `baselineCommit`, `preExistingDirty[]`, backup `{workflow-id}.baseline/`. Full reset: M=1 + new workflow-id.

**Backward nav** (normal only): Gate **Go back** or Step 7→5 shortcut. Targets: 1–3,5–7,9–11 in `completedSteps`. Sub-menu: Planning/Implementation/Review/Validation → confirm → checkpoint revert → redispatch M. Log `backward-nav | from | to | ISO`.

---

## State & dispatch

### `state.md` YAML

```yaml
workflowId, slug, us, specSource, specPath
startedAt, endedAt, status: active|completed|cancelled|failed
currentStep, dryRun, autoMode, skipIntegration, skipTests, fullMode
execMode: sequential|parallel|null  # set after Step 3
branch, baselineCommit, preExistingDirty: []
checkpoints, workflowManifest, commits: [{sha, step, message}]
completedSteps, stepStatus, skippedSteps, completedTasks, stepDispatches
refineRound, currentModel  # session-derived; refresh on resume
stepModels: [{step: N, model: "name", dispatched: ISO}]
# modelChain removed — ignore if present in old state files
telemetry:
  workflowStartedAt: ISO
  workflowEndedAt: null
  totalElapsedSec: null
  loc: { baseline, final, added, removed, netDelta }
  totalTokens: int|null
  steps: [{ N, label, dispatchedAt, finishedAt, elapsedSec, promptTokens, completionTokens, estimated, model, filesTouched }]
```

Sections: Workflow baseline, manifest, Step file log, Refinement registry, Context, Artifacts, Step outputs, Step model log, Workflow memory, Accumulated decisions, Doc consolidation log, Open items, Gate history.

### Resume / reset

→ [`setup.md`](../shared/setup.md) § Resume / reset

### Base Prompt Prefix (`Task` body)

```markdown
# Subagent — Step {STEP} — {Label}
Read state: `.cursor/plans/{slug}/{workflow-id}.state.md`
Skill: {SKILL.md path} — read full.
Orch: SKILL.md § Step {STEP} · model {currentModel} · {modeFlags}
Enhancing skills (mandatory): karpathy-guidelines, caveman, self-learning, gabarito
Read: state workflow memory + decisions + doc log; MEMORY.md index; `config.json.rules.stackFile`.
Anchor: uswf/{workflow-id}/before-step-{STEP} @ {sha} · CWD: {repo-root | worktree}
Role: fresh; no resume. files_touched required (revert). model: {currentModel}.
Rules: no `.cursor/plans/` in git-add except Step 12 G2-delivery; needs_user: ≥2 choices, recommended first.
Learning: read ## Workflow memory + ## Step outputs (all prior steps) for traps/errors. Do NOT repeat broken approaches. Record own mistakes in step-output.learning.
Telemetry required: elapsedSec, promptTokens + completionTokens (from LLM metadata if available, else estimate chars/3.5 with estimated: true).
End with ```step-output(status, step, artifacts, files_touched, verification, refine, summary, evidence, decisions, doc_consolidation, needs_user, errors, retry_hint, learning, model, telemetry{elapsedSec, promptTokens|null, completionTokens|null, estimated})
```
```

### Transition Gates

Post-step: hygiene → checkpoint (`Shell` tag) → short summary → gate. Board at phase boundaries.

| Mode | Tool |
|------|------|
| auto | auto-gate table → immediate `Task`/`Shell` |
| normal | Prefer `AskQuestion`; slim menu per [`gates.md`](../shared/gates.md) |

Shows gates.md banner (`Current model` + Pause → Cursor → Resume) and `**Next step:** {N+1} — {Label}`. Primary: **Advance** (Recommended) / **More options…**. Soft tips at F1→F2 / F3→F4 only.

---

## Bootstrap & Entry

→ [`setup.md`](../shared/setup.md) § Bootstrap & Entry

## Step instructions

→ [`STEP-DISPATCH.md`](STEP-DISPATCH.md) (load when advancing/dispatching)

## Error policy

Retry: max 3; backoff 0s→30s→60s. Revert: Checkpoint Algorithm only. Conduct: 4/8 no Task; orch never implements code; fresh Task/step; branch-direct default; G2-code steps 7/10/11; G2-delivery step 12; G3 step 13 only; HS-2a blocks plan-dir commits mid-workflow.

## Post-workflow (outside this agent)

Manual QA after workflow completion (or pause before Step 12) not resumed here. Use [`10-update-plan-implementation`](../10-update-plan-implementation/SKILL.md) — append plan §9, implement delta, update `{slug}.result.md`, certify for PR. Distinct from Step 10 (in-pipeline review fixes).

## Triggers

```
@[spec-to-pr] [auto|dry-run|skip-integration|skip-tests|full|strict] [US {issue_id} | {org}/{project}#{id} | {name}.spec.md | "feature description"]
/spec-to-pr [flags] [US {issue_id} | {org}/{project}#{id} | {name}.spec.md | "feature description"]
/status | progress | where am I? → Progress Board only
go back | change plan | back to step X → Backward Nav (not in auto)
switch model | change model → Pause workflow, switch in Cursor, resume (no in-gate picker)
```

**Flags:** `auto`, `dry-run`, `skip-integration`, `skip-tests`, `full`, `strict` (full US verification at Step 6). Model = session; switch via Pause → Cursor → Resume ([`gates.md`](../shared/gates.md)).

Gates: [`gates.md`](../shared/gates.md). Config: [`config-resolution.md`](../shared/config-resolution.md).

If invoked **without** US number, spec path, or description:
> **Give me a GitHub issue id, an Azure DevOps work item (`ADO {id}` or `{org}/{project}#{id}`), a path to a hand-written `*.spec.md`, or a free-text feature description to start.**

Examples:
- `/spec-to-pr auto skip-tests skip-integration US 1234`
- `/spec-to-pr "Implement a product analytics dashboard with real-time charts"`
- `/spec-to-pr auto contoso/project#5678`
- `/spec-to-pr ADO 2416`
- `/spec-to-pr specs/my-feature.spec.md`
- `/spec-to-pr full US 99`
- `/spec-to-pr auto skip-tests US 567`
