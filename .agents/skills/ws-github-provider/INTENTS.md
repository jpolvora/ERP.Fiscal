# ws-github-provider — Intent procedures

Load when executing an intent from [`SKILL.md`](SKILL.md). Expand `{plansDir}` from config (`plans.dir`, default `.agents/plans`). Resolve `{owner}/{repo}` from config — never literals.

## `validate-auth`

```bash
gh auth status
```

- Exit 0 → pass for CLI issue/PR flows.
- Non-zero → instruct `gh auth login` (or fix `GH_TOKEN` / host); **STOP**.
- For `list-threads` / `resolve-thread`, also require a token in `AGENTIC_CODE_REVIEWERS_GITHUB_TOKEN`, `GITHUB_TOKEN`, or `GH_TOKEN`. If missing, print the scripts’ token hint and **STOP**.

## `fetch-to-spec`

Entry: `{n}`, `US {n}`, or GitHub issue URL → slug `us-{n}`.

```bash
mkdir -p {plansDir}/us-{n}
gh issue view {n} --json number,title,body,state,labels,assignees,comments,url \
  > {plansDir}/us-{n}/step-00-us-{n}.issue.json
python .agents/skills/ws-github-provider/scripts/github-issue-to-spec.py \
  --input {plansDir}/us-{n}/step-00-us-{n}.issue.json \
  --output {plansDir}/us-{n}/step-00-us-{n}.spec.md \
  --repo {owner}/{repo}
```

## `create-pr`

```bash
gh pr create --head {head} --base {base} --title "{title}" --body-file {plansDir}/pr-body.md
```

On Windows / PowerShell prefer `--body-file` (or single-quoted body) to avoid backtick escape mangling. Reuse an existing open PR for the same head→base when present. Capture PR number and URL for the caller (`ws-ship-pr`).

## `list-threads`

```bash
node .agents/skills/ws-github-provider/scripts/fetch_threads.cjs {PR_ID} [--json]
```

Return structured threads (`threadId`, path, line, comments) to `ws-fix-pr` / `ws-goal-fix-pr`.

## `resolve-thread`

```bash
node .agents/skills/ws-github-provider/scripts/resolve_thread.cjs {THREAD_ID} "{resolution note}"
```

Skip remote mutation when the parent skill is in `dry-run`.

## `merge-pr`

```bash
gh pr checks {PR_ID} --watch
gh pr merge {PR_ID} --merge
```

Do **not** add `--delete-branch` for the configured working branch (`project.workingBranch`, default `develop`).
