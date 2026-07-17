# Config & SCM Resolution — Dual-Mode

Canonical config and SCM resolution for **all** workflow skills.
Used by `spec-to-pr`, `spec-to-pr-lite`, providers, `08-fix-pr`, `09-goal-fix-pr`, `11-ship-pr`.

---

## Config path (only)

```text
.agents/skills/shared/config.json
```

Template: [`config.json.example`](config.json.example). Schema: [`config.schema.json`](config.schema.json).

**Forbidden as primary runtime config:**

- `.agents/skills/spec-to-pr/config.json`
- `.agents/skills/spec-to-pr-lite/config.json`

Scripts and skills that still mention those paths are **bugs** — fix to `shared/config.json`. Lite and full always share this file (dual-mode).

---

## SCM provider resolution (`providers.scm`)

1. Read `providers.active` / `providers.scm` from `.agents/skills/shared/config.json`.
2. If `providers` absent: enabled GitHub tracker → `scm=github`; else enabled Azure DevOps → `scm=azure-devops`; else STOP (require explicit `providers.scm`). Prefer GitHub if both enabled.
3. If `scm` absent: if active is `github`|`azure-devops` → scm=active; if active=`local` → parse `project.repoUrl` host (`github.com` → github; `dev.azure.com` / `visualstudio.com` → azure-devops); else STOP.
4. Reject `scm: "local"` for PR/thread/merge intents.
5. Load matching provider skill; call intents by name — do not embed host CLI recipes in consumer skills.

| `providers.scm` | Skill |
|-----------------|-------|
| `github` | [`github-provider`](../github-provider/SKILL.md) |
| `azure-devops` | [`azure-devops-provider`](../azure-devops-provider/SKILL.md) |

---

## Workflow mode flag

When dispatched from an orchestrator, skills receive (via Task prompt / state):

| Field | Values | Meaning |
|-------|--------|---------|
| `workflowType` | `standard` \| `lite` | Which orch owns the run |
| `workflowMode` | `true` when under orch | Suppress re-AskQuestion for ship / commit when orch already gated |
| `shipAction` | `create-pr` \| `push-only` \| `skip` | From orch ship gate |

Standalone invokes omit these; skills may present their own gates.
