---





name: ws-classify-complexity
description: Pipeline complexity classifier — analyzes a spec against config.json dagThresholds and recommends ws-spec-to-pr-lite or ws-spec-to-pr (standard).
version: 0.0.109
invocation_names:
  - classify-complexity
  - ws-classify-complexity
---

# ws-classify-complexity

Analyze a feature spec and recommend **`lite`** or **`standard`** pipeline (`workflowType`) by comparing counted metrics to `config.json` → `dagThresholds`. Writes `{us-dir}/step-00-{slug}.classify.md`.

**Language:** en-us only. **Harness-neutral:** use portable aliases from [`../ws-shared/tools.md`](../ws-shared/tools.md).

## Orthogonality to Complexity gate

This skill recommends **orchestrator choice** (`lite` | `standard`). It is **orthogonal** to the full-orch **Complexity gate** in [`../ws-shared/gates.md`](../ws-shared/gates.md) (`simple` | `standard` | `complex`), which runs before Step 1 and controls whether Steps 1–2–3 are skipped.

| Axis | Values | When | Purpose |
|------|--------|------|---------|
| **Pipeline classifier** (this skill) | `lite` \| `standard` | Step 0 (after spec exists) or `--classify` | Pick `ws-spec-to-pr-lite` vs `ws-spec-to-pr` |
| **Complexity gate** ([`gates.md`](../ws-shared/gates.md)) | `simple` \| `standard` \| `complex` | Full orch before Step 1 | Skip or enforce plan / interview / DAG steps |

Do not merge the two axes. A `lite` recommendation does not imply `simple`; a `standard` recommendation does not imply `complex`.

## Invocation

Standalone:

```
/classify-complexity <spec-path> [--score-analysis <path>]
```

Workflow (Step 0): orchestrator runs after `step-00-{slug}.spec.md` exists and **before** advancing to Step 1.

| Parameter | Default | Notes |
|-----------|---------|-------|
| `<spec-path>` | required | Canonical `{us-dir}/step-00-{slug}.spec.md` |
| `--score-analysis` | none | Pass 1 artifact `step-05-{slug}.score-analysis.md` (AC6; unavailable at first Step 0 classify) |
| `--output-dir` | spec parent dir | Override classify.md destination |

Script (explicit launcher per `tools.md`):

```bash
node {skillsRoot}/ws-classify-complexity/scripts/classify.cjs <spec-path> [--output-dir <dir>] [--score-analysis <path>]
```

## Steps

1. **Resolve paths** — Expand `{skillsRoot}`, `{sharedDir}`, `{plansDir}` from [`config.json`](../ws-shared/config.json) (or example). Input spec must exist; script exits non-zero if missing.
   - Done when: spec path and slug are known.

2. **Run classifier** — Invoke `classify.cjs`. It:
   - Parses spec frontmatter (manual mini-parser; no npm YAML deps)
   - Counts sections (`##`), acceptance criteria (`AC\d+` / Acceptance Criteria bullets), unique backtick path refs, and layer signals (spec `### Layer:` headings + configured `stack.backend.layers`)
   - Loads `dagThresholds` from `{sharedDir}/config.json`, falling back to `config.json.example`
   - Optionally reads `--score-analysis` when Pass 1 scores exist (`scoreAndRefine`)
   - Writes `step-00-{slug}.classify.md` with recommendation, metrics table, threshold comparison, and reasoning
   - Done when: classify artifact exists on disk.

3. **Present user gate** — Unless `autoMode` or `--skip-gates` / `skipQualityGates` (quality gate only):

   | # | Option |
   |---|--------|
   | 1 | **Accept recommendation** (Recommended) |
   | 2 | **Override to standard** |
   | 3 | **Override to lite** |

   - `autoMode` / `user-gate-auto`: select index **0** (accept recommendation).
   - Log: `classify | recommended={lite|standard} | choice={accept|override-standard|override-lite} | ISO`.

4. **Apply choice** — Record `finalPipeline` in classify.md (or state) per gate outcome.
   - **Mid-flight advisory:** If already running `ws-spec-to-pr` and recommendation is `lite`, **stay on current orchestrator** unless the user explicitly chooses **Override to lite** (and orch supports handoff). Log `classify | lite-recommended | stay-standard-unless-override`. Never silently switch `workflowType` mid-flight.
   - Done when: `finalPipeline` is set and orch routes accordingly.

## scoreAndRefine (AC6)

| Phase | Behavior |
|-------|----------|
| **Step 0 / first classify** | Threshold-only recommendation. Classify artifact § scoreAndRefine = `deferred (Pass 1 scores unavailable at Step 0)`. |
| **After Pass 1** | Orchestrator may re-invoke with `--score-analysis {us-dir}/step-05-{slug}.score-analysis.md`. Script adds distribution (mean, variance, low-score clusters) and may adjust **advisory** recommendation. |
| **Mid-flight** | Updated recommendation is **advisory only** unless user re-gates. Do not silently change `workflowType`. |

Heuristic (when scores present): uniform high scores (mean ≥ 8, low variance) may reinforce `lite`; high variance or low-score clusters bias toward `standard`.

## Output artifact

`{us-dir}/step-00-{slug}.classify.md` — runtime artifact (not Step 8 delivery stage set). Registered in [`ARTIFACTS.md`](../ws-spec-to-pr/ARTIFACTS.md) when wired.

## Threshold source

[`config.json`](../ws-shared/config.json) → `dagThresholds` (defaults in [`config.json.example`](../ws-shared/config.json.example)):

| Key | Default | Compared metric |
|-----|---------|-----------------|
| `maxImplementationSteps` | 3 | AC / requirement count |
| `maxExpectedFiles` | 6 | Unique backtick path references |
| `maxLayers` | 2 | Spec layer headings + configured stack layers |

**Recommend `lite`** when **all** metrics are within limits; otherwise **`standard`**.

## Related skills

- [`ws-multi-spec`](../ws-multi-spec/SKILL.md) — batch Smart Flow prefers this skill (or live `dagThresholds`) for lite vs standard selection.
- [`ws-plan-to-tasks`](../ws-plan-to-tasks/SKILL.md) — DAG sequential detection uses the same `dagThresholds` for plan size.
- [`ws-spec-to-pr`](../ws-spec-to-pr/SKILL.md) / [`ws-spec-to-pr-lite`](../ws-spec-to-pr-lite/SKILL.md) — orchestrators wire Step 0 classify and `--classify` flag.

## step-output

Return YAML:

```yaml
step-output:
  status: success|failed
  recommendedPipeline: lite|standard
  finalPipeline: lite|standard
  classifyPath: "{us-dir}/step-00-{slug}.classify.md"
  notes: "..."
```
