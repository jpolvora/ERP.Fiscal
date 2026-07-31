# Shared — Workflow Config & Consumer Data Hub

**Audience: agents** (config resolution, gates, skill loading, consumer-owned paths). Humans: install narrative in the project README or upstream [workflow-skills](https://github.com/jpolvora/workflow-skills) README.

> **Config, gates, bootstrap docs, skill loading, and consumer-owned project data** for [`ws-spec-to-pr`](../ws-spec-to-pr/SKILL.md) and [`ws-spec-to-pr-lite`](../ws-spec-to-pr-lite/SKILL.md).
>
> This folder is **not** an installable skill package. The installer copies hub templates here when a workflow or Full package is selected.
>
> Promoted utilities and harness skills live as **top-level** installable skills under `.agents/skills/<skill>/`.
>
> **Consumer-owned** (preserved on update; never overwritten by upstream):
> `config.json`, `STACK.md`, `MEMORY.md`, `memory/*`, `installed-skills.json`, and `CHANGELOG.md` (when `rules.changelogFile` points under `ws-shared/`).
> Fresh install seeds `config.json` from `config.json.example`, empty `MEMORY.md` / `CHANGELOG.md` from templates, and `STACK.md` from `STACK.md.example`. Prefer [`ws-configure-project`](../ws-configure-project/SKILL.md) to fill placeholders. The installer writes `installed-skills.json` for update/uninstall tracking.
>
> **Installer scope:** only `.agents/skills/` (skill folders + this `ws-shared/` hub). Never creates or overwrites consumer repo-root files (root `AGENTS.md`, host pointers). Optional root/host pointers stay consumer-owned; ws-check-harness may suggest them.

**Language:** en-us only for skill bodies, gates, banners, and harness docs.

---

## Config & Tools

| File | Purpose |
|------|---------|
| [`config.json.example`](config.json.example) | Project config template — seeded to `config.json` on fresh install; fill via [`ws-configure-project`](../ws-configure-project/SKILL.md); never commit |
| [`config.schema.json`](config.schema.json) | JSON Schema for `config.json` validation |
| [`config-resolution.md`](config-resolution.md) | Canonical config path + SCM resolution (dual-mode) |
| [`gates.md`](gates.md) | Shared user-gate / delivery / ship / session-model banner (dual-mode) |
| [`tools.md`](tools.md) | Canonical agent tool vocabulary (aliases → config keys), **Path tokens** (`{skillsRoot}` / `{sharedDir}` / `{plansDir}`), script launchers (`python` / `node` / `bash`). Load with `config.json` before tool calls. |
| [`STACK.md.example`](STACK.md.example) | Template for human-readable stack companion — seeds `STACK.md` |
| [`setup.md`](setup.md) | Bootstrap & entry logic shared by `ws-spec-to-pr` and `ws-spec-to-pr-lite` |
| [`MEMORY.md.template`](MEMORY.md.template) | Empty memory index template — seeds `MEMORY.md` |
| [`CHANGELOG.md.template`](CHANGELOG.md.template) | Empty ws-changelog stub — seeds `CHANGELOG.md` |
| [`skill-dependencies.json`](skill-dependencies.json) | Install graph + **`packageVersion`** + single **`upstream`** ownership block (no per-skill `upstream:` in SKILL.md) |

## Consumer-owned (local only)

| File | Purpose |
|------|---------|
| `config.json` | Project identity, stack, verification, providers (seeded; gitignored) |
| `STACK.md` | Human-readable companion to `config.json` (edit freely) |
| `MEMORY.md` | Compiled anti-regression index (`ws-self-learning` skill) |
| `memory/*.md` | Individual memory entries (compile into `MEMORY.md`) |
| `CHANGELOG.md` | Append-only history (`ws-changelog` skill; default `rules.changelogFile`) |
| `installed-skills.json` | Managed skill list for `update` / `uninstall` (installer-written) |

---

## Skill loading (mandatory)

| Skill | Path | Trigger |
|-------|------|---------|
| `ws-karpathy-guidelines` | [`../ws-karpathy-guidelines/SKILL.md`](../ws-karpathy-guidelines/SKILL.md) | Every prompt — surgical scope |
| `ws-changelog` | [`../ws-changelog/SKILL.md`](../ws-changelog/SKILL.md) | Every task completion |
| `ws-self-learning` | [`../ws-self-learning/SKILL.md`](../ws-self-learning/SKILL.md) | Before plan/code/fix: consult `{sharedDir}/MEMORY.md`; on completion: write traps → compile |

`ws-tdah` is **on-demand** here (invoke `/ws-tdah` · `/tdah` · `start ws-tdah`). Upstream root `AGENTS.md` may autoload it for development dogfood only — not the consumer default.

### Consumer root override (dual-hub)

Default **shared hub only** (typical consumer install): `ws-tdah` and `ws-senior-developer` are **on-demand** — `ws-tdah` via explicit invoke; `ws-senior-developer` via `rules.seniorDeveloper` or explicit invoke. Neither is in the mandatory autoload table above.

Some consumers add a **root** `AGENTS.md` (installer never writes it) that promotes `ws-tdah` and/or `ws-senior-developer` to per-prompt autoload. That is an **intentional consumer override**, not a shared-hub defect.

When **both** hubs load in one session, root `AGENTS.md` skill-loading and precedence sections **win** for autoload decisions over shared-hub opt-in wording here.

See also: [`setup.md`](setup.md) § External dependencies · upstream root `AGENTS.md` (dogfood example of root autoload).

### Precedence (highest first)

1. Explicit user instructions (current turn)
2. Consumer root `AGENTS.md` when present (skill loading + precedence — overrides shared-hub opt-in defaults)
3. Design / spec / architecture constraints
4. `ws-karpathy-guidelines`
5. `ws-senior-developer` when autoloaded (root hub or `rules.seniorDeveloper` set; opt out via `stop ws-senior-developer` or unset path)
6. `ws-tdah` when autoloaded (root hub or `/ws-tdah`; opt out via `stop ws-tdah` / `stop verbosity` / `normal mode`)

### Opt-out

| Phrase | Effect |
|--------|--------|
| `stop ws-tdah` / `stop verbosity` / `normal mode` | Disable ws-tdah |
| `stop ws-gabarito` / `sem ws-gabarito` | Same disable (retired alias) |
| `stop ws-senior-developer` | Disable ws-senior-developer when autoloaded |
| `/ws-tdah` · `/tdah` · `start ws-tdah` · `start ws-gabarito` | Activate (single default mode) |

---

## Promoted skills (top-level installables)

### Utilities (Workflows package)

| Skill | Path |
|-------|------|
| `ws-tdah` | [`../ws-tdah/SKILL.md`](../ws-tdah/SKILL.md) |
| `ws-karpathy-guidelines` | [`../ws-karpathy-guidelines/SKILL.md`](../ws-karpathy-guidelines/SKILL.md) |
| `ws-fable-method` | [`../ws-fable-method/SKILL.md`](../ws-fable-method/SKILL.md) |
| `ws-fable-domain` | [`../ws-fable-domain/SKILL.md`](../ws-fable-domain/SKILL.md) |
| `ws-spec-format` | [`../ws-spec-format/SKILL.md`](../ws-spec-format/SKILL.md) |
| `ws-classify-complexity` | [`../ws-classify-complexity/SKILL.md`](../ws-classify-complexity/SKILL.md) |
| `ws-configure-project` | [`../ws-configure-project/SKILL.md`](../ws-configure-project/SKILL.md) |
| `ws-goal-loop` | [`../ws-goal-loop/SKILL.md`](../ws-goal-loop/SKILL.md) |
| `ws-self-learning` | [`../ws-self-learning/SKILL.md`](../ws-self-learning/SKILL.md) |
| `ws-changelog` | [`../ws-changelog/SKILL.md`](../ws-changelog/SKILL.md) |
| `ws-spec-index` | [`../ws-spec-index/SKILL.md`](../ws-spec-index/SKILL.md) |
| `ws-sync-spec` | [`../ws-sync-spec/SKILL.md`](../ws-sync-spec/SKILL.md) |
| `ws-senior-developer` | [`../ws-senior-developer/SKILL.md`](../ws-senior-developer/SKILL.md) |

### Harness & review (Workflows package)

| Skill | Path |
|-------|------|
| `ws-check-harness` | [`../ws-check-harness/SKILL.md`](../ws-check-harness/SKILL.md) |
| `ws-check-workflows` | [`../ws-check-workflows/SKILL.md`](../ws-check-workflows/SKILL.md) |
| `ws-secrets-leak-review` | [`../ws-secrets-leak-review/SKILL.md`](../ws-secrets-leak-review/SKILL.md) |
| `ws-fable-judge` | [`../ws-fable-judge/SKILL.md`](../ws-fable-judge/SKILL.md) |

### Extra package (optional)

Present only after Extra or Full install. If a path is missing, treat as intentional omission (not a broken route).

| Skill | Path |
|-------|------|
| `ws-write-a-skill` | [`../ws-write-a-skill/SKILL.md`](../ws-write-a-skill/SKILL.md) |
| `ws-show-harness` | [`../ws-show-harness/SKILL.md`](../ws-show-harness/SKILL.md) |

### Intentionally orch-only (not duplicated here)

Pipeline folders `ws-write-spec`…`ws-fix-pr`, `ws-goal-fix-pr`, `ws-update-plan-implementation`, orchestrators (`ws-spec-to-pr`, `ws-spec-to-pr-lite`, `ws-multi-spec`), and providers are discovered via host invoke / orch dispatch — not listed as promoted utilities.

Install packages and dependency map: upstream `bin/skill-dependencies.json` in [workflow-skills](https://github.com/jpolvora/workflow-skills) (not vendored in consumer clones).

---

## Task router (consumer)

| Intent | Load |
|--------|------|
| Spec → PR E2E | `ws-spec-to-pr` |
| Spec → PR lite | `ws-spec-to-pr-lite` |
| Batch spec delivery | `ws-multi-spec` |
| Project spec index init/sync/promote | `ws-spec-index` |
| Auto-update feature specs after code changes | `ws-sync-spec` |
| Fable Method 7-step loop | `ws-fable-method` |
| Classify spec pipeline complexity | `ws-classify-complexity` |
| Adversarial audit / fraud scan | `ws-fable-judge` |
| Domain adapters (DevOps/Data/Research) | `ws-fable-domain` |
| Engineering delivery gate / Code review proof | `ws-senior-developer` (default on-demand; opt in via `rules.seniorDeveloper`; root `AGENTS.md` may autoload — see § Consumer root override) |
| Fill / update `config.json` | `ws-configure-project` |
| Audit harness | `ws-check-harness` |
| Check workflows | `ws-check-workflows` |
| Secrets / leaks | `ws-secrets-leak-review` |
| Format/review spec | `ws-spec-format` |
| Record learning | `ws-self-learning` |
| Record ws-changelog | `ws-changelog` |
| Create / rewrite a skill | `ws-write-a-skill` (Extra) |
| Show active harness | `ws-show-harness` (Extra) |

Pipeline steps 0–9: use orchestrator dispatch (do not invent alternate folder ids).

---

## Managed skills — no silent local refactors

Skills under `.agents/skills/` (except consumer-owned `ws-shared/` data) are **managed upstream copies**. `update` overwrites them.

| Context | Do | Do not |
|---------|----|--------|
| **Consumer repo / CI / Actions** | Verify a real runtime bug with evidence. If a lasting skill/script fix is needed, **tell the user to fix upstream** ([workflow-skills](https://github.com/jpolvora/workflow-skills) PR) or open that PR; local experiments are temporary only. | Autonomously reorder, “hygiene-refactor,” or rewrite managed skill scripts from a false positive (e.g. Python same-module call-before-`def` is not a `NameError`). |
| **Managed script calls** | Invoke with explicit launchers (`python` / `node` / `bash`) per [`tools.md`](tools.md) § Script launchers. On failure: report and stop. | Rewrite managed scripts for shell quirks, or invent temp scanners/bridges when a recipe fails. |
| **Agent shell scans** | Prefer `python -m py_compile` on real `*.py` paths, or a short **uncommitted** temp script if a one-liner heredoc breaks on quoting. Delete temps when done. | Commit throwaway scanners into the consumer tree, or treat shell `SyntaxError` in an embedded heredoc as a skill-script bug. |

False positives that look like “forward reference” bugs are almost always safe at Python call time. Prefer reporting + upstream suggestion over silent local churn.

---

## Cross-platform shell & encoding (Windows + Linux)

Applies to every shell call and temp script an agent creates in a consumer project, on any shell (bash, PowerShell/pwsh, cmd) and any OS (Windows, Linux).

### Python UTF-8 (mandatory)

1. **Never rely on the OS default encoding.** On Windows, Python defaults to `cp1252` for `open()` and console stdout — one non-ASCII char raises `UnicodeEncodeError` / `UnicodeDecodeError`.
2. Always pass `encoding="utf-8"` explicitly on file I/O: `open(path, "r", encoding="utf-8")` / `Path(path).read_text(encoding="utf-8")` / `write_text(..., encoding="utf-8")`.
3. Scripts that print non-ASCII: launch with `python -X utf8 script.py` (or set `PYTHONIOENCODING=utf-8` in the same command), or call `sys.stdout.reconfigure(encoding="utf-8")` near the top.
4. Prefer ASCII-only stdout in short-lived agent scripts; keep accents/non-ASCII inside files, not the console.

### Quoting (bash / PowerShell / cmd)

1. **Create script files with the host file-writing tool, never with shell redirection.** Do not build scripts via `echo "..." > file`, `Set-Content`, or heredocs containing embedded quotes/JSON — quoting rules differ per shell and corrupt content silently.
2. When a command needs nested quotes (JSON payloads, regex with quotes, multi-line Python/JS), write a short **uncommitted temp script** with the host file-writing tool, run it with an explicit launcher (`python` / `node` / `bash` per [`tools.md`](tools.md) § Script launchers), and delete it when done.
3. **bash only:** heredocs must use a quoted delimiter (`<<'EOF'`) so `$`, backticks, and quotes pass through literally. Never use heredocs in PowerShell or cmd.
4. **PowerShell:** single quotes are literal; double quotes expand `$`. Do not paste bash idioms — `&&` fails on Windows PowerShell 5.1 (use `;` or one command per call), `$?` and `$(...)` have different semantics.
5. **cmd:** no single-quote protection and no multi-line input; avoid it for anything beyond simple one-liners.
6. When unsure which shell the host runs, keep each call to a single simple invocation and route complex logic through a script file (rule 2).

---

## Recommended Feature Delivery Checklist (before push / ship)

### Consumer Projects

Run this checklist prior to triggering `/ship-pr` or shipping features in a consumer project:

- [ ] **1. Run Tests & Verification**: Execute local test commands (`verification.backendTest` / `verification.frontendTest` or project test scripts).
- [ ] **2. Harness & Workflow Audit**: Run `ws-check-harness` / `ws-check-workflows` to ensure 0 critical findings.
- [ ] **3. Configure & Verify Project**: Verify `.agents/skills/ws-shared/config.json` settings and stack definitions.
- [ ] **4. Clean Docs & Artifacts**: Ensure documentation files have no merge conflict markers or uncommitted scratch files.
- [ ] **5. Ship via `ship-pr`**: Execute `/ship-pr` (runs Prepare Board, commits, pushes, creates PR).

### Upstream Maintainers (`jpolvora/workflow-skills` source repo only)

Additional obligations when maintaining and releasing the upstream skills package:

- [ ] **1. Run Package Tests**: Execute `npm run test` (runs installer, integrity checks, and tree verification).
- [ ] **2. Single Version Bump**: Increment package version once per release PR and stamp site footer (`npm run build-site:bump`).
- [ ] **3. Regenerate Integrity Manifest**: Run `npm run generate-integrity` and `npm run verify-integrity`.
- [ ] **4. Harness Audit**: Run `ws-check-harness` to ensure 0 critical findings.
- [ ] **5. Hub Sync**: Keep `AGENTS.md` and `.agents/AGENTS.md` in sync before shipping PR.

---

## Skill discovery (consumers)

Installed skills live at `.agents/skills/<name>/SKILL.md`. Load on demand from orchestrator dispatch, task intent, or host skill discovery. There is **no** packaged `.agents/AGENTS.md` in consumer projects; **this file** is the consumer-facing hub. A thin root `AGENTS.md` (when the consumer adds one) should point here — installer never writes it.

---

## External dependencies

Not shipped in the hub package (except where noted). Resolve each dependency in **order** (first match wins). Read paths from `config.json` when present. Do **not** assume host-private rule folders.

| Dependency | Resolve (first match) |
|------------|------------------------|
| `senior-developer` | `config.json` → `rules.seniorDeveloper` (set path to opt in; default on-demand in shared hub) → local skill (`senior-developer/SKILL.md`) → global/user skill. Root `AGENTS.md` may autoload — see § Consumer root override |
| `ws-karpathy-guidelines` | `config.json` → `rules.karpathyGuidelines` → shipped `../ws-karpathy-guidelines/SKILL.md` → global skill |
| Stack companion | `config.json` → `rules.stackFile` (default `.agents/skills/ws-shared/STACK.md`) — consumer-owned under `ws-shared/` |
| Changelog file | `config.json` → `rules.changelogFile` (default `.agents/skills/ws-shared/CHANGELOG.md`) |
| Domain glossary | `config.json` → `domain.glossaryFile` (often `CONTEXT.md`) — consumer root, optional |
| Optional consumer rules | Other `config.json` `rules.*` paths when set — do not invent filenames |
| Workflow artifacts | `config.json` → `plans.dir` (token `{plansDir}`; default `.agents/plans`) · `plans.specsDir` (default `.agents/specs`) · optional `reviews.dir` (default `.agents/codereviews`) |

Bootstrap notes: [`setup.md`](setup.md). Config resolution: [`config-resolution.md`](config-resolution.md).

### Code review proof

When skills ask for **Code review proof**, use the checklist from the **resolved** `rules.seniorDeveloper` skill. Do **not** paste or duplicate that checklist here.
