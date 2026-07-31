---














name: ws-sync-spec
description: Feature spec synchronizer — auto-updates feature specifications after prompt-driven code changes to prevent spec drift.

version: 0.0.109
invocation_names:
  - sync-spec
  - ws-sync-spec
---

# ws-sync-spec

Keep feature specifications (`*.spec.md`) continuously synchronized with real implemented code after prompts, adjustments, or task completions.

## Purpose

When code changes occur days or weeks after initial spec delivery (e.g. prompt: "adjust business logic in controller X"), existing feature specs become out-of-sync. This skill identifies affected specs, analyzes drift between spec requirements and implemented code, proposes surgical updates, and writes approved changes.

## Invocation

Standalone / Manual:
```text
/ws-sync-spec [target-spec-or-component]
```

Completion Gate / Auto-run:
Run after task completion (alongside `ws-changelog` and `ws-self-learning`).

## Steps

1. **Identify Delta Scope**:
   - Inspect `git status` / `git diff HEAD` (or recent turn prompt context) to determine modified files, classes, routes, views, controllers, or business logic.
   - **Done when:** A summary of modified code symbols, components, and logic changes is established.

2. **Discover Target Spec(s)**:
   - Search `{plansDir}/specs`, `specs/`, and `{plansDir}/specs/index.PRD` for candidate `*.spec.md` files.
   - Match modified code filenames, component names, API endpoints, or domain concepts against spec text.
   - If no spec is found, report: `"No existing spec found for modified feature."` (optionally suggest `ws-write-spec`).
   - **Done when:** Target spec file path(s) are selected or confirmed.

3. **Analyze Spec Drift & Draft Updates**:
   - Compare original spec requirements and acceptance criteria against actual implemented code logic.
   - Formulate surgical in-place edits for updated sections (e.g., Business Logic, Acceptance Criteria, API / Parameters, Data Flow).
   - Prepare a **Revision History** entry to append at the bottom of the spec (`### [YYYY-MM-DD] Revision: {brief summary of change} (Prompt: "{summarized prompt}")`).
   - **Done when:** Surgical text diff and revision entry are generated.

4. **Propose & Confirm (User Gate)**:
   - Present the proposed spec modifications and diff summary to the user in chat.
   - Prompt the user for approval before writing changes to disk.
   - **Done when:** User approves the proposed spec updates.

5. **Apply Updates & Report**:
   - Apply surgical replacements to the target spec file(s) and append the revision history log.
   - Display a summary of updated files and sections to the user.
   - **Done when:** Spec file is written to disk and confirmation report is presented in English.

## Optional Autoload Rule (Consumer `AGENTS.md`)

To automatically check for spec drift at the end of every implementation task, users can optionally add the following suggestion to their project's `AGENTS.md`:

```markdown
- **Spec Drift Sync**: At the end of implementation tasks, invoke `ws-sync-spec` to ensure feature specs (`*.spec.md`) stay synced with updated code.
```
