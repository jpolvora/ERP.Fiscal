# ws-goal-loop — Templates

Load from [`SKILL.md`](SKILL.md) when writing round or final reports.

## Per-iteration report (`$RUNTIME_DIR/round-<N>.md`)

```markdown
# Goal Loop Round <N>

| Field | Value |
|-------|-------|
| ID | <ID> |
| Iteration | <N>/<MAX_ITERATIONS> |
| Mode | drive / watch |
| Success criterion | <SUCCESS_CRITERION> |
| Criterion met | yes / no |
| Actions taken | <summary> |
| Build/tests | <pass/fail> |
| Commit | <hash> |
| Push | <yes/no> |
```

## Final report

```markdown
# Goal Loop Result — <ID>

| Field | Value |
|-------|-------|
| ID | <ID> |
| Iterations executed | <N> |
| Stop reason | convergence / max iterations / user / escalate / error |
| Criterion met | yes / no |
| Rounds | <summary per round> |
| Final state | <evidence of last collect> |
| URL | <resource URL> |
```

## Sentinel sketch

```bash
RUNTIME_DIR="<resolved runtime dir>"
mkdir -p "$RUNTIME_DIR"
echo "goal_loop_wake_<ID>" > "$RUNTIME_DIR/sentinel.pid"
echo $! >> "$RUNTIME_DIR/sentinel.pid"
sleep <WAIT_SECONDS> && echo 'GOAL_LOOP_WAKE_<ID> {"reason":"post-push","id":"<ID>","iteration":<N>}'
```

Prefer `{plansDir}/{slug}/.runtime/`; fallback `.agents/skills/ws-goal-loop/runs/<ID>/`. Never OS temp.
