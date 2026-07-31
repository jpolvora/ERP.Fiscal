# Canonical `*.spec.md` format

Referenced by [`SKILL.md`](SKILL.md). Other skills link here — do not duplicate.

## File name

| Source | Pattern |
|--------|---------|
| Tracker issue/WI `{id}` | `{plansDir}/us-{id}/step-00-us-{id}.spec.md` |
| Local slug | `{plansDir}/{slug}/step-00-{slug}.spec.md` |

Slug: `us-{id}` for tracker ids; else basename without `.spec.md` (strip optional `step-00-`).

## YAML frontmatter (required)

```yaml
---
id: 1474              # integer — tracker id; null if local
slug: us-1474
title: "Feature title"
source: github        # github | azure-devops | local
issueState: open      # optional
issueUrl: "https://github.com/{org}/{repo}/issues/1474"  # optional
workItemType: "User Story"  # optional — ADO
specDate: 2026-07-02
---
```

## Body (required sections)

```markdown
# Specification — {title}

## Description

(description text)

## Acceptance Criteria

- AC1: …
- AC2: …

## Child Tasks

(optional — when tracker had sub-tasks)

### Task #{id} — {title}

- **Status:** …
- **Description:** …

## Notes

(optional links / dependencies)
```

## Validation

1. ACs enumerable and testable — one line per AC.
2. `source: local` → author owns complete ACs (no tracker fetch).
3. `*.issue.json` is audit-only — downstream reads `spec.md` only.
4. Hand-written specs may live under `specs/` etc.; orch normalizes to canonical `step-00` path.
