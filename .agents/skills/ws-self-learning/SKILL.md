---








name: ws-self-learning
version: 0.0.109
description: Anti-regression memory engine — consults shared MEMORY before planning/coding and records newly discovered traps into the project knowledge hub.

invocation_names:
  - self-learning
  - ws-self-learning
---

# Self-Learning

**Bidirectional gate** — MEMORY is both input (avoid known traps) and output (record new ones).

Expand path tokens first ([`tools.md`](../ws-shared/tools.md) § Path tokens): `{sharedDir}` → `.agents/skills/ws-shared`, `{skillsRoot}` → `.agents/skills`.

Consumer-owned memory lives in the shared hub (never overwritten by install/update):

- Entries: `{sharedDir}/memory/YYYY-MM-DD-[slug].md`
- Compiled index: `{sharedDir}/MEMORY.md`

## When to run

| Moment | Action |
|--------|--------|
| **Before plan / before code / before fix** | **Consult:** `Grep` / `Read` `{sharedDir}/MEMORY.md` for keywords of the task (shell, script, encoding, skill path, module). Apply matching **DO NOT** and **INSTEAD DO** directives. Skip only for pure Q&A with no repo edits. |
| Implementation hit a trap/pitfall/race | **Write:** new file in `{sharedDir}/memory/`, then `python {skillsRoot}/ws-self-learning/scripts/self_learning.py --compile` (expand tokens before shell) |
| Standard feature/bug fix, no new trap | Proof line: `Learning: N/A (standard implementation)` after confirming no new pitfall |
| Pure Q&A, no durable insight | Proof line: `Learning: N/A (no new project knowledge)` |

Task is **not done** until the completion side runs (write or explicit `Learning: N/A`) and proof includes a **`Learning:`** line. Prefer consulting MEMORY **before** inventing a new approach when the domain already has High/Critical entries.

## Pre-work consult (mandatory for mutating work)

1. Identify 3–8 keywords from the task (e.g. `bash`, `CRLF`, `launcher`, `verify.sh`, `managed skill`, `encoding`).
2. `Grep` those terms in `{sharedDir}/MEMORY.md` (tool alias `read-memory`).
3. If a hit is Severity Medium+, fold its **DO NOT** / **INSTEAD DO** directives into the plan or first edit. Do not re-discover the same failure mode.
4. For scripts/skills specifically, also apply the preflight in memory entry **Script/skill authoring preflight** (launchers, LF, Windows Python `\r\n`, no shell bridges).

## Process (write after)

1. **Analyze context** — What did we try that failed? What non-obvious constraint or pitfall did we hit?
2. **Write to `{sharedDir}/memory/`** — New file `{sharedDir}/memory/YYYY-MM-DD-[slug].md`. **ONLY** traps/pitfalls. **DO NOT** use as a ws-changelog or to record patterns an LLM already knows.
3. **Compile `MEMORY.md`** — Expand tokens, then run:
   ```bash
   python .agents/skills/ws-self-learning/scripts/self_learning.py --compile
   ```
4. **Proof + chat** — Set `**Learning:** [entry title]` or `N/A` in the final proof; one-line summary in the reply.

## Conflict Resolution

If `MEMORY.md` merge-conflicts on pull/merge, **do not** resolve by hand. Run:
```bash
python .agents/skills/ws-self-learning/scripts/self_learning.py --compile
```
This rebuilds a clean index from `{sharedDir}/memory/` (per-file entries do not conflict).

## Individual Memory File Template

```markdown
### [YYYY-MM-DD] [Topic/Component]
- **Layer**: [e.g. Core, Infrastructure, Api, Web, Tests, or N/A]
- **Module**: [e.g. Auth, Wallet, Users, or N/A]
- **Severity**: [Low, Medium, High, Critical]
- **Scenario / Context**: [When implementing X, dealing with Y, or configuring Z]
- **DO NOT**: [What specific pattern, assumption, or approach to avoid and why it fails]
- **INSTEAD DO**: [What specific correct implementation pattern or action to use instead]
```

Path tokens: [`tools.md`](../ws-shared/tools.md) § Path tokens.
