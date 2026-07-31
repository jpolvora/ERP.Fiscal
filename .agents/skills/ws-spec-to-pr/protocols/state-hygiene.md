# State Hygiene Protocol

After step N, before the progress board, the orchestrator MUST execute State Hygiene.

**Applies in `autoMode` / `fullMode` / `dryRun` the same as normal.** Skipping hygiene or omitting `--elapsed` → **HS-5**.

## Automated update

```bash
python {skillsRoot}/ws-spec-to-pr/scripts/update_state.py \
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
  --jsonl-out {plansDir}/{slug}/telemetry/step-{NN}.jsonl
```

`--jsonl-out` is **mandatory** on every `update_state` call (zero-padded `NN` = step number). Creates `{plansDir}/{slug}/telemetry/` lazily. When `--skip-gates` or `config.json.invariants.skipQualityGates` is active, add `--bypassed`.

### `--elapsed` (mandatory)

| Status | Rule |
|--------|------|
| `completed` / `failed` | `--elapsed` **required** (integer ≥ 0). Measure agent wall-clock for the step (dispatch → step-output). **Do not omit** (script rejects missing flag). **Do not invent 0** unless the step truly finished in under 1s. |
| `skipped` | `--elapsed 0` allowed (script defaults to 0 when status is skipped). |

Source: `step-output.telemetry.elapsedSec`. Missing telemetry on a completed/failed step → **HS-5** before Progress Board.

Script also:

- Writes `telemetry.steps[]` + recomputes `totalElapsedSec` / `totalTokens` (null `elapsedSec` treated as 0)
- Upserts `## Telemetry log` table row in the state body
- Appends `## Gate history` line

## Manual fallback (if Python unavailable)

```yaml
- Refresh currentModel from executing session model (unknown if unavailable). If changed vs prior, log model-change | step {N} | {old} → {new} | ISO in ## Gate history. Ignore leftover modelChain.
- Pass session model into --model {modelName} when calling update_state.py (recorder only; not a user override flag)
- Append ## Step outputs ### Step N (include model: {modelName} in block)
- Append step-output.learning → ## Workflow memory (dedupe)
- Merge files_touched → ## Step file log ### Step N
- Append to ## Step model log: | Step N | {label} | {model} | dispatched {ISO} |
- Record telemetry: elapsedSec, promptTokens, completionTokens, estimated → ## Telemetry ### Step N
- Append to ## Telemetry log: | Step N | {label} | {model} | {elapsedSec}s | {tokens} |
- Recompute workflowManifest; update completedSteps, stepStatus, currentStep
- Assert created paths exist; currentStep = next gate
- Step 2: ## Refinement registry
```

## Pre-advance validation (before dispatch to step N+1)

After `update_state` and checkpoint tag `uswf/{workflow-id}/before-step-{N+1}`, run as a **shell command** (not `dispatch-agent`):

```bash
python {skillsRoot}/ws-spec-to-pr/scripts/validate_state.py \
  {plansDir}/{slug}/{workflow-id}.state.md \
  --pre-advance {N+1}
```

Checks (per [`ARTIFACTS.md`](../ARTIFACTS.md) step-input table): checkpoint tag exists and is reachable; required input artifacts on disk; `completedSteps` monotonicity.

| Result | Action |
|--------|--------|
| exit 0 | Continue → Progress Board → Transition Gate → dispatch N+1 |
| exit ≠ 0 | **HS-5** — STOP; no board, no gate, no dispatch |

**Bypass (this gate only):** When `--skip-gates` or `skipQualityGates` is active, **skip** this shell call. Record bypass in JSONL via `--bypassed` on `update_state` (`gate: pre-advance`). Does not bypass build/test/security, HS-1–HS-4, or other quality gates.

Fail hygiene or pre-advance → **HS-5** (STOP before Progress Board).
