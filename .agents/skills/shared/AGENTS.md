# Shared — Workflow Config & Consumer Data Hub

**Audience: agents** (config resolution, gates, consumer-owned paths). Humans: install narrative in root [`README.md`](../../../README.md).

> **Config, gates, bootstrap docs, and consumer-owned project data** for [`spec-to-pr`](../spec-to-pr/SKILL.md) and [`spec-to-pr-lite`](../spec-to-pr-lite/SKILL.md).
>
> This folder is **not** an installable skill package. The interactive installer copies hub templates here when a workflow or Full package is selected.
>
> Promoted utility skills (`caveman`, `gabarito`, `karpathy-guidelines`, `spec-format`, `goal-loop`, `self-learning`, `changelog`) live as **top-level** installable skills under `.agents/skills/<skill>/`.
>
> **Consumer-owned** (preserved on update; never overwritten by upstream):
> `config.json`, `stack.md`, `MEMORY.md`, and `memory/*`.
> Fresh install seeds empty `MEMORY.md` from `MEMORY.md.template` and `stack.md` from `stack.md.example`. Upstream hub memory/stack/config are **not** copied to consumers.

---

## Config & Tools

| File | Purpose |
|------|---------|
| [`config.json.example`](config.json.example) | Project config template — copy to `config.json`, fill in, never commit |
| [`config.schema.json`](config.schema.json) | JSON Schema for `config.json` validation |
| [`config-resolution.md`](config-resolution.md) | Canonical config path + SCM resolution (dual-mode) |
| [`gates.md`](gates.md) | Shared AskQuestion / delivery / ship / session-model banner (dual-mode) |
| [`tools.md`](tools.md) | Canonical agent tool vocabulary (aliases → config keys) |
| [`stack.md.example`](stack.md.example) | Template for human-readable stack companion — seeds `stack.md` |
| [`setup.md`](setup.md) | Bootstrap & entry logic shared by `spec-to-pr` and `spec-to-pr-lite` |
| [`MEMORY.md.template`](MEMORY.md.template) | Empty memory index template — seeds `MEMORY.md` |

## Consumer-owned (local only)

| File | Purpose |
|------|---------|
| `config.json` | Project identity, stack, verification, providers |
| `stack.md` | Human-readable companion to `config.json` (edit freely) |
| `MEMORY.md` | Compiled anti-regression index (`self-learning` skill) |
| `memory/*.md` | Individual memory entries (compile into `MEMORY.md`) |

---

## Promoted skills (top-level installables)

| Skill | Path |
|-------|------|
| `caveman` | [`../caveman/SKILL.md`](../caveman/SKILL.md) |
| `gabarito` | [`../gabarito/SKILL.md`](../gabarito/SKILL.md) |
| `karpathy-guidelines` | [`../karpathy-guidelines/SKILL.md`](../karpathy-guidelines/SKILL.md) |
| `spec-format` | [`../spec-format/SKILL.md`](../spec-format/SKILL.md) |
| `goal-loop` | [`../goal-loop/SKILL.md`](../goal-loop/SKILL.md) |
| `self-learning` | [`../self-learning/SKILL.md`](../self-learning/SKILL.md) |
| `changelog` | [`../changelog/SKILL.md`](../changelog/SKILL.md) |

Install packages and dependency map: [`../../../bin/skill-dependencies.json`](../../../bin/skill-dependencies.json).
