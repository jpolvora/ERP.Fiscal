---













name: ws-check-workflows
description: Workflow FSM simulation runner — validates step continuity, state isolation, provider dispatch, and artifact transitions across standard, lite, and multi-spec pipelines.
version: 0.0.109
disable-model-invocation: true
invocation_names:
  - check-workflows
  - ws-check-workflows
---

# ws-check-workflows

Deep validation and simulation skill designed to analyze, simulate, and verify the structural integrity of both the standard `ws-spec-to-pr` (steps 0–9) and sequential `ws-spec-to-pr-lite` (steps 0–5) workflow orchestrators.

---

## Quick Start

Run deep simulation and validation against both workflows:

```bash
# Standard report execution
python .agents/skills/ws-check-workflows/scripts/check_workflows.py

# Save Markdown report to ws-check-workflows-report.md
python .agents/skills/ws-check-workflows/scripts/check_workflows.py --report

# Interactive auto-fix mode (prompts for confirmation before applying)
python .agents/skills/ws-check-workflows/scripts/check_workflows.py --fix

# Non-interactive auto-fix (CI / automated runner)
python .agents/skills/ws-check-workflows/scripts/check_workflows.py --fix --yes
```

---

## Simulation & Validation Scope

The validation process performs end-to-end simulation across both orchestrators:

### 1. Workflow Simulation & Step Continuity
- **Standard (`ws-spec-to-pr`) Simulation**: Simulates stepping through Steps 0 to 9 (`ws-write-spec`, `ws-write-plan`, `ws-interview`, `ws-plan-to-tasks`, `ws-implement-tasks`, `ws-verify-plan`, `ws-code-review`, `ws-testing`, `ws-ship-pr`, `ws-fix-pr`).
- **Lite (`ws-spec-to-pr-lite`) Simulation**: Simulates stepping through Steps 0 to 5 (`ws-write-spec`, `ws-write-plan`, `ws-implement-tasks`, `ws-code-review`, `ws-ship-pr`, `ws-fix-pr`).
- **Linked Skill Check**: Verifies that every step links to an existing skill under `.agents/skills/<skill>/SKILL.md`.

### 2. Script Syntax & Execution Check
- Compiles Python scripts (`.py`) via `py_compile` and checks Node.js scripts (`.cjs`/`.js`) via `node --check`.
- Flags syntax errors, invalid imports, or execution issues as critical broken steps.

### 3. Orchestrator Dependency Closure
- Asserts that all skills dispatched by `ws-spec-to-pr` and `ws-spec-to-pr-lite` are declared in `bin/skill-dependencies.json`.

### 4. Config Sharing & State Isolation
- Verifies that all provider scripts and state handlers target `ws-shared/config.json`.
- Confirms state handlers serialize `workflowType` (`standard` vs `lite`).

---

## Report & Confirmation Flow

1. **Detailed Simulation Report**: Generates a structured breakdown of Full and Lite workflow simulation results, along with a table of detected issues and actionable **Suggested Fixes**.
2. **User Confirmation Gate**: Prompts for explicit user confirmation before applying automated fixes.
