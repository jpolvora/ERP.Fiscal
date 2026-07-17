# State Hygiene Protocol

After step N, before the progress board, the orchestrator MUST execute State Hygiene.

## Automated update

```bash
python .agents/skills/spec-to-pr/scripts/update_state.py \
  .cursor/plans/{slug}/{workflow-id}.state.md \
  --step {N} \
  --status {completed|failed|skipped} \
  --elapsed {elapsedSec} \
  --tokens {promptTokens}:{completionTokens} \
  --model {modelName} \
  --created "{comma_separated_created_files}" \
  --modified "{comma_separated_modified_files}" \
  --deleted "{comma_separated_deleted_files}" \
  --gate-choice "{gateChoice}"
```

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

Fail hygiene → **HS-5** (STOP before Progress Board).
