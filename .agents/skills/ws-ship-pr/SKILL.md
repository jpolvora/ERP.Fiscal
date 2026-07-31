---













name: ws-ship-pr
description: End-to-end PR shipping manager — drives prepare-to-PR checklists, pushes code, creates PRs, waits for CI, and manages convergence.
version: 0.0.109
disable-model-invocation: true
invocation_names:
  - ship-pr
  - ws-ship-pr
---

# ship-pr

Ship from `config.project.workingBranch` (default `develop`) to `config.project.baseBranch`. Act as a **DevOps Engineer / Release Manager**: drive the **prepare-to-PR** goal checklist (verify + **discover and wait** for local consumer prepare / before-push / before-publish harness steps), then push/create PR via the SCM provider configured in `.agents/skills/ws-shared/config.json` (`providers.scm`: GitHub, Azure DevOps / ADO, etc.), wait 30 seconds for automated code-review actions / CI pipelines to start on SCM infrastructure, run `ws-goal-fix-pr` (default 300 seconds heartbeats), and merge only when clean.

## SCM Independence & Configuration

`ws-ship-pr` is **SCM-provider independent**. It reads `.agents/skills/ws-shared/config.json` at runtime to determine the active SCM platform and dispatch the corresponding provider skill:

- **`providers.scm: "github"`** → dispatches [`ws-github-provider`](../ws-github-provider/SKILL.md) (`gh` CLI / GitHub REST API)
- **`providers.scm: "azure-devops"`** (or `"ado"`) → dispatches [`ws-azure-devops-provider`](../ws-azure-devops-provider/SKILL.md) (`az repos` / ADO REST API)
- **`providers.scm: "local"`** → dispatches [`ws-local-spec-provider`](../ws-local-spec-provider/SKILL.md)

Prepare board (mandatory): [PREPARE-CHECKLIST.md](PREPARE-CHECKLIST.md). Wait/converge timing: [GOAL-OVERRIDES.md](GOAL-OVERRIDES.md). Examples: [examples.md](examples.md).

## Invocation

Standalone:

```
/ship-pr [commit-title] [base=<branch>] [head=<branch>] [dry-run] [no-merge] [skip-gates] [max <n>]
```

Workflow: `ws-spec-to-pr` Step 8 or `ws-spec-to-pr-lite` Step 4. Dispatched with `workflowMode: true`, `shipAction`, and typically `stopBeforeFixPr: true`: create/push PR and STOP; orch Step 9 runs `ws-goal-fix-pr`/`fix-pr`.

| Parameter | Default | Notes |
|-----------|---------|-------|
| `commit-title` | (optional) | Message for any uncommitted changes |
| `base` | `config.project.baseBranch` | Auto-detect `main`/`master` if unset |
| `head` | `config.project.workingBranch` (`develop`) | Branch to push and use as PR head |
| `dry-run` | `false` | Simulate; no commits/push/real PR API calls |
| `no-merge` | `false` | Create PR and run checks; stop before merge |
| `skip-gates` | `false` | Sets `skipQualityGates: true` — see § Quality gate bypass |
| `max <n>` | `10` | `ws-goal-fix-pr` iteration cap (ignored when `stopBeforeFixPr`) |
| `workflowMode` | `false` | Orchestrator-set: execute `shipAction` without re-asking |
| `shipAction` | (orchestrator-selected) | `create-pr` \| `push-only` \| `skip` |
| `stopBeforeFixPr` | `false` | Skip Step 6; orchestrator owns fix-PR at Step 9 |
| `skipQualityGates` | `false` | Orchestrator-set from `--skip-gates` or `config.json` → `invariants.skipQualityGates` |

Before executing, restate commit title, head/base, SCM provider (read from `config.json`), mode, `skipQualityGates`, `stopBeforeFixPr`, max, and `shipAction`. When `skipQualityGates` is active, prefix banners with **`[GATES BYPASSED]`**. Resolve branches and provider from `.agents/skills/ws-shared/config.json` only.

## Quality gate bypass (`skipQualityGates`)

