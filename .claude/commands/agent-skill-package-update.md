---
name: agent-skill-package-update
description: Workflow command scaffold for agent-skill-package-update in ERP.Fiscal.
allowed_tools: ["Bash", "Read", "Write", "Grep", "Glob"]
---

# /agent-skill-package-update

Use this workflow when working on **agent-skill-package-update** in `ERP.Fiscal`.

## Goal

Add, update, or remove agent skill packages and synchronize AGENTS.md documentation and skill routing.

## Common Files

- `.agents/skills/*/SKILL.md`
- `.agents/skills/*/scripts/*`
- `.agents/skills/*/README.md`
- `.agents/skills/shared/*`
- `.agents/AGENTS.md`
- `AGENTS.md`

## Suggested Sequence

1. Understand the current state and failure mode before editing.
2. Make the smallest coherent change that satisfies the workflow goal.
3. Run the most relevant verification for touched files.
4. Summarize what changed and what still needs review.

## Typical Commit Signals

- Update or add multiple files under .agents/skills/ (SKILL.md, scripts, references, etc.)
- Update .agents/AGENTS.md and/or AGENTS.md at root to reflect changes in skills or routing
- Update or clean up related documentation (e.g., shared/AGENTS.md, gates.md, setup.md)
- Optionally update rules under .cursor/rules/ or related .mdc files
- Optionally update scripts or CHANGELOG.md

## Notes

- Treat this as a scaffold, not a hard-coded script.
- Update the command if the workflow evolves materially.