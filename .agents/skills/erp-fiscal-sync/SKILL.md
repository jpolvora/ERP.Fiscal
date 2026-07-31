---
name: erp-fiscal-sync
description: Sync and promote ERP.Fiscal trees between a vendored consumer copy and the canonical library (user-invoked: /erp-fiscal-sync).
disable-model-invocation: true
version: 1.1
invocation_names:
  - erp-fiscal-sync
---

# ERP.Fiscal Sync — promote / pull

Portable skill: bidirectional sync between a **vendored** ERP.Fiscal tree and the **canonical** library. Agent uses shell/git/diff/copy only (no skill scripts).

**Leading words:** promote · pull · checklist · adapt · commit-scan · verify

Parameters, markers, excludes, recipes → [`REFERENCE.md`](REFERENCE.md).

## Parameters

| Param | Required | Meaning |
|-------|----------|---------|
| `direction` | yes | `promote` = vendored → canonical · `pull` = canonical → vendored |
| `peer` | yes* | Path to the other project root |
| `libPath` | no | Explicit ERP.Fiscal tree (skips search) |
| `since` | no | Commit-scan window (default: last 20 commits / 30 days on each side) |

\*Omit `peer` only when cwd is one side and `libPath` is the other.

**Done when:** `direction` and both roots (`vendoredRoot`, `canonicalRoot`) exist on disk.

## Steps

### 1. Resolve roots

Detect cwd role via markers; resolve `peer` / `libPath`; label `canonicalRoot` vs `vendoredRoot`. Print both; ask if ambiguous.

**Done when:** `source` and `target` are set from `direction` and both paths exist.

### 2. Content inventory → checklist

Build the sync **checklist** from **byte/content comparison** (`git diff --no-index`, `cmp`, unified diff) — timestamps alone are not evidence of change.

For each relative path in scope ([`REFERENCE.md`](REFERENCE.md) § Scope), skip excludes, then add a checklist row:

| Field | Values |
|-------|--------|
| path | relative to lib root |
| status | `add` / `modify` / `identical` / `target-only` |
| hunks | short summary of method/type deltas (modify) |
| feature | inferred capability (provider, parser, contract, helper, test) |
| action | `copy` / `adapt` / `skip` / `delete?` (default; refine in step 4) |

**Done when:** checklist printed with counts (add/modify/identical/target-only); identical rows listed as skip-default.

### 3. Commit-scan

On **both** lib roots (or their containing git repos), run `git log` for paths under `src/` and `test/` within `since` ([`REFERENCE.md`](REFERENCE.md) § Commit-scan). Map commits → checklist rows (message, files, intent). Prefer commit narrative when content and mtime disagree.

**Done when:** recent commits summarized and linked to checklist paths; open questions listed.

### 4. Feature adapt pass

For each **modify** / **add** row: read source and target; decide `copy` vs **adapt** (merge API surface, keep target-only fixes, rewrite consumer-bound types out on `promote`). Update checklist `action`. Expand rows for missing tests of new public methods.

**Done when:** every non-identical row has a justified `action`; adapt notes name methods/types touched.

### 5. Gate

Show direction, roots, commit-scan highlights, and the checklist. Await approval (all / subset / abort).

**Done when:** user approved a concrete checklist subset (or aborted with no writes).

### 6. Apply

Execute approved actions: copy adds; overwrite or surgically edit modifies; create/update tests called out in the checklist. UTF-8; preserve target line endings on replace.

**Done when:** approved paths/methods match intended content; unapproved rows untouched.

### 7. Verify + tests

1. `dotnet build` on target solution.
2. `dotnet test` on target (full or filter to touched projects).
3. If coverage gaps remain for promoted public APIs: **add** focused unit tests (FakeHttpMessageHandler / parser fixtures), then re-run those tests.
4. On `promote` into canonical: [`security-check`](../security-check/SKILL.md) on the sync diff.

**Done when:** build green; tests run and reported; new tests added when gaps found; failures fixed or explicitly blocked to the user.

## Portability

Copy this folder to the consumer `{skillsRoot}/erp-fiscal-sync/`. Path parameters only — never hardcode a consumer name.
