---













name: ws-configure-project
version: 0.0.109
description: Project configuration wizard — detects project settings and conducts interactive interviews to populate ws-shared/config.json.

invocation_names:
  - configure-project
  - ws-configure-project
---

# ws-configure-project

Fill or refresh consumer `config.json` via detect → suggest → user-gate. Portable: no host-product names; paths use `{plansDir}` tokens after write.

**Config path:** `.agents/skills/ws-shared/config.json` (gitignored). Template: [`ws-shared/config.json.example`](../ws-shared/config.json.example). Schema: [`shared/config.schema.json`](../ws-shared/config.schema.json).

**Callers:** standalone anytime; [`ws-shared/setup.md`](../ws-shared/setup.md) bootstrap step 1; post-install when user opts in.

## Invocation

```
/ws-configure-project [--section <name>] [--detect-only] [--force]
```

| Flag | Effect |
|------|--------|
| `--section` | Only interview that top-level key (`project`, `stack`, `providers`, `verification`, `plans`, `reviews`, `rules`, `domain`, `fable`, `defaults`) |
| `--detect-only` | Print detections + suggestions; do not write |
| `--force` | Re-interview even when required fields look filled |

## Steps

1. **Ensure file** — If `config.json` missing: `cp` from `config.json.example`. If example missing, STOP (hub not installed).
   - Done when: `config.json` exists on disk (or detect-only with example readable).

2. **Detect** — Scan the consumer repo for stack, SCM, and commands. Apply heuristics in [`INTERVIEW.md`](INTERVIEW.md) § Detection. Build a suggestion map (path → value) without writing yet.
   - Done when: suggestion map covers at least `project`, `providers`/`issueTrackers`, `verification`, and `plans.dir` (defaults OK).

3. **Gap list** — Compare current `config.json` to required keys in INTERVIEW.md § Required. Mark each: filled / placeholder (`<…>` or empty) / missing.
   - Done when: gap list exists; `--force` treats filled as re-ask candidates.

4. **Interview** — For each gap (or `--section` only): user-gate with ≥2 options, **recommended = detected suggestion** first; include **Keep current** / **Skip**. Write accepted values into `config.json` after each section (or batch if user prefers). Never commit `config.json`.
   - Done when: all required gaps resolved or explicitly skipped; optional sections offered once then skippable.

5. **Stack companion** — Default `rules.stackFile` = `.agents/skills/ws-shared/STACK.md` (installer-seeded; consumer-owned). Prefer that path. Do **not** require or create a repo-root stack file.
   - If shared `STACK.md` exists but config points at a missing root file: suggest set `rules.stackFile` → `.agents/skills/ws-shared/STACK.md` (**Recommended**) / Keep current / Skip.
   - If the resolved target is missing: offer **Generate** into `.agents/skills/ws-shared/STACK.md` (setup 1b heuristics) / **Skip**. Write only under `.agents/skills/ws-shared/` unless the user explicitly chose another path.
   - Done when: config points at an existing companion, or user skipped.

6. **Validate & handoff** — Confirm JSON parses; required fields non-placeholder; print summary table (`key` → `value`). Tell caller: resume setup / run `/ws-spec-to-pr` or `/ws-spec-to-pr-lite`.
   - Done when: summary shown; `--detect-only` ends after step 2 with no write.

## Rules

- Prefer detect + suggest over blank prompts.
- Do not invent org/repo secrets; leave PAT/env keys as env-var names only.
- `providers.scm` never `local`; hybrid `active=local` + `scm=github|azure-devops` allowed.
- Artifact defaults: `plans.dir` → `.agents/plans`, `plans.specsDir` → `.agents/specs` (prefer existing repo-root `specs/`), `reviews.dir` → `.agents/codereviews`, `rules.changelogFile` → `.agents/skills/ws-shared/CHANGELOG.md` unless user picks otherwise.
- Language: en-us.
