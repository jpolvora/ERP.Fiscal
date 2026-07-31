---













name: ws-local-spec-provider
description: Local Markdown spec provider — detects, normalizes, and registers hand-written *.spec.md feature specifications into canonical pipeline artifacts.

version: 0.0.109
disable-model-invocation: true
invocation_names:
  - local-spec-provider
  - ws-local-spec-provider
---

# ws-local-spec-provider

Filesystem local-spec entry: detect/configure `plans.specsDir` (default **`.agents/specs`**; prefer existing repo-root `specs/`), register/normalize `*.spec.md` → `{us-dir}/step-00-{slug}.spec.md` with `source: local`. No remote trackers.

**PR/thread/merge:** hybrid — load `providers.scm` skill ([ws-github-provider](../ws-github-provider/SKILL.md) / [ws-azure-devops-provider](../ws-azure-devops-provider/SKILL.md)). Never no-op silently. Reject `scm: "local"`.

## Invocation

### Standalone Mode

```
/ws-local-spec-provider <intent> [args...]
```

Examples: `fetch-to-spec path/to/feature.spec.md` · `validate-auth` · `fetch-to-spec feature --mirror`. Prints `specPath` / `specsDir`.

### Workflow Mode

Orch when `providers.active=local` or input is `*.spec.md`; also `ws-write-spec` optional mirror. Records `specSource: local`, skips Step 0 → Step 1 gate.

| Parameter | Default | Notes |
|-----------|---------|-------|
| `<intent>` | required | Contract below |
| path / slug | — | Local file or slug under `specsDir` |
| `--mirror` | false | Also write `{specs-dir}/{slug}.spec.md` |

## Config

| Key | Role |
|-----|------|
| `plans.specsDir` | Specs root (omit → prefer `specs/` if present, else `.agents/specs`) |
| `plans.dir` | `{us-dir}` = `{plansDir}/{slug}/` |
| `providers.active` | `local` → this skill for `fetch-to-spec` |
| `providers.scm` | `github` \| `azure-devops` for PR intents |

Missing `specsDir`: ensure dir + write key into local `config.json` (gitignored). Do not invent repo-root `specs/` unless chosen.

| Layout | Path |
|--------|------|
| Flat | `{specs-dir}/{slug}.spec.md` |
| Nested | `{specs-dir}/{slug}/README.spec.md` or `{slug}/{slug}.spec.md` |

Canonical copy is always `{us-dir}/step-00-{slug}.spec.md`.

## Intent contract

| Intent | Behavior |
|--------|----------|
| `fetch-to-spec` | Register/normalize → canonical `source: local`; optional `--mirror`; skip Step 0 |
| `validate-auth` | `specsDir` exists/creatable; `config.json` writable when configuring |
| `create-pr` / `list-threads` / `resolve-thread` / `merge-pr` | **Delegate** to `providers.scm` |

### Provider resolution

1. Read `providers.active` / `providers.scm`.
2. Absent `providers`: enabled GitHub → github; else ADO → azure-devops; else local. Prefer GitHub if both.
3. Absent `scm`: if active is github|azure-devops → scm=active; if local → parse `project.repoUrl` host; else **STOP** for explicit `providers.scm`.

## Scripts (Done when)

```bash
python .agents/skills/ws-local-spec-provider/scripts/detect_specs_dir.py --validate   # validate-auth
python .agents/skills/ws-local-spec-provider/scripts/detect_specs_dir.py --detect [--ensure]
python .agents/skills/ws-local-spec-provider/scripts/detect_specs_dir.py --configure specs
python .agents/skills/ws-local-spec-provider/scripts/register_local_spec.py \
  --input path/to/feature.spec.md [--slug feature] [--mirror] [--force]
```

Slug: frontmatter `slug:` else basename (strip `step-00-`); nested README twin → parent dir name. Always set `source: local` + `slug`/`title`. Overwrite only if identical or `--force` / confirm. Validate sections via [`ws-spec-format`](../ws-spec-format/SKILL.md).

## Entry patterns

| Input | Action |
|-------|--------|
| `*.spec.md` path | `fetch-to-spec` → `{us-dir}` |
| Slug under `specsDir` | Resolve file → `fetch-to-spec` |
| Already canonical `step-00` | Re-normalize `source: local` if needed; skip Step 0 |

Standalone prints `specPath` / `specsDir`. Workflow records `specSource: local`, skips Step 0 → Step 1 gate. en-us; portable via `config.json` only.
