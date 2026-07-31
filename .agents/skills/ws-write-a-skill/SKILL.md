---













name: ws-write-a-skill
description: Skill authoring & optimization protocol — guides the creation, editing, structural formatting, and progressive-disclosure tuning of agent skills.

version: 0.0.109
invocation_names:
  - ws-write-a-skill
  - write-a-skill
---

# ws-write-a-skill

Root virtue: **predictability** (same process every run, not the same tokens). Bold terms → [`GLOSSARY.md`](GLOSSARY.md).

## Steps

1. **Gather** — Ask: domain/task, use cases/branches, scripts needed?, reference materials?
   - Done when: scope and invocation choice (model vs user) are agreed.

2. **Consult MEMORY** — Grep `{sharedDir}/MEMORY.md` for skill/script traps (launchers, CRLF, encoding, managed-skill edits). Apply Solutions before drafting scripts.
   - Done when: relevant entries noted or none found.

3. **Decide invocation** — Model-invoked (keep `description`, pay **context load**) only if the agent or another skill must reach it. Otherwise user-invoked (`disable-model-invocation: true`, human-facing one-line description).
   - Done when: invocation mode is set and justified.

4. **Draft** — Create `skill-name/SKILL.md` (plus scripts/reference files only when earned). Prefer **steps** with checkable **Done when** criteria; push rare detail behind a **context pointer**. Recipes must use explicit `python` / `node` / `bash` launchers ([`tools.md`](../ws-shared/tools.md) § Script launchers). `.sh` files: LF only. Frontmatter `version:` must match package `package.json` / `{sharedDir}/skill-dependencies.json` → `packageVersion`. Do **not** put per-skill `upstream:` — ownership lives once in `skill-dependencies.json` → `upstream`.
   - Done when: frontmatter + body exist; every step has a Done when; description matches mode (triggers if model-invoked); version matches package.

5. **Prune** — Single source of truth; cut **no-ops**, **duplication**, **sediment**; hunt **leading words**; collapse synonym **branches** in the description.
   - Done when: checklist below passes.

6. **Review with user** — Coverage, clarity, detail level.
   - Done when: user accepts or requests a specific edit.

## Folder layout

```
skill-name/
├── SKILL.md        # required (prefer ≤100 lines)
├── evals/
│   └── evals.json  # recommended — 2–3 prompts + assertions per agentskills.io
├── GLOSSARY.md     # or REFERENCE.md — disclosed reference
├── EXAMPLES.md     # optional
└── scripts/        # deterministic helpers only (when earned)
```

## Description (model-invoked)

Third person · max 1024 chars · front-load the **leading word** · one trigger per **branch** · no body identity.

Good: `Extract text and tables from PDFs, fill forms, merge docs. Use when working with PDFs, forms, or document extraction.`

Bad: `Helps with documents.`

## When to add scripts / split

- Scripts: deterministic ops, repeated codegen, explicit error handling.
- Split / disclose: SKILL.md >100 lines, rare branches, distinct domains.

## Review checklist

- [ ] Description mode correct (triggers if model-invoked; human one-liner if user-invoked)
- [ ] SKILL.md ≤100 lines or excess disclosed
- [ ] Every step has checkable Done when
- [ ] No time-sensitive facts; consistent terms; one-level-deep pointers
- [ ] Failure modes checked: premature completion, duplication, sprawl, no-op, negation
- [ ] MEMORY consulted; scripts use explicit launchers; `.sh` is LF

## Diagnose (edit existing skills)

Load [`GLOSSARY.md`](GLOSSARY.md). Apply levers: information hierarchy, progressive disclosure, leading words, pruning. Prefer sharpening completion criteria before splitting by sequence.
