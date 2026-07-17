# Optional Artifact Cleanup (Step 12)

**Only when `status: completed` and user explicitly chooses cleanup** — never on **Pause workflow**.

Gate options (folded into delivery gate "delete temps" in [`gates.md`](../../shared/gates.md)):

- **Delete temporary artifacts** — exec/dag/issue/report/integration files, worktrees, baseline, archive, checkpoint tags
- **Keep all artifacts** (audit)

## Cleanup execution

1. Delete temp files:
   ```bash
   rm {us-dir}/step-03-{slug}.plan.exec.md
   rm {us-dir}/step-03-{slug}.exec.dag.json
   rm {us-dir}/step-00-{slug}.issue.json
   rm {us-dir}/step-06-{slug}.plan.report.md
   rm {us-dir}/step-11-{slug}.integration-test.plan.md
   rm {us-dir}/step-11-{slug}.integration-test.report.md
   ```
2. Remove checkpoint tags: `git tag -l "uswf/{workflow-id}/*" | xargs -r git tag -d`
3. Remove worktrees + prune branches:
   ```bash
   git worktree list | grep "uswf/{workflow-id}" | awk '{print $1}' | while read wt; do
     git worktree remove "$wt" --force 2>/dev/null
   done
   git branch -l "uswf/{workflow-id}/*" | sed 's/^[* ]*//' | xargs -r git branch -D
   ```
4. Remove baseline: `rm -rf {us-dir}/{workflow-id}.baseline/`
5. Remove archive: `rm -rf {us-dir}/{workflow-id}.archive/`

**Preserved:** `step-01-{slug}.plan.md`, `step-02-{slug}.plan.refined.md`, `step-12-{slug}.result.md`, `step-00-{slug}.spec.md`, `{workflow-id}.state.md` (while `status: active`).

**Post-cleanup verification:**
```bash
git worktree list | grep "uswf/{workflow-id}" && echo "WARN: worktree remains" || echo "Clean"
git tag -l "uswf/{workflow-id}/*" | wc -l | xargs -I{} echo "Tags remaining: {}"
git branch -l "uswf/{workflow-id}/*" | wc -l | xargs -I{} echo "Branches remaining: {}"
```

**Pause / `status: active`:** skip cleanup. **Dry-run:** log intended deletions only.
