# Setup & Bootstrap — Shared Workflow Entry

Initialization, configuration bootstrap, flags, resume logic, and first-run setup.
Shared by [`ws-spec-to-pr`](../ws-spec-to-pr/SKILL.md) and [`ws-spec-to-pr-lite`](../ws-spec-to-pr-lite/SKILL.md).

Artifact paths: [`../ws-spec-to-pr/ARTIFACTS.md`](../ws-spec-to-pr/ARTIFACTS.md). Resume rules in this file are canonical; FAQ/DIAGRAM must link here.

---

## External dependencies (guardrails)

Resolve `config.json` `rules.*` before assuming a skill or rule file exists. Full portable contract (same meaning): [`AGENTS.md`](AGENTS.md) § External dependencies in this folder · upstream root [`../../../AGENTS.md`](../../../AGENTS.md)#external-dependencies when authoring against the source repo.

| Key | Role | Resolve (first match) |
|-----|------|------------------------|
| `rules.seniorDeveloper` | Optional engineering guardrails; **Code review proof** source | config path when set (on-demand default; root `AGENTS.md` may promote autoload — see [`AGENTS.md`](AGENTS.md) § Consumer root override) → local `senior-developer` skill → global/user skill |
| `rules.karpathyGuidelines` | Surgical-change guidelines | config path → shipped `../ws-karpathy-guidelines/SKILL.md` → global skill |
| `rules.stackFile` | Human-readable stack companion | config path (default `.agents/skills/ws-shared/STACK.md`); bootstrap may create under `ws-shared/` if missing (see 1b below) — never require repo-root files |
| Other `rules.*` | Optional consumer rules (e.g. `efMigrations`, `viewPatterns`) | Use path from config when set; do not invent filenames |

**Code review proof:** When pipeline / utility skills ask for proof, load the checklist from the **resolved** `rules.seniorDeveloper` skill. Do not paste that checklist into hub docs. For autoload vs on-demand behavior, see [`AGENTS.md`](AGENTS.md) § Consumer root override.

---

## Bootstrap & Entry

> **[ws-spec-to-pr]** — Before Step 0.
> **[ws-spec-to-pr-lite]** — Before Step 0.

### Entry matrix (both orchestrators)

Same entry paths for **standard** and **lite**. Resolve provider from `config.json` `providers.active` (or legacy `issueTrackers.*.enabled` inference). Canonical spec write: `{us-dir}/step-00-{slug}.spec.md`.

| Input | Provider / skill | Step 0 action |
|-------|------------------|---------------|
| GitHub `{n}` / `US {n}` | [`ws-github-provider`](../ws-github-provider/SKILL.md) `fetch-to-spec` | Fetch issue → spec; `slug: us-{n}` |
| ADO `{org}/{project}#{id}` / `ADO {id}` / `WI {id}` | [`ws-azure-devops-provider`](../ws-azure-devops-provider/SKILL.md) `fetch-to-spec` | Fetch WI → spec |
| Hand-written `*.spec.md` (any path) | [`ws-local-spec-provider`](../ws-local-spec-provider/SKILL.md) `fetch-to-spec` | Register / normalize → canonical spec |
| Free-text feature description (no spec) | `ws-write-spec` (standard or lite) | Brainstorm → `step-00-{slug}.spec.md` |
| Plain text in invocation (no issue id, no `*.spec.md` path) | `ws-write-spec` | Same as free-text row |

Optional mirror: `{specs-dir}/{slug}.spec.md` for human browsing. Downstream skills **always** read `step-00-{slug}.spec.md` under `{us-dir}`.

1. **Config check**: Check if `.agents/skills/ws-shared/config.json` exists (fresh install normally seeds it from `config.json.example`).
   - If missing: `cp .agents/skills/ws-shared/config.json.example .agents/skills/ws-shared/config.json`.
   - Load path tokens early ([`tools.md`](tools.md) § Path tokens): `pathTokens.skillsRoot` / `sharedDir` (defaults `.agents/skills` / `.agents/skills/ws-shared`) plus `{plansDir}` ← `plans.dir`. Expand braces before Read/Grep/Shell.
   - User-gate: **Configure now (Recommended)** / **Skip**.
   - If **Configure now** (or config exists but required fields are placeholders/`<…>` / empty): load and run [`ws-configure-project`](../ws-configure-project/SKILL.md) (same session). Pass `--section` only when fixing one area mid-workflow.
   - If **Skip**: continue with example defaults; warn that providers/verification may be wrong until ws-configure-project runs.
