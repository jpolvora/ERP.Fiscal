---













name: ws-goal-loop
description: Generic convergence loop primitive — stateful loop engine providing sentinel management, heartbeat/settle timers, and re-check convergence control.

version: 0.0.109
disable-model-invocation: true
invocation_names:
  - goal-loop
  - ws-goal-loop
---

# ws-goal-loop

Convergence loop skeleton. A **goal-orchestrator** loads this skill and binds success criteria, collect, and act.

```
Goal → collect+evaluate → [act → verify → report → wait]×N → done
```

## Parameters

| Parameter | Description |
|-----------|-------------|
| `PR_NUMBER` | Target PR |
| `SUCCESS_CRITERION` | Checkable expression (e.g. `activeThreads == 0`) |
| `MAX_ITERATIONS` | Hard stop (default **20**) |
| `WAIT_SECONDS` | Post-push wait before re-collect (default **300**) |
| `COLLECT_CMD` / `ACT_CMD` / `VERIFY_CMD` | Collect / act / verify commands |
| `DRY_RUN` | Skip push, resolve, destructive actions |
| `RUNTIME_DIR` | Sentinel dir (default `{us-dir}/.runtime` or skill `runs/`) |

## Sentinel

Project-local runtime only (never OS temp). Templates: [`TEMPLATES.md`](TEMPLATES.md).

| Rule | Detail |
|------|--------|
| One per session | Kill old PID before starting new |
| Track | `$RUNTIME_DIR/sentinel.pid` |
| Stop/abort | Kill + remove PID file |
| Wake regex | `^GOAL_LOOP_WAKE_<ID>` for `notify_on_output` |

## Loop (Done when each stage completes)

1. **Collect + evaluate** — Run `COLLECT_CMD` vs `SUCCESS_CRITERION`. Met → final report, kill sentinel, stop. Fail → stop (no improvised APIs).
2. **Act** — One `ACT_CMD` round (sync → investigate → fix → validate → commit → resolve → push, or dry-run sim).
3. **Verify** — Fresh `VERIFY_CMD` / review / publish / resolve evidence. **3× identical failure** → stop and escalate.
4. **Report** — `$RUNTIME_DIR/round-<N>.md` per [`TEMPLATES.md`](TEMPLATES.md).
5. **Heartbeat** — Arm sentinel `WAIT_SECONDS`; on wake re-collect. `0` active → done. Else `n+1` or stop at `MAX_ITERATIONS`.
6. **Exit** — Criterion true · max iterations · user stop · escalation · collect failure.

## Stop conditions

| Condition | Action |
|-----------|--------|
| Criterion met | Final report + kill sentinel |
| User stop / escalate / max / collect fail | Kill sentinel; summarize blockers / remaining |

## Dry-run

When `DRY_RUN=true`: no push/resolve/destructive; log + reports + verify; **do not** arm sentinel — re-collect immediately.

## Dependencies

Reports/sentinel: [`TEMPLATES.md`](TEMPLATES.md) · Artifact registry: [`ARTIFACTS.md`](../ws-spec-to-pr/ARTIFACTS.md)