`--skip-gates` → `skipQualityGates: true` (or `config.json` → `invariants.skipQualityGates`). Parsed at orchestrator bootstrap — [`setup.md`](../ws-shared/setup.md) § Parse flags; inherited in `workflowMode`.

When active:

| Skipped (quality gates only) | Never skipped |
|------------------------------|---------------|
| Fable PREPARE row 5 visibility (may credit ⏭) — see [PREPARE-CHECKLIST.md](PREPARE-CHECKLIST.md) § 5 | Build, test, security/leak scan (PREPARE rows 1–4) |
| **Except** `auditVerdictsBlockShip` + **REFUTED** (safety floor still STOP) | SCM resolution and auth |
| Consumer prepare discovery when orch already satisfied (cite evidence) | HS-1–HS-4 |

**Safety floor (never bypassed):** When `config.json` → `fable.enabled`, `autoAudit`, and `auditVerdictsBlockShip` are all `true`, a **REFUTED** [`ws-fable-judge`](../ws-fable-judge/SKILL.md) verdict still **STOP**s delivery — Step 1 preflight and PREPARE row 5 ❌.

**Banner:** Closing summary, Prepare board header, and delivery outcome show **`[GATES BYPASSED]`** alongside mode flags (`[DRY-RUN]`, etc.) when `skipQualityGates` is true.

## Steps

1. **Preflight**: resolve `workingBranch`/`baseBranch`/`gitRemote` and SCM provider (`providers.scm` in `config.json`); confirm active branch is `workingBranch`; check `git status` and tracking drift; `git pull {gitRemote} {workingBranch}`; auto-detect base via `bash .agents/skills/ws-ship-pr/scripts/detect-base-branch.sh` if unset; stop on unexpected dirty files outside delivery scope.
   - Optional `fable` integration (safety floor — **never** bypassed by `skipQualityGates`): If `config.json.fable.enabled`, `autoAudit`, and `auditVerdictsBlockShip` are `true`, verify [`ws-fable-judge`](../ws-fable-judge/SKILL.md) audit verdict is not `REFUTED`. If `REFUTED`, stop delivery and require remediation before pushing or creating PR.
   - Done when: branches and SCM provider resolved; working tree clean enough to ship and pulled.

2. **Prepare to PR (goal)**: load [PREPARE-CHECKLIST.md](PREPARE-CHECKLIST.md). Drive coverage → build → tests → security → **fable-judge verdict (row 5)** → **consumer prepare discovery** (row 6; scan local `AGENTS.md` / `{sharedDir}/AGENTS.md` / `rules.*` / ship docs for prepare or before-push/publish/deliver steps; **wait** until those obligations complete) until every required row is ✅/⏭. **Always show row 5** on the board; under `skipQualityGates` it may credit ⏭ for visibility only — **`REFUTED` still ❌ STOP** when `auditVerdictsBlockShip` applies. Show the board to the user after each item and before shipping. Credit orch Steps 6–7 only with cited evidence for the **current** tree. STOP on any ❌ — including unfinished discovered local prepare steps.
   - Done when: board shown; scan evidence recorded for row 6; all required rows ✅/⏭.

3. **Code-review loop**: skip if already reviewed under `ws-spec-to-pr` Step 6 or `ws-spec-to-pr-lite` Step 3 (record on board). Otherwise load [ws-code-review](../ws-code-review/SKILL.md) against `base` and run the fix → re-review loop for Critical/Warning (max 3 rounds; Pause on residual).
   - Done when: review clean, Pause after 3-iteration cap with residual documented, or skipped with evidence.

4. **Commit & push**: only after Step 2 is green. Commit remaining ship-scope changes (delivery commit may already exist under `workflowMode`); `git push -u {gitRemote} {workingBranch}`. Skip push when `shipAction: skip` or `dry-run`.
   - Done when: branch pushed with no uncommitted ship-scope changes, or ship explicitly skipped.

