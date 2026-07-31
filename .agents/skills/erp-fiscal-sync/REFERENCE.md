# ERP.Fiscal Sync — reference

Disclosed detail for [`SKILL.md`](SKILL.md). Load when resolving roots, building the checklist, commit-scanning, adapting, or verifying.

## Markers

A directory is an **ERP.Fiscal lib root** when **any** strong marker holds:

| Marker | Strength |
|--------|----------|
| `ERP.Fiscal.slnx` (or `ERP.Fiscal.sln`) at that directory | strong |
| `src/ERP.Fiscal.Abstractions/` **and** `src/ERP.Fiscal.PlugNotas/` | strong |
| Folder name `ERP.Fiscal` or `ERP.Fiscal.sync` containing the above | strong |

**Role:**

- **canonical** — lib markers at the **git / workspace root**.
- **vendored** — lib markers **nested** under a larger consumer tree.

**Search order** when `libPath` is absent and `peer` is a consumer root:

1. `{peer}/ERP.Fiscal`
2. `{peer}/ERP.Fiscal.sync`
3. `{peer}/vendor/ERP.Fiscal`, `{peer}/lib/ERP.Fiscal`, `{peer}/modules/ERP.Fiscal`
4. Recursive locate of `ERP.Fiscal.slnx` / `src/ERP.Fiscal.Abstractions` under `peer` (depth-capped; prefer shallowest strong match)
5. If multiple strong matches: list them and ask — do not pick silently

## Direction

| Value | Source | Target | Intent |
|-------|--------|--------|--------|
| `promote` | vendoredRoot | canonicalRoot | Land consumer-side lib fixes into the canonical library |
| `pull` | canonicalRoot | vendoredRoot | Refresh the nested copy from canonical |

Aliases: `vendor-to-canonical` → `promote`; `canonical-to-vendor` → `pull`.

## Scope (include)

| Include | Notes |
|---------|--------|
| `src/**` | Abstractions/PlugNotas and future lib projects |
| `test/**` | Unit tests for the lib |
| `common.props`, `nuget.props`, `Directory.Build.props`, `Directory.Build.targets` | When present at lib root |
| `*.slnx`, `*.sln` | Solution entry at lib root only |
| `src/**/*.csproj`, `test/**/*.csproj` | Project files |

Out of default scope (gate expansion only): `docs/`, `.github/`, `scripts/`, `.agents/`, consumer-only files.

## Excludes (always)

- `bin/`, `obj/`, `.git/`, `node_modules/`, `.vs/`, `TestResults/`
- `*.user`, `*.pfx`, `*.p12`, `*.pem`, `*.key`
- `.env`, `.env.*`, `appsettings.*.local.json`, `secrets.json`
- `.tmp*`, caches, live API capture fixtures

## Content comparison (checklist input)

Primary signal = **file content**, not mtime:

```bash
# Per-file (preferred for checklist rows)
git diff --no-index -- "source/rel" "target/rel"
cmp -s "source/rel" "target/rel" && echo identical

# Tree scan (then drill into modifies)
diff -ru --exclude=bin --exclude=obj --exclude=.git source/src target/src
diff -ru --exclude=bin --exclude=obj source/test target/test
```

For each **modify**, skim the unified diff and record: types/methods added-removed-changed → checklist `hunks` + `feature`.

## Commit-scan

Run from the **git top-level** that owns each lib root (vendored may be a subfolder of a consumer repo).

```bash
# Window: prefer --since=30.days if since unset; also cap with -n 20
git -C "$REPO" log -n 20 --since=30.days --oneline -- "LIB_REL/src" "LIB_REL/test"

# Detail for a hot path
git -C "$REPO" log -n 10 -p --follow -- "LIB_REL/src/.../SomeFile.cs"
```

Use commit subjects/bodies to:

1. Confirm intent of content deltas (bugfix vs WIP vs consumer-only experiment).
2. Surface source-only commits not yet reflected in target content.
3. Flag target commits that must be preserved via **adapt** (not blind overwrite).

If a path is outside git (rare): note `commit-scan: unavailable` and rely on content diff only.

## Adapt rules

| Situation | Action |
|-----------|--------|
| Target missing file; source has lib-neutral code | `copy` |
| Both differ; same public API intent | Prefer source on `promote` / source on `pull`, unless target has extra hardening — then **adapt** merge |
| Source references consumer aggregates / EF / localized `Nfe:*` | On `promote`: strip or rewrite to neutral types; else `skip` and report |
| Target-only file with no source counterpart | `delete?` only with explicit user confirm; default keep |
| New public method/type without test | Add checklist row under `test/**` → implement in step 6–7 |

## Verify + tests

On **target** after apply:

```bash
dotnet build ERP.Fiscal.slnx
dotnet test ERP.Fiscal.slnx --no-build
# Or filter: dotnet test test/ERP.Fiscal.PlugNotas.Tests/...
```

Add tests when promote/pull introduces parsers, classifiers, helpers, or provider branches without coverage. Prefer existing patterns: `FakeHttpMessageHandler`, JSON fixtures, InternalsVisibleTo.

## Promote hygiene

1. Copy only lib-neutral code (contracts, providers, parsers, Abstractions DTOs).
2. Stop and report consumer-bound references ([AGENTS.md](../../../AGENTS.md) lib boundary).
3. Surgical checklist over whole-tree mirrors.

## Example invocations

```text
/erp-fiscal-sync direction=promote peer=../MyErp
/erp-fiscal-sync direction=pull peer=/path/to/ERP.Fiscal.canonical since=14.days
/erp-fiscal-sync direction=promote libPath=../MyErp/modules/ERP.Fiscal
```
