# ws-azure-devops-provider — Intent procedures

Load when executing an intent from [`SKILL.md`](SKILL.md). Expand `{plansDir}` from config. Resolve `{org}` / `{project}` / `{apiBase}` / `{patEnvVar}` from `issueTrackers.azureDevOps` — never consumer literals.

## `validate-auth`

1. Read `org`, `project`, `apiBase`, `patEnvVar` from `issueTrackers.azureDevOps` in `{sharedDir}/config.json`.
2. Resolve PAT: env named by `patEnvVar` → `ADO_PAT` → `AZURE_DEVOPS_PAT` (legacy file secret only if env empty and legacy config path exists).
3. If org/project missing or PAT empty → print fix instructions; **STOP**.
4. Optional smoke: GET `{apiBase}/{org}/{project}/_apis/wit/fields/System.State?api-version=7.1` with Basic auth (empty user + PAT), or `fix_pr_azure_context.py` smoke.

## `fetch-to-spec`

| Input | Org/project | Slug |
|-------|-------------|------|
| `ADO {id}` / `WI {id}` | config tracker | `us-{id}` |
| `{org}/{project}#{id}` | parsed (must match tracker) | `us-{id}` |
| ADO work-item URL | parsed from URL | `us-{id}` |

```bash
mkdir -p {plansDir}/us-{id}
python .agents/skills/ws-azure-devops-provider/scripts/ado-workitem-to-spec.py \
  --org {org} --project {project} --id {id} \
  --api-base {apiBase} --pat-env {patEnvVar} \
  --snapshot {plansDir}/us-{id}/step-00-us-{id}.issue.json \
  --output {plansDir}/us-{id}/step-00-us-{id}.spec.md
```

## `create-pr`

Prefer Azure CLI:

```bash
az repos pr create \
  --organization "https://dev.azure.com/{org}" \
  --project "{project}" \
  --repository "{repository}" \
  --source-branch {head} \
  --target-branch {base} \
  --title "{title}" \
  --description "{body}"
```

`{repository}` from remote / project config. Reuse open PR for same source→target when present. REST equivalent OK when `az` unavailable (same PAT + org/project).

## `list-threads`

```bash
python .agents/skills/ws-azure-devops-provider/scripts/fix_pr_azure_context.py collect \
  --pr-id {PR_ID} \
  --output .agents/skills/ws-fix-pr/runs/pr-{PR_ID}/context.json
```

Return structured threads to `ws-fix-pr` / `ws-goal-fix-pr`. Active count = `len(activeThreads)`. Collect prints `collect-summary:` on stderr. `context.json` is UTF-8 — on Windows re-read with `encoding="utf-8"`.

## `resolve-thread`

```bash
python .agents/skills/ws-azure-devops-provider/scripts/fix_pr_azure_context.py resolve-thread \
  --pr-id {PR_ID} \
  --thread-id {THREAD_ID} \
  --model {model} \
  --comment "{resolution note}"
```

Pass `--dry-run` when parent is dry-run; skip remote mutation.

## `merge-pr`

1. Wait for required policies / PR status (mirror GitHub checks watch).
2. Complete:

```bash
az repos pr update --id {PR_ID} --status completed \
  --organization "https://dev.azure.com/{org}" \
  --project "{project}"
```

Do **not** delete the configured working branch after completion.