1b. **Stack file bootstrap**: Read `config.json.rules.stackFile` (default: `.agents/skills/ws-shared/STACK.md`). Prefer ws-configure-project step 5 when that skill just ran. If config still points at a missing root `STACK.md`/`stack.md` while `.agents/skills/ws-shared/STACK.md` exists, set `rules.stackFile` to the shared path (no root file required). Else `Shell` `test -f {stackFile}`. If missing:
   - Auto-detect the project stack by scanning the repository:
     - **Language/Framework**: Look for `package.json` (Node/React/Next), `*.csproj`/`*.sln`/`*.slnx` (.NET), `pyproject.toml`/`requirements.txt` (Python), `go.mod` (Go), `Cargo.toml` (Rust), `pom.xml`/`build.gradle` (Java), `Gemfile` (Ruby), etc.
     - **Frontend framework**: Check `package.json` `dependencies` for `next`, `react`, `vue`, `angular`, `svelte`, `vite`, `tailwindcss`, etc.
     - **Backend framework**: Check for `express`, `fastify`, `nest`, `django`, `flask`, `fastapi` in matching config files.
     - **Database**: Check for `prisma/schema.prisma`, `drizzle.config.*`, `Migrations/`, `alembic/ini`, EF Core migrations, `docker-compose.yml` (PostgreSQL, MySQL, MongoDB images).
     - **Project structure**: List top-level directories (`src/`, `web/`, `tests/`, `app/`, `lib/`, `cmd/`, etc.) and infer conventional layers.
     - **Tool versions**: `node --version`, `dotnet --version`, `python --version`, `go version` (if installed).
     - **Build/test commands**: Check `package.json` `scripts` (`build`, `test`, `lint`, `dev`), `Makefile` targets, existing CI configs (`.github/workflows/`, `.gitlab-ci.yml`).
   - Generate companion from the detected information using [`STACK.md.example`](STACK.md.example) as format reference.
   - Write to `.agents/skills/ws-shared/STACK.md` (or the resolved `rules.stackFile` when it already lives under `.agents/skills/ws-shared/`). Do **not** create a repo-root stack file.
   - If auto-detection is incomplete or ambiguous (multiple possible stacks), present findings to the user and ask for clarification on uncertain items.
   - Log: `stack companion bootstrapped: {stackFile}`.
2. **Parse flags**: `auto`, `dry-run`, `skip-testing`, `skip-tests`, `skip-gates`, `full`, `strict`, `score-and-refine` (aliases: `analyze-second-pass`, `score-refine`, `scoreAndRefine`).
   - **Combined Switches:** These switches can be used individually or combined in any configuration (e.g. `full` + `auto` + `dry-run` to run a fully automated dry-run simulation of the entire workflow for testing).
   - Map: `skip-testing` → `skipTesting: true`; `skip-tests` → `skipTests: true`; `skip-gates` → `skipQualityGates: true`; `score-and-refine` / `analyze-second-pass` / `score-refine` / `scoreAndRefine` → `scoreAndRefine: true`.
   - When `skipQualityGates` is true (flag or `config.json` → `invariants.skipQualityGates`), quality gates are bypassed (classifier enforcement, fable quality visibility except `auditVerdictsBlockShip` + REFUTED, pre-advance CI, telemetry soft gates). Build, test, security, SCM, and HS-1..HS-4 still run.
   - Set `currentModel` from the **executing session model** (agent identity / runtime). If unknown → `unknown`.
   - Do **not** accept `--model` or `--model-chain` (removed). If the raw invocation still contains them, ignore and note once in the init banner: `model flags ignored — use Pause → switch model in IDE/agent host → Resume`.
   - Do **not** store or apply `modelChain`.
   - `strict` → full US verification at Step 5 (standard orch only).
2a. **Gate contract**: Load [`gates.md`](gates.md) — universal step controls, combined delivery + ship gate at standard Step 8 / lite Step 4, separate fix-PR at standard Step 9 / lite Step 5. Config/SCM: [`config-resolution.md`](config-resolution.md).
2b. **Mode hint (new workflow only):** If user did not pass density flags and invoked full `ws-spec-to-pr` without `--full`/`auto`, optionally offer once: **Full pipeline** (rec) / **Use lite instead** (`/ws-spec-to-pr-lite`) — see gates.md Mode selection. Skip when already on lite.
3. **Log parsed args and switch states**: Write a banner to step output showing all switches and their resolved values:
   ```markdown
   ### Init — Parsed args
   Raw invocation: `{raw args from user}`
   
   | Switch | Resolved |
   |--------|----------|
   | `autoMode` | `{true/false}` |
   | `dryRun` | `{true/false}` |
   | `fullMode` | `{true/false}` |
   | `scoreAndRefine` | `{true/false}` |
   | `skipTesting` | `{true/false}` |
   | `skipTests` | `{true/false}` |
   | `skipQualityGates` | `{true/false}` |
   | `currentModel` | `{session model}` |
   | `slug` | `{slug}` |
   | `workflowId` | `{workflow-id}` |
   | `branch` | `{branch}` |
   | `baseBranch` | `{baseBranch}` |
   ```
   Write this block immediately after flag parsing, before auto-resume. Applies in all modes (normal, auto, dry-run). In `dryRun`, prefix with `[DRY-RUN]`.