5. **Create PR**: only when Step 2 is green and `shipAction: create-pr` (or standalone default). Resolve `providers.scm` per [`config-resolution.md`](../ws-shared/config-resolution.md) (read from `config.json`: `github`, `azure-devops` / `ado`, or `local-spec-provider`; STOP if unresolved — do not invent a client). Load matching provider ([ws-github-provider](../ws-github-provider/SKILL.md) or [ws-azure-devops-provider](../ws-azure-devops-provider/SKILL.md)), `validate-auth` (STOP on failure), then `create-pr --head {workingBranch} --base {baseBranch}` (reuse open PR for same head→base when present). Capture PR id and URL.
   - Done when: PR id/URL captured or reused. If `stopBeforeFixPr` and `shipAction: create-pr`: print URL and STOP (success).

6. **Monitor reviews & converge**: skip if `stopBeforeFixPr` (orch Step 9 owns [ws-goal-fix-pr](../ws-goal-fix-pr/SKILL.md)). Otherwise, after pushing and creating PR, wait **30 seconds** (wait for code-review action / CI workflows to start on SCM infrastructure), then start [ws-goal-fix-pr](../ws-goal-fix-pr/SKILL.md) (default **300 seconds** heartbeat/settle loop, [GOAL-OVERRIDES.md](GOAL-OVERRIDES.md)), poll required checks and `list-threads` via the configured SCM provider, and dispatch `ws-goal-fix-pr` until `activeThreads == 0` or `max`. Never merge while threads remain, checks are red, or on escalate-stop. Prepare the handoff prompt/state for `ws-goal-fix-pr` even when stopping early so Step 9 can resume cleanly.
   - Done when: `activeThreads == 0` and required checks green, or run stopped with PR URL reported.

7. **Merge**: only when Step 6 converged and checks green. Configured SCM provider intent `merge-pr`; skip when `no-merge` or `stopBeforeFixPr`. Never delete `{workingBranch}`.
   - Done when: merged via configured SCM provider or explicitly skipped; `{workingBranch}` intact.

8. **Telemetry aggregate** (post-delivery, non-blocking): after successful ship completion — PR created (`stopBeforeFixPr` / workflow Step 8 handoff), merge done (standalone or full convergence), or `shipAction: skip` with workflow delivery marked complete — run `node bin/generate-telemetry-aggregate.cjs` (writes `{plansDir}/telemetry/aggregate.json`). When `stopBeforeFixPr`, orchestrator Step 9 also runs this after `ws-goal-fix-pr` convergence (idempotent). On failure: **warn and continue** — do not block ship, merge, or PR handoff.
   - Done when: aggregate script ran or failure warned; delivery outcome already reported.

## Output

```markdown
**PR:** {provider-returned-url}
```

In `dry-run`, `push-only`, `skip`, or early `stopBeforeFixPr` stop, state the outcome clearly (no placeholder URL). Always include the final Prepare to PR board in the closing summary. When `skipQualityGates` is active, prefix the closing summary with **`[GATES BYPASSED]`**.

## Dependencies

- Prepare board: [PREPARE-CHECKLIST.md](PREPARE-CHECKLIST.md) · Verify helper: `bash .agents/skills/ws-ship-pr/scripts/verify.sh`
- SCM Providers (configured via `config.json` `providers.scm`): [ws-github-provider](../ws-github-provider/SKILL.md) · [ws-azure-devops-provider](../ws-azure-devops-provider/SKILL.md) · [ws-local-spec-provider](../ws-local-spec-provider/SKILL.md)
- Security: [ws-secrets-leak-review](../ws-secrets-leak-review/SKILL.md)
- Review: [ws-code-review](../ws-code-review/SKILL.md) · Convergence: [ws-goal-fix-pr](../ws-goal-fix-pr/SKILL.md) · Fixer: [ws-fix-pr](../ws-fix-pr/SKILL.md)
- Base detection: `bash .agents/skills/ws-ship-pr/scripts/detect-base-branch.sh` · Artifacts: [ARTIFACTS.md](../ws-spec-to-pr/ARTIFACTS.md)

Language: en-us only.
