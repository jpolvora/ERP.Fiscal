# AGENTS.md — Packaged Skills Index

**Audience: agents** (consumer skill index + portability rules).  
**Humans:** install and contribute via the upstream root [`README.md`](../README.md). Upstream agent hub: root [`AGENTS.md`](../AGENTS.md).

This folder ships skills for consumers (`npx --yes github:jpolvora/workflow-skills` or local CLI). Skills under `.agents/skills/` support project workflows (`spec-to-pr` / lite + pipeline) and the `test/` install dry-run tree.

This file is the **packaged routing index** after install — not a human install guide.

> **Source hub:** Root [`AGENTS.md`](../AGENTS.md) owns layers, skill loading, verification, and site catalog for the upstream repo. Prefer *this* file for what ships under `.agents/skills/` in consumers.

> **Drift check:** After add/remove/rename under `.agents/skills/`, update **both** root `AGENTS.md` and this packaged index (and regenerate the site when routing/layers change).

> **Doc roles:** `AGENTS.md` / this file = agent contracts. `README.md` = human install/UX. Keep facts aligned; do not put install walkthroughs here.

**Language:** Skill content and pipeline output = **en-us**.

---

## Rules for skills under `skills/` (`.agents/skills/*`)

These rules apply to **every** skill shipped in this package (pipeline, providers, review, utility). They mirror the portability and integrity expectations enforced by [`check-harness`](skills/check-harness/SKILL.md).

### Portability and genericity (mandatory)

1. **Portable and project-agnostic** — Skills must work in any consumer repo. Do **not** hardcode org/repo names, solution filenames, API hosts, tenant fields, or stack-specific build/test commands inside skill bodies or scripts.
2. **Customize via `config.json`** — Project identity, stack, verification commands, issue trackers, and `providers.active` / `providers.scm` live in `skills/shared/config.json` (gitignored; copy from `skills/shared/config.json.example`). Skills **read** config / `STACK.md` / `tools.md`; they do not embed consumer metadata. See [`config-resolution.md`](skills/shared/config-resolution.md).
3. **Repo-root-relative paths only** — References use paths like `skills/01-write-plan/SKILL.md` or `.agents/skills/...` from the consumer root. **Forbidden:** absolute paths (`C:\Users\...`, `/home/...`) or author-machine dependencies.
4. **Progressive disclosure** — Route via this index / root hub; do not paste entire skill bodies into hubs. Prefer links to the canonical skill over duplicated prose.
5. **No `name:` collisions** — Each `SKILL.md` frontmatter `name:` must be unique across the installed tree.
6. **Evidence-based harness changes** — When fixing routing/links, cite verified paths; do not invent files.
7. **Consistent skill and task folder references** — References to tasks, steps, and subagent skills in all workflow files must match the exact, prefixed skill folder names (e.g. `05-verify-plan`, `07-integration-validation`, `10-update-plan-implementation`). Unprefixed, retired, or placeholder folder references are forbidden.

### Language (mandatory)

- All skill bodies, frontmatter, scripts (user-facing strings), gates, banners, Progress Board text, and generated artifact templates: **English (en-us)**.
- No Portuguese (PT-BR) in skill content. Conversational replies to a user may follow the user’s language when the project allows it; **skill files stay en-us**.

### Upstream ownership and consumer edits

