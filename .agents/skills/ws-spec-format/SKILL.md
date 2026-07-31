---













name: ws-spec-format
description: Specification schema & validator — defines canonical *.spec.md format, section hierarchy, and acceptance criteria rules.

version: 0.0.109
invocation_names:
  - spec-format
  - ws-spec-format
---

# ws-spec-format

Create, review, or format `*.spec.md`. Canonical format SoT — other skills **reference** this skill; they do not duplicate frontmatter/sections. Schema: [`FORMAT.md`](FORMAT.md). Language: **en-us**.

## Modes

| Mode | When | Output |
|------|------|--------|
| **create** | Feature / tracker without local spec | New canonical `*.spec.md` |
| **review** | Gaps or format drift | Gap report + proposed fixes (edit only with approval) |
| **format** | Valid content, nonstandard shape | Reformatted file or proposed diff |

Infer mode or ask. Triggers: `/ws-spec-format`, `@ws-spec-format`, create/review/format/validate phrasing.

## Review (Done when report emitted; no edit without approval)

1. Read `*.spec.md` (or `{us-dir}/`).
2. Validate frontmatter, required sections, AC quality per [`FORMAT.md`](FORMAT.md).
3. Cross-check architecture docs when present (`CONTEXT.md`, stack file, `config.json.domain`).
4. Emit check table (Frontmatter / Description / ACs → OK|FAIL + fix).
5. Edit only on explicit `apply fixes` / `format`.

## Create (Done when path confirmed)

1. Collect title, description, ACs (or provider fetch).
2. GitHub issue → `ws-github-provider` `fetch-to-spec`.
3. ADO WI → `ws-azure-devops-provider` `fetch-to-spec`.
4. Existing hand-written `*.spec.md` → `ws-local-spec-provider` register/normalize (no invented tracker fields).
5. Confirm canonical path with complete frontmatter + sections.

## Downstream

`ws-spec-to-pr`, `ws-write-plan`, `ws-interview`, `ws-verify-plan`, `ws-testing` read **`{us-dir}/step-00-{slug}.spec.md`** only — never live tracker APIs or `*.issue.json`. See [`ARTIFACTS.md`](../ws-spec-to-pr/ARTIFACTS.md).

Providers: [ws-github-provider](../ws-github-provider/SKILL.md) · [ws-azure-devops-provider](../ws-azure-devops-provider/SKILL.md) · [ws-local-spec-provider](../ws-local-spec-provider/SKILL.md). Hub: [`AGENTS.md`](../ws-shared/AGENTS.md).
