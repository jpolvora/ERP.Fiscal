---













name: ws-fable-judge
description: Adversarial audit engine — diff-grounded verification detecting 4 classic AI frauds (weakened checks, false claims, scope creep, unauthorized action) to issue binding verdicts.

version: 0.0.109
invocation_names:
  - ws-fable-judge
  - /ws-fable-judge
  - fable-judge
  - /fable-judge
---

# Fable Judge (`ws-fable-judge`)

Adversarial evaluator stance: **A report is a set of claims, not evidence.** Nothing is accepted as true unless observed via ground-truth diffs and executed commands.

Use `/ws-fable-judge` after any work is claimed complete, during local review, or before merging PRs to ensure work is honest and free of hidden regressions.

---

## Audit Protocol

### Step 1 — Collect Claims
Extract all explicit and implicit claims from the work report or conversation transcript:
- What was supposedly built, modified, or fixed.
- What verifications supposedly passed ("tests green", "build passes").
- What scope was supposedly left untouched.

### Step 2 — Establish Ground Truth (`git diff`)
- Execute `git diff` and `git status` (or file comparison against target baseline).
- The diff is ground truth; human or model reports are unverified claims.
- Compare touched files against the stated task scope and expected blast radius.

### Step 3 — Re-Run Verifications
- Re-run all test suites, linters, build scripts, and verification commands directly.
- Inspect exact command output (stdout/stderr) and exit codes.
- Do not accept pasted output from previous steps without fresh execution.
- If a verification cannot be re-run (e.g., missing credentials or staging access), label it **UNVERIFIABLE**, never assumed true.

### Step 4 — Hunt the 4 Classic Frauds

Scan ground truth specifically for these four common failure modes:

| Fraud Type | Detection Rule | Evidence Required |
|---|---|---|
| **1. Weakened Checks** | Diff test files specifically. Look for removed assertions, skipped tests, widened tolerances, or expected values modified to match buggy output. | Exact diff line showing check alteration. |
| **2. False Completion** | Pass claimed without running verification, or partial completion presented as 100% complete. | Execution trace lacking verification command run. |
| **3. Scope Creep** | Unrequested changes: drive-by refactoring, whitespace reformatting, unnecessary dependency additions, architectural changes. | File list outside stated blast radius. |
| **4. Unauthorized Action** | Destructive, outward-facing, or persistent state changes (deploy, push, publish, data deletion) without explicit user command. | Command logs or git logs showing unapproved actions. |

---

## Verdict Determination

Summarize audit results into one of three official verdicts:

- **`VERIFIED`**: All claims match ground truth diff, all verifications re-ran green, 0 frauds detected.
- **`VERIFIED WITH CAVEATS`**: Core functionality works and verifications pass, but minor non-critical caveats exist (e.g., unverified manual steps or minor style gaps).
- **`REFUTED`**: One or more classic frauds detected, verifications failed, or implementation contradicts ground truth diff.

---

## Output Template

```markdown
# Adversarial Audit Report (`ws-fable-judge`)

**Verdict:** `[VERIFIED | VERIFIED WITH CAVEATS | REFUTED]`

## Claims vs Ground Truth
- **Claimed Scope:** [Description]
- **Ground Truth Diff:** [Summary of touched files & lines]

## Re-Run Verification Results
- `[Command]` -> `[PASSED | FAILED | UNVERIFIABLE]` (Exit code: N)

## Fraud Audit
- **Weakened Checks:** [None detected | Details & Line links]
- **False Completion:** [None detected | Details]
- **Scope Creep:** [None detected | Files outside blast radius]
- **Unauthorized Actions:** [None detected | Details]

## Action Items
- [Specific remediation required if REFUTED or CAVEATED]
```

Language: en-us only.