| Role | Rule |
|------|------|
| **Canonical upstream** | [`jpolvora/workflow-skills`](https://github.com/jpolvora/workflow-skills) is the authoritative source for pipeline and dependency skills. |
| **Installed copies** | Skills under `.agents/skills/` in consumer projects are **managed copies**. A plain `update` **overwrites** skill files. **Preserved:** `shared/config.json`, `shared/stack.md`, `shared/MEMORY.md`, `shared/memory/*`. |
| **Local edits** | Consumers **may** edit skills locally for experiments, but those changes **can be lost** on the next `npx --yes github:jpolvora/workflow-skills update` (or `update --include-new`). |
| **Contribute back** | Lasting improvements must be authored against the upstream repo and submitted as a **pull request** to `jpolvora/workflow-skills` (prefer `develop` → `main`). Do not treat a consumer fork of skill files as the long-term source of truth. |

### Pre-merge gate: `check-harness` (mandatory for upstream)

Before committing skill changes and before merging to **`main`** on `workflow-skills`:

1. Load and run [`skills/check-harness/SKILL.md`](skills/check-harness/SKILL.md) (Phases 0–5c scan → Phase 6 correction plan → Phase 7 only with approval).
2. The audit **must** cover at least: routing vs disk inventory, broken/relative links, absolute paths, redundancy / progressive disclosure, **portability** (no hardcoded project metadata; parameterization via `config.json` / stack docs), and **en-us** compliance.
3. Do **not** merge skill PRs to `main` while critical harness findings remain open.
4. After harness-affecting changes, also regenerate the site catalog when applicable (`node bin/build-site.js` in the upstream repo) and keep root `AGENTS.md` + this packaged index in sync (**Drift check** above).

Standalone invoke: `/check-harness` or `@check-harness` (optional `--dry-run` for report-only).

---

## Workflows

| Skill | Path | Role |
|-------|------|------|
| `spec-to-pr` | `skills/spec-to-pr/SKILL.md` | Spec → plan → implement → verify → review → integrate → PR (FSM F0–F6, steps 0–13) |
| `spec-to-pr-lite` | `skills/spec-to-pr-lite/SKILL.md` | Fast, sequential Plan → implement → review → ship PR (steps 1–5) |

### Dual-Mode Execution & Compatibility

Both workflows co-exist cleanly in **dual mode** inside consumer projects:
- **Shared Configuration**: `.agents/skills/shared/config.json` only ([`config-resolution.md`](skills/shared/config-resolution.md)).
- **Shared Gates**: [`gates.md`](skills/shared/gates.md) — prefer `AskQuestion`; markdown fallback when unavailable; slim transitions; one delivery; one ship; no re-ask inside `11-ship-pr` when `workflowMode: true`.
- **Session model**: `currentModel` from the executing session; switch via Pause → Cursor UI → Resume (no `--model` / `--model-chain`). Soft tips at F1→F2 / F3→F4 (full orch only).
- **State Isolation**: `workflowType` (`standard` / `lite`) prevents cross-resuming.
- **Pipeline Reusability**: Shared pipeline skills stay orch-agnostic and interchangeable.
- **Dispatch:** [`spec-to-pr/STEP-DISPATCH.md`](skills/spec-to-pr/STEP-DISPATCH.md) is **standard-only** (steps 0–13). Lite keeps its own Steps 1–5; do not treat STEP-DISPATCH as lite step numbers.

---

## Skill index

### Harness & infrastructure

| Skill | Path | Description |
|-------|------|-------------|
| `check-harness` | `skills/check-harness/SKILL.md` | Audit harness integrity (routing, links, redundancy) |
| `check-workflows` | `skills/check-workflows/SKILL.md` | Validate workflow FSM paths, step continuity, config sharing, and state isolation |
| `write-a-skill` | `skills/write-a-skill/SKILL.md` | Create skills with structure and progressive disclosure |

### Engineering standards

| Skill | Path | Description |
|-------|------|-------------|
| `mobile-first-design` | `skills/mobile-first-design/SKILL.md` | Responsive mobile-first design |
| `design-taste-frontend` | `skills/taste-skill/SKILL.md` | Anti-slop frontend (landing pages, portfolios, redesigns) |

### `spec-to-pr` pipeline (`00`–`11`)

| Skill | Step(s) | Path | Description |
|-------|---------|------|-------------|
| `00-write-spec` | 0 | `skills/00-write-spec/SKILL.md` | Draft canonical spec from feature description |
| `01-write-plan` | 1 | `skills/01-write-plan/SKILL.md` | Generate implementation plan from issue / spec |
| `02-interview` | 2 | `skills/02-interview/SKILL.md` | Audit and refine plan until shared understanding |
| `03-plan-to-tasks` | 3 | `skills/03-plan-to-tasks/SKILL.md` | Break plan into atomic DAG tasks |
| `04-implement-tasks` | 5, 10 | `skills/04-implement-tasks/SKILL.md` | Execute or fix code following plan/DAG |
| `05-verify-plan` | 6 | `skills/05-verify-plan/SKILL.md` | Verify deliverables against acceptance criteria |
| `06-code-review` | 9 | `skills/06-code-review/SKILL.md` | Two-phase triage + investigation local review |
| `07-integration-validation` | 11 | `skills/07-integration-validation/SKILL.md` | Pre-PR integration test battery |
| `08-fix-pr` | 13 (via ship-pr) | `skills/08-fix-pr/SKILL.md` | Resolve active PR review threads |
| `09-goal-fix-pr` | 13 (via ship-pr) | `skills/09-goal-fix-pr/SKILL.md` | Loop fix-pr until zero open threads |
| `10-update-plan-implementation` | Post-workflow | `skills/10-update-plan-implementation/SKILL.md` | Capture QA findings and apply plan deltas |
| `11-ship-pr` | 13 | `skills/11-ship-pr/SKILL.md` | End-to-end PR delivery and merge |

### Providers (platform-specific entry + PR ops)

| Skill | Path | Description |
|-------|------|-------------|
| `github-provider` | `skills/github-provider/SKILL.md` | GitHub issue→spec; auth; PR create/threads/merge (`gh`) |
| `azure-devops-provider` | `skills/azure-devops-provider/SKILL.md` | ADO work item→spec; PAT auth; PR create/threads/merge |
| `local-spec-provider` | `skills/local-spec-provider/SKILL.md` | Local `*.spec.md` detect/register; PR via configured SCM |

### Review & audit

| Skill | Path | Description |
|-------|------|-------------|
| `security-review` | `skills/security-review/SKILL.md` | Security review (OWASP, injection, XSS, auth, crypto) |
| `dotnet-security-performance-review` | `skills/dotnet-security-performance-review/SKILL.md` | C# security and performance review (login, auth, EF) |
| `tdd-sdd-ddd-reviewer` | `skills/tdd-sdd-ddd-reviewer/SKILL.md` | Architectural audit (Clean Architecture, TDD, DDD) |
| `domain-review` | `skills/domain-review/SKILL.md` | Domain / bounded-context review |
| `multi-domain-review` | `skills/multi-domain-review/SKILL.md` | Batch review of multiple domains |
| `secrets-leak-review` | `skills/secrets-leak-review/SKILL.md` | Secrets / PII / credential leak scan |

### Utility & meta (top-level installable)

| Skill | Path | Description |
|-------|------|-------------|
| `caveman` | `skills/caveman/SKILL.md` | Ultra-compressed response (~75% fewer tokens) |
| `gabarito` | `skills/gabarito/SKILL.md` | Ten operational response guidelines |
| `karpathy-guidelines` | `skills/karpathy-guidelines/SKILL.md` | Surgical changes; no scope creep |
| `spec-format` | `skills/spec-format/SKILL.md` | Create / review / format `*.spec.md` |
| `self-learning` | `skills/self-learning/SKILL.md` | Anti-regression notes in `MEMORY.md` |
| `changelog` | `skills/changelog/SKILL.md` | Summarized history in `CHANGELOG.md` |
| `goal-loop` | `skills/goal-loop/SKILL.md` | Generic convergence loop (used by `09-goal-fix-pr`) |

---

## Task router

| When to use | Skill to load |
|-------------|---------------|
| Spec → PR end-to-end | `spec-to-pr` |
| Spec → PR lite (sequential) | `spec-to-pr-lite` |
| Write a spec | `00-write-spec` |
| Plan implementation | `01-write-plan` → `02-interview` → `03-plan-to-tasks` |
| Implement / fix code | `04-implement-tasks` |
| Verify against plan | `05-verify-plan` |
| Local code review | `06-code-review` |
| Integration tests pre-PR | `07-integration-validation` |
| Fix PR review threads | `08-fix-pr` / `09-goal-fix-pr` |
| Ship / merge PR | `11-ship-pr` |
| GitHub issue→spec or GitHub PR ops | `github-provider` |
| Azure DevOps work item→spec or ADO PR ops | `azure-devops-provider` |
| Local `*.spec.md` register / normalize | `local-spec-provider` |
| Format / review a spec | `spec-format` |
| Security review | `security-review` or `dotnet-security-performance-review` |
| Secrets / leak scan | `secrets-leak-review` |
| Architecture (TDD/DDD) | `tdd-sdd-ddd-reviewer` |
| Domain review | `domain-review` or `multi-domain-review` |
| Frontend design | `design-taste-frontend` or `mobile-first-design` |
| Create a new skill | `write-a-skill` |
| Audit harness | `check-harness` |
| Validate / check workflow processes | `check-workflows` |
| Generic convergence loop | `goal-loop` |

---

## External dependencies

Portable guardrails contract for **installed** consumers (this file ships under `.agents/`). Prefer this section when the consumer root `AGENTS.md` omits `#external-dependencies`. Upstream hub copy (same contract): root [`AGENTS.md`](../AGENTS.md) § External dependencies. Bootstrap notes: [`skills/shared/setup.md`](skills/shared/setup.md).

Not shipped in the skill package (except where noted). Resolve each dependency in **order** (first match wins). Read paths from `skills/shared/config.json` when present.

| Dependency | Resolve (first match) |
|------------|------------------------|
| `senior-developer` | `config.json` → `rules.seniorDeveloper` → local skill (`senior-developer/SKILL.md`) → `.cursor/rules/senior-developer.mdc` (or equivalent path from config) → global/user skill |
| `karpathy-guidelines` | `config.json` → `rules.karpathyGuidelines` → shipped `skills/karpathy-guidelines/SKILL.md` → global skill |
| Stack companion | `config.json` → `rules.stackFile` (default `STACK.md` / `stack.md`) — consumer-owned |
| Domain glossary | `config.json` → `domain.glossaryFile` (often `CONTEXT.md`) — consumer root, optional |
| Optional consumer rules | Other `config.json` `rules.*` paths when set (e.g. `rules.efMigrations`, `rules.viewPatterns`) — do not invent filenames |
| Domain catalog | `specs/domains/` — consumer-owned |

### Code review proof

When skills ask for **Code review proof**, use the checklist / verification obligations from the **resolved** `rules.seniorDeveloper` skill (local/global `senior-developer` equivalent after the table above). Do **not** paste or duplicate that checklist here.

---

## Consumer notes

- Installed skill trees are **managed upstream copies**. Consumer-owned under `skills/shared/`: `config.json`, `stack.md`, `MEMORY.md`, `memory/` — preserved on update; skill files are overwritten.
- To refresh from upstream: `npx github:jpolvora/workflow-skills update` (add `--include-new` when new top-level skill folders appeared upstream).
- Consumers may copy or adapt this index into their own root `AGENTS.md`; keep paths relative to the install root (typically `.agents/skills/...`).
- **Dual hub (OK):** consumer root `AGENTS.md` may stay project-specific and delegate skill routing to `.cursor/rules/index.mdc` (or equivalent). This packaged `.agents/AGENTS.md` remains the **skill catalog** for the installed hub; keep both aligned after updates.
- **Do not** rely on in-place edits to pipeline skills in a consumer project for production workflows — prefer an upstream PR (see **Upstream ownership** above). In-place edits are overwritten on update.
- Before upstream merge to `main`, skill changes must pass **`check-harness`** (see **Pre-merge gate** above).
- Guardrails / External Dependencies: use **this file** § [External dependencies](#external-dependencies) (`rules.seniorDeveloper` / `rules.karpathyGuidelines` / `rules.stackFile` in config). Do not require a consumer root `AGENTS.md` section; if the root hub also documents the contract, keep both aligned.
