# Delivery Result Protocol (Step 12)

Before delivery commit. `dryRun`: simulate result + plan edits + benchmark; no real commit.

## Steps

1. **`Read`** sources: `step-00-{slug}.spec.md`, `step-02-{slug}.plan.refined.md` (or `step-01-{slug}.plan.md` if Step 2 bypassed), `step-06-{slug}.plan.report.md`, `step-10-{slug}.report.md`, integration report if exists, `## Open items` in state.
2. **`Write`** `{us-dir}/step-12-{slug}.result.md`:

```markdown
# {slug} — Delivery Result

## Expected
<!-- from spec ACs + plan scope -->

## Done
<!-- from verify report + delivery report + completed DAG tasks -->

## Next steps
<!-- open items, reservations, manual follow-ups before PR -->

## References
- Spec: {specPath}
- Plan: step-02-{slug}.plan.refined.md (or step-01-{slug}.plan.md if Step 2 was bypassed)
- Verify: step-06-{slug}.plan.report.md
- Delivery: step-10-{slug}.report.md
```

3. **Capture LOC delta** (`Shell` from repo root):
   - Baseline: `git ls-files src/ web/ tests/ | xargs git show {baselineCommit}: 2>/dev/null | wc -l`
   - Final: `git ls-files src/ web/ tests/ | xargs wc -l 2>/dev/null | tail -1 | awk '{print $1}'`
   - Diff: `git diff --stat {baselineCommit} -- src/ web/ tests/ | tail -1` → parse added/removed → `telemetry.loc`
4. **Compute benchmark:** sum `telemetry.steps[].elapsedSec` → `totalElapsedSec`; sum tokens → `totalTokens`; `netDelta = added - removed`.
5. **Append Benchmark** (template below) to `step-12-{slug}.result.md`.
6. **Update plan checkmarks:** `[x]` per verify report + `completedTasks` + `completedSteps` ≥5.
7. Register `resultSnapshot` + `telemetry.workflowEndedAt` in state `## Artifacts`.
8. **G2-delivery gate** → stage plan + result → `git commit -m "docs({slug}): delivery plan and result"`.
9. Log `step-12-delivery-commit | {sha}` in `## Gate history` and `commits[]`.

## Telemetry & benchmark

Gate time (AskQuestion waiting) excluded — agent execution time only.

| Event | Field |
|-------|-------|
| Workflow start | `telemetry.workflowStartedAt` |
| Step N dispatch/finish | `telemetry.steps[N].dispatchedAt` / `finishedAt` / `elapsedSec` |
| Workflow end | `telemetry.workflowEndedAt` |
| Total | `telemetry.totalElapsedSec` |

Tokens: LLM metadata or estimate chars/3.5 (`estimated: true`). Shell steps (7, 12, 13): elapsed only; tokens=0.

LOC at bootstrap + Step 12:

```bash
git ls-files src/ web/ tests/ | xargs wc -l 2>/dev/null | tail -1
git diff --stat {baselineCommit} -- src/ web/ tests/ | tail -1
```

Fields: `telemetry.loc.baseline`, `.final`, `.added`, `.removed`, `.netDelta` (count `src/`, `web/`, `tests/` only).

### step-output telemetry contract

```yaml
step-output:
  telemetry:
    elapsedSec: {int}
    promptTokens: {int|null}
    completionTokens: {int|null}
    estimated: {boolean}
```

Missing telemetry → **HS-5**.

### Benchmark template (append to result)

```markdown
## Benchmark

| Metric | Value |
|--------|-------|
| Total wall-clock time | {h}h {m}m {s}s ({totalElapsedSec}s agent execution) |
| Steps executed | {N} |
| Total tokens | {sum} (estimated: {bool}) |
| Lines added | +{added} |
| Lines removed | -{removed} |
| Net LOC delta | +{netDelta} |
| Baseline LOC | {baseline} |
| Final LOC | {final} |

### Step breakdown

| Step | Label | Model | Elapsed | Tokens (est.) | Files changed |
|------|-------|-------|---------|---------------|---------------|
| 0 | Spec Creation | {model} | {elapsedSec}s | {tokens} | {n} |
```

Token efficiency: `tokens/loc`. Velocity: `loc/min`.