4. **Auto resume** or **Active Resume** (see [Resume / reset](#resume--reset)).
5. **Identity**: `workflow-id`, `slug`, `us-dir`.
   - **[ws-spec-to-pr]**: Inject `workflowType: standard` into the initialized frontmatter of `{us-dir}/{workflow-id}.state.md`.
   - **[ws-spec-to-pr-lite]**: Inject `workflowType: lite`.
6. **Baseline**: `git status --porcelain` → `preExistingDirty[]`; `git rev-parse HEAD` → `baselineCommit`.
7. **LOC baseline**: `Shell` capture → `telemetry.loc.baseline`. Store ISO → `telemetry.workflowStartedAt`.
8. **Checkpoint**: tag `uswf/{workflow-id}/before-step-0`.
9. **Progress Board** render.
10. **Step Entry Gate** → dispatch.

---

## Resume / Reset

**Auto:** skip Active Resume; use auto resume policy. If existing `active`/`paused` workflow matches same US/slug + `autoMode` + matching `workflowType`, auto-resume.

**Normal — workflow discovery (mandatory before any new workflow):**

1. `Glob` `{plansDir}/**/*.state.md` (`{plansDir}` ← `config.plans.dir`) → list all state files.
2. For each, `Read` frontmatter YAML: `status`, `workflowId`, `slug`, `us`, `currentStep`, `startedAt`, `autoMode`, `workflowType`.
3. Filter by `workflowType` match (`standard` vs `lite`).
   - **Completed Workflow Check:** If an existing workflow matches the target US/slug and has `status: completed` (or all steps finished):
     Prompt via `user-gate`:
     ```text
     Completed workflow state detected for US {us} ({slug}):
     
     Options:
     1. Run Score & Second Pass (score-and-refine) mode to analyze task scores and refine implementation (Recommended)
     2. Restart workflow from zero (overwrite/new run)
     3. View completed results & exit
     ```
     Choosing Option 1 sets `scoreAndRefine: true` and dispatches Score Analysis / 2nd Pass execution.
   - **Unfinished Workflow Check:** Filter `status: active` or `status: paused`. Present as **selectable list** via user-gate:
     ```text
     Found {N} unfinished workflow(s):
     
     1. US {us} — {slug} — Step {currentStep} — started {startedAt} — [{autoMode ? 'AUTO' : 'normal'}] (Recommended)
     2. US {us} — {slug} — Step {currentStep} — started {startedAt} — [{autoMode ? 'AUTO' : 'normal'}]
     
     Options:
     - Resume workflow #1 (Recommended)
     - Resume workflow #2
     - Start new workflow from zero
     - Cancel for now
     ```
   - **Non-Existent State:** If no matching completed or unfinished workflow exists, start fresh from **Zero** (Step 0).
4. Resume: load state, `status: active`, skip bootstrap, jump to `currentStep` gate.
5a. **Session model refresh (mandatory on every resume):** Re-read the executing session model → update `currentModel`. If changed vs prior frontmatter value, log `model-change | step {currentStep} | {old} → {new} | ISO` in ## Gate history. Ignore leftover `modelChain` keys in old state files.
6. Paused: resume at same step (checkpoint revert M=currentStep → hygiene → board → gate)..
6. Paused: resume at same step (checkpoint revert M=currentStep → hygiene → board → gate).
7. No unfinished workflows: skip list, proceed to bootstrap.

**Full reset:** Checkpoint Revert M=1 → gate **Start again** (new workflow-id) / **Exit**.

**Stale state cleanup** (new workflow start or explicit):
- Starting new while old exist → offer: **Archive old workflows** (move to `{workflow-id}.archive/`) / **Delete** / **Keep both**.
- Stale = `status: active|paused` and `startedAt` older than 7 days → flag `[STALE]`.
