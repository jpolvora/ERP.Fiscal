---













name: ws-goal-fix-pr
description: PR thread convergence loop — orchestrates iterative fix-pr rounds until all open PR review threads are resolved and checks pass.
version: 0.0.109
disable-model-invocation: true
invocation_names:
  - goal-fix-pr
  - ws-goal-fix-pr
---

# ws-goal-fix-pr

Drive PR review threads to zero by wrapping [ws-fix-pr](../ws-fix-pr/SKILL.md) in a [ws-goal-loop](../ws-goal-loop/SKILL.md): auto-approve cooperative gates and re-check threads after every push until `activeThreads == 0`.

Act as a **Principal Engineer** coordinating fix iterations and driving open threads to zero. Thread-count probes and fix rounds are SCM-aware: resolve `providers.scm`, then delegate platform I/O: never hardcode a GitHub-only or ADO-only recipe here. See [examples.md](examples.md) for worked scenarios.

## Invocation

Standalone:

```
/ws-goal-fix-pr <PR-NUMBER> [dry-run] [max <n>] [wait <n>]
```

Workflow (Step 9 of ws-spec-to-pr / Step 5 of ws-spec-to-pr-lite): dispatched by the orchestrator after ship creates a PR (`stopBeforeFixPr: true`); receives `PR-NUMBER` and `max` from orchestrator state.

| Parameter | Default | Notes |
|-----------|---------|-------|
| `<PR-NUMBER>` | required | Target Pull Request number |
| `dry-run` | false | Simulate fixes/resolutions; no commits, pushes, or resolve calls |
| `max <n>` | 10 | Iteration ceiling (align with `ws-ship-pr` default) |
| `wait <n>` | 300 | Post-round / pre-check wait interval in seconds |

Before executing, restate the parsed parameters: PR number, success criterion, mode, `max`, `wait`, `dry-run`, and `providers.scm`.

## SCM resolution

Resolve per [config-resolution.md](../ws-shared/config-resolution.md).

| `providers.scm` | Provider skill | Intent used here |
|-----------------|----------------|-------------------|
| `github` | [ws-github-provider](../ws-github-provider/SKILL.md) | `list-threads` |
| `azure-devops` | [ws-azure-devops-provider](../ws-azure-devops-provider/SKILL.md) | `list-threads` |

Success criterion: `len(activeThreads) == 0` from a `list-threads` call, using the provider's normalized status: never a raw `gh pr view … jq` or ADO collect command inlined here.

## Automation overrides (vs fix-pr defaults)

| fix-pr gate | ws-goal-fix-pr behavior |
|-------------|----------------------|
| Confirmation gate (plan-gate.md) | Auto-yes: save gate file and proceed |
| Commit + resolve + push gate | Auto: execute unless `dry-run` |
| Escalate threads | Stop iteration; block until user resolves ambiguity |
| CI Auto-Fix `in_progress` | Inform user; do not auto-block |

## Steps

1. **Initialize**: restate parameters (above) and resolve `providers.scm`.
   - Done when: PR number, mode, and provider are confirmed.

2. **Initial heartbeat check**: call `list-threads`. If `activeThreads == 0` on this first check, do not exit: arm the ws-goal-loop 300s heartbeat timer, wait, and re-collect once.
   - Done when: `activeThreads` is confirmed either still 0 (stop, converged) or > 0 (proceed to Act).

3. **Act round**: dispatch [ws-fix-pr](../ws-fix-pr/SKILL.md) for `<PR-NUMBER>` with overrides active. Commit as `fix(#<PR-NUMBER>): fix issues from review threads [<threadId>, ...]`, resolve fixed threads by executing the `resolveReviewThread` GraphQL mutation (via `resolve_thread.cjs` or `gh api graphql` so `isResolved` transitions to `true`), and `git push origin HEAD` (skip push when `dry-run`).
   - Done when: the round's approved threads are fixed or resolved, and pushed (unless `dry-run`).

4. **Verify**: run `config.json.verification` commands plus a `ws-code-review` diff check. Three consecutive verification failures stop the loop and escalate.
   - Done when: verification passed, or the loop has stopped and escalated.

5. **Re-check & loop**: wait `<wait>` seconds, re-collect `activeThreads`, and repeat from step 3 until `activeThreads == 0`, `max` is reached, escalation occurs, or the user aborts.
   - Done when: one of the stop conditions above is met.

6. **Final report**: always output: iterations executed and stop condition; threads handled per round (fixed / resolved / escalated); links to round reports (`{reviewsDir}/PR-<N>-round-*.md`; `{reviewsDir}` ← `config.reviews.dir`); commit hashes and push confirmation; final `activeThreads` count with evidence; PR URL; and the merge handoff note (this skill never merges: the caller merges only after `activeThreads == 0` and required checks are green).
   - Done when: the report is presented to the user.

Language: en-us only.
