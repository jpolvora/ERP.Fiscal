---






name: ws-senior-developer
description: Engineering delivery gate enforcing strict scope control (suggest unasked changes first), anti-reinvention/code simplification, interactive ambiguity resolution via user-gate options, and pre-ship proof.

version: 0.0.109
invocation_names:
  - senior-developer
  - ws-senior-developer
---

# ws-senior-developer

Use this optional, model-invoked delivery gate only when explicitly invoked or resolved
through `rules.seniorDeveloper`. It guides engineering work across prompting/interactive
sessions, planning phases, code reviews, and workflow steps; it does not replace a
configured project policy or an installed workflow.

## Core Engineering Directives

Always strictly observe these core AI execution rules during interactive prompting, plan drafting, implementation, and code review:

1. **Do not implement unasked changes — suggest first:**
   - Never write code for features, refactors, abstractions, or enhancements that were not explicitly requested by the user or required by the approved plan.
   - If an opportunistic improvement, potential edge case fix, or complementary feature is discovered, outline it as a recommendation first using `user-gate` (or interactive options tool) and await user approval before proceeding.

2. **Do not reinvent the wheel — audit and simplify:**
   - Prior to writing custom logic or utility functions, inspect existing codebase helpers, standard libraries, and project patterns. Reuse established utilities whenever possible.
   - In every task, evaluate whether code can be reduced, simplified, or refactored cleanly to eliminate duplication, dead code, or unnecessary complexity without changing behavior.

3. **Stop and ask on ambiguity — present options via `user-gate`:**
   - If requirements, design trade-offs, architecture choices, or forward execution paths are ambiguous or underspecified, stop execution immediately.
   - Formulate explicit options detailing trade-offs, pros/cons, and a recommended path, then present them via `user-gate` so the developer can make an informed decision on how to proceed.

## 1. Route existing intent

If the request names a workflow command, dispatch it through that workflow without adding
a competing gate. If it explicitly requests implementation, use the installed implementation
capability under `{skillsRoot}` and apply this skill only as its requested delivery constraint.

**Done when:** the applicable workflow or implementation route is identified before review
or planning requirements are imposed.

## 2. Classify scope

Treat a trivial or single-file change as exempt from plan ceremony; apply only the focused
checks needed for its risk. For multi-file or multi-modification free-text work, continue
to context and planning.


Require a confirmed plan before multi-file or multi-modification free-text work. Use
`{plansDir}` for plan artifacts, and route applicable work to installed workflow,
specification, and specification-sync capabilities.

During plan confirmation:
- Enforce Core Directive 1: Ensure the plan contains only requested scope; present optional enhancements as distinct suggestions.
- Enforce Core Directive 3: Use `user-gate` when confirmation or decision among ambiguous paths is needed.

**Done when:** an approved plan identifies scope, verification, and any required handoffs.

## 5. Implement within constraints

Make the smallest change that satisfies the confirmed plan. Follow the loaded policy and
architecture constraints, preserve consumer-owned data, and report a blocker instead of
inventing unconfigured commands, dependencies, or policy.

During implementation:
- Strictly adhere to Core Directive 1: Do not implement unasked side-effects or out-of-scope additions.
- Strictly adhere to Core Directive 2: Check if code can be simplified or existing utilities reused rather than writing redundant custom logic.

**Done when:** implementation matches the approved scope or a concrete blocker is reported.

## 6. Produce pre-ship proof

Before branch or pull-request handoff, perform focused review and provide this Code review
proof checklist:

- [ ] Run non-empty configured build, test, and format aliases that apply.
- [ ] Run configured secrets checking and resolve or report findings.
- [ ] Assess relevant documentation and specification-index updates.
- [ ] Review the changed scope for correctness, regressions, policy compliance, and strict adherence to requested scope (no unasked additions or over-engineering).
- [ ] Report command evidence, outcomes, remaining risks, and blockers.

Use configured aliases such as `build-backend`, `test-backend`, and `lint-backend`; do not
hardcode consumer commands.

**Done when:** every applicable checklist item has evidence or an explicit blocker.

## Opt-out

| Phrase | Effect |
|--------|--------|
| `stop ws-senior-developer` | Disable for this session when autoloaded |
| Unset `config.json` → `rules.seniorDeveloper` (empty string) | Disable path resolution / opt out of delivery-gate resolve |
