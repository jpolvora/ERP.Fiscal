---
name: security-audit-tooling-update
description: Workflow command scaffold for security-audit-tooling-update in ERP.Fiscal.
allowed_tools: ["Bash", "Read", "Write", "Grep", "Glob"]
---

# /security-audit-tooling-update

Use this workflow when working on **security-audit-tooling-update** in `ERP.Fiscal`.

## Goal

Add or update security audit tools, pre-commit hooks, and documentation to enhance secret scanning and compliance.

## Common Files

- `scripts/pre-commit-security-check.sh`
- `scripts/audit-history-secrets.sh`
- `.husky/pre-commit`
- `.gitleaks.toml`
- `.agents/skills/security-check/SKILL.md`
- `README.md`

## Suggested Sequence

1. Understand the current state and failure mode before editing.
2. Make the smallest coherent change that satisfies the workflow goal.
3. Run the most relevant verification for touched files.
4. Summarize what changed and what still needs review.

## Typical Commit Signals

- Add or update scripts for security checks (e.g., pre-commit-security-check.sh, audit-history-secrets.sh)
- Add or update configuration files for security tools (e.g., .gitleaks.toml, .husky/pre-commit)
- Update package.json and package-lock.json to add dependencies
- Update documentation (README.md, AGENTS.md, SKILL.md for security-check)
- Update .gitignore as needed

## Notes

- Treat this as a scaffold, not a hard-coded script.
- Update the command if the workflow evolves materially.