---













name: ws-multi-spec
version: 0.0.109
description: Sequential smart multi-spec batch orchestrator — evaluates spec complexity across project specifications to dispatch standard or lite pipeline workers.

invocation_names:
  - multi-spec
  - ws-multi-spec
---

## Audience & load

| Audience | Doc |
|----------|-----|
| **Master Orchestrator (this file)** | Loop FSM + tool bindings + intelligence flow auto-detection |
| **Protocol & State** | [`PROTOCOL.md`](PROTOCOL.md) · [`STATE.md`](STATE.md) |
| **Examples & Evals** | [`EXAMPLES.md`](EXAMPLES.md) · [`evals/evals.json`](evals/evals.json) |

**Always-on load:** this file + [`PROTOCOL.md`](PROTOCOL.md). **On demand:** [`STATE.md`](STATE.md) · [`EXAMPLES.md`](EXAMPLES.md) · [`../ws-spec-to-pr/SKILL.md`](../ws-spec-to-pr/SKILL.md) · [`../ws-spec-to-pr-lite/SKILL.md`](../ws-spec-to-pr-lite/SKILL.md) · [`../ws-ship-pr/SKILL.md`](../ws-ship-pr/SKILL.md) · [`../ws-goal-fix-pr/SKILL.md`](../ws-goal-fix-pr/SKILL.md) · [`../ws-shared/tools.md`](../ws-shared/tools.md). Language: **en-us** only.

## Native tool contract

Canonical aliases: [`../ws-shared/tools.md`](../ws-shared/tools.md). Params: `{sharedDir}/config.json`. Never narrate undone work. Master orchestrator never edits code directly — dispatches `ws-spec-to-pr` or `ws-spec-to-pr-lite` workers via `dispatch-agent`.

| Intent | Alias | Rule |
|--------|-------|------|
| Worker dispatch | `dispatch-agent` | `generalPurpose` or subagent; `description: "ws-multi-spec worker [{flowMode}] — {slug}"`; sequential execution |
| User gate | `user-gate` / `user-gate-auto` | Selection gate for blank scan; failure pause gate (Resume, Skip, Abort) |
| SCM / state probe | `Shell` | SCM query & file probes; parse worker `step-output` |
| State persistence | `write-to-file` | Update `{plansDir}/ws-multi-spec/{runId}.state.md` |

# `ws-multi-spec` — Smart Multi-Spec Orchestrator

Sequential multi-spec batch delivery orchestrator with **smart complexity & flow auto-detection**.

## Goals

1. Batch process a list or directory of specs sequentially.
2. **Smart Flow Auto-Detection**: Evaluate spec complexity via [`ws-classify-complexity`](../ws-classify-complexity/SKILL.md) (live `dagThresholds`) to select `ws-spec-to-pr` (full) or `ws-spec-to-pr-lite` (fast). See [`PROTOCOL.md`](PROTOCOL.md).
3. Probe before execution to skip already-implemented specs.
4. Base branch synchronization (`baseBranch`) before worker dispatch and on run resume.
5. **Complete End-to-End Cycle per Spec**: After worker creates PR, wait for automated code reviewers and CI, execute `ws-goal-fix-pr` until `activeThreads == 0`, and explicitly merge & close PR via SCM provider before queue advancement.
6. **Post-Merge Base Sync**: Automatically fetch and pull base branches (`main`/`master`/`develop`) immediately after PR merge success so subsequent feature branches (`checkout -b`) are created from an up-to-date, synced base.
7. Unified state management across specs in `{plansDir}/ws-multi-spec/{runId}.state.md`.
8. Pause on worker failure with clear recovery options (Resume, Skip, Abort).

## Invariants

| Topic | Rule |
|-------|------|
| Sequential | Exactly one spec worker active at a time |
| Base Branch Sync | Master records `baseBranch` in state file header; before worker dispatch or on resume, spec feature branch MUST be synced with `baseBranch` (`git merge {baseBranch}` or `git rebase {baseBranch}`) to incorporate latest base features/merged code |
| Flow Auto-Detect | `ws-spec-to-pr-lite` when ≤3 steps / ≤6 files / ≤2 layers / frontmatter `complexity: low`; `ws-spec-to-pr` otherwise |
| Merge & Closure | Every created PR MUST undergo `ws-goal-fix-pr` convergence (waiting for automated code reviews & CI), resolve `activeThreads` to 0, and be explicitly merged (`state: MERGED`) via SCM provider (`gh pr merge` / SCM API). Leaving PRs open/unmerged is FORBIDDEN |
| Post-Merge Sync | Immediately after PR merge success (`state: MERGED`), master MUST fetch and pull `baseBranch` (`main`/`master`/`develop`) so the next spec worker's feature branch (`git checkout -b feature/{slug}`) is created from an already updated and fully synced base branch |
| Next-Spec Block | Master orchestrator MUST NOT dispatch the next spec until prior spec PR is fully merged (`merged: true`, `activeThreads: 0`, `state: MERGED`) on SCM provider and base branches are synced |
| Isolation | Fresh worker context per spec; no shared scratch across specs |
| Pause on fail | No silent continue on worker failure; gate required |
| Skip done | Probe already-implemented items before dispatching |
| Dry-run | Forward `dryRun` to worker; state records simulated outcome with `[DRY-RUN]` label |

## Triggers

```
/ws-multi-spec
/ws-multi-spec .agents/specs/13-runner.spec.md .agents/specs/14-editor.spec.md
/ws-multi-spec .agents/plans/ws-multi-spec/ms-20260725T220000Z.state.md
```
