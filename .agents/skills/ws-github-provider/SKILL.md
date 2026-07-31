---













name: ws-github-provider
description: GitHub SCM provider — handles GitHub issue-to-spec conversion, authentication, PR creation, review thread resolution, and merging via gh CLI.
version: 0.0.109
disable-model-invocation: true
invocation_names:
  - github-provider
  - ws-github-provider
---

# ws-github-provider

GitHub-specific inbound (`fetch-to-spec`) and SCM (`create-pr`, threads, merge). Pipeline skills load this when `providers.active` / `providers.scm` is `github`; they link here instead of embedding `gh` recipes.

Resolve `owner` / `repo` from `{sharedDir}/config.json` ([`config-resolution.md`](../ws-shared/config-resolution.md)). Never hardcode org/repo.

## Invocation

### Standalone Mode

```
/ws-github-provider <intent> [args...]
```

Examples: `fetch-to-spec 2416` · `validate-auth` · `create-pr --head develop --base main` · `list-threads 42` · `merge-pr 42`.

### Workflow Mode

Orch entry / `ws-fix-pr` / `ws-goal-fix-pr` / `ws-ship-pr` pass intent + args when `providers.active` or `providers.scm` is `github`; gates follow the parent.

| Parameter | Default | Notes |
|-----------|---------|-------|
| `<intent>` | required | Contract table below |
| ids | per intent | Tracker/SCM ids — not baked into config for other repos |
| `dry-run` | false | Simulate when caller supports it |

## Prerequisites

- `gh` on `PATH`; `gh auth status` for CLI flows.
- GraphQL threads: `AGENTIC_CODE_REVIEWERS_GITHUB_TOKEN` → `GITHUB_TOKEN` → `GH_TOKEN` → `gh auth token` fallback.
- Config: `issueTrackers.github` and/or `providers.active` / `providers.scm` = `github`.

Auth failure → **STOP** with `validate-auth` fixes. No silent provider fallback.

## Intent contract

| Intent | Input | Output | Implementation |
|--------|-------|--------|----------------|
| `fetch-to-spec` | Issue id / URL | `{us-dir}/step-00-us-{n}.spec.md` (+ optional `*.issue.json`) | `gh issue view` → converter |
| `validate-auth` | none | Pass/fail + fixes | `gh auth status` + thread token note |
| `create-pr` | head, base, title/body | PR URL + id | `gh pr create` (reuse open head→base) |
| `list-threads` | PR id | Thread list | `fetch_threads.cjs` |
| `resolve-thread` | thread id (+ comment) | Resolved (`isResolved: true` via `resolveReviewThread` GraphQL mutation) | `resolve_thread.cjs` |
| `merge-pr` | PR id | Merged | `gh pr checks --watch` then `gh pr merge --merge` |

**Branch rule:** never `--delete-branch` when head is `project.workingBranch` (default `develop`).

**Procedures:** load [`INTENTS.md`](INTENTS.md) for the intent being run.

## Canonical scripts

Prefer these paths (legacy orch/fix-pr shims may forward here):

| Script | Path |
|--------|------|
| Issue → spec | `.agents/skills/ws-github-provider/scripts/github-issue-to-spec.py` |
| List threads | `.agents/skills/ws-github-provider/scripts/fetch_threads.cjs` |
| Resolve thread | `.agents/skills/ws-github-provider/scripts/resolve_thread.cjs` |

Optional: `issueTrackers.github.issueToSpecScript` must still resolve to the converter.

## Config keys

| Key | Role |
|-----|------|
| `providers.active` / `providers.scm` | `github` → this skill |
| `issueTrackers.github` | enabled, org/repo, optional `issueToSpecScript` |
| `project.workingBranch` / `baseBranch` / `gitRemote` | create/merge defaults |
| `plans.dir` | `{us-dir}` root |

Legacy: absent `providers.*` → GitHub default when `issueTrackers.github.enabled`.

## Dependencies

[ws-spec-to-pr](../ws-spec-to-pr/SKILL.md) · [ws-ship-pr](../ws-ship-pr/SKILL.md) · [ws-fix-pr](../ws-fix-pr/SKILL.md) · [ws-goal-fix-pr](../ws-goal-fix-pr/SKILL.md) · [ws-spec-format](../ws-spec-format/SKILL.md)
