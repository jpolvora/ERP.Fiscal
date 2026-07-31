---













name: ws-secrets-leak-review
version: 0.0.109
description: Secrets & PII leak auditor — scans code diffs and repositories for hardcoded credentials, API tokens, passwords, private keys, and sensitive data.
allowed-tools: Read, Grep, Glob
invocation_names:
  - secrets-leak-review
  - ws-secrets-leak-review
---

# Secrets & Leak Review

Read-only leak audit. Prefer **Grep / Glob / Read** (respect ignore). Do **not** run `secrets_scanner.sh` during the skill run; that script is optional pre-commit only.

## Goal

Find hardcoded secrets, credentials, PII, private keys, connection strings, and sensitive files that should not be committed. Emit one report, then **stop**.

## Hard stop (mandatory)

- Max **6** Grep calls and **2** Glob calls total. Batch patterns; do not re-scan.
- Prefer repo defaults (respect `.gitignore`). Never use `--no-ignore` / `-t all` / unbounded shell `rg` loops.
- Cap each Grep with `head_limit` ≤ 40. Do not page past the first page.
- No subagent / Bash / parallel agent scans.
- After the report (or "No leaks detected."), **finish**. Do not install hooks unless the user asked.

## What to find

| Priority | Look for | How |
|----------|----------|-----|
| HIGH | API keys / tokens (`AKIA…`, `sk-…`, `gh[ps]_…`, `github_pat_…`, `xox…`) | Grep those literals |
| HIGH | Private key armor (`BEGIN … PRIVATE KEY`) | Grep |
| HIGH | Connection strings with embedded passwords (`postgres://user:pass@…`, mysql, mongodb, redis, jdbc) | Grep |
| HIGH | Sensitive files present: `.env` (not `.env.example`), `*.pem` / `*.key` / `*.pfx` / `*.p12`, `secrets.y*ml`, `credentials.json`, `id_rsa` / `id_ecdsa` / `id_ed25519` | Glob |
| HIGH | YAML/JSON/env assignments: `password`, `api_key`, `secret_key`, `access_key`, `auth_token`, `private_key`, `connection_string` with real-looking values | Grep (skip obvious placeholders) |
| MEDIUM | Internal hosts / private IPs / corp URLs in tracked config | Grep, sample only |
| MEDIUM | PII-shaped data (SSN/CPF-like, real emails outside example/test domains) | Grep, sample only |
| LOW | Docs mentioning credentials without redaction | Grep `*.md` only if prior hits are thin |

Skip: `example.com` / `test.com` / `localhost` emails, `# pragma: allowlist secret`, docs that clearly show placeholders (`YOUR_KEY`, `xxx`, `changeme`).

Pattern catalog: [REFERENCE.md](REFERENCE.md).

## Steps

1. **Scope** — Default: whole working tree via Grep/Glob (ignore-aware). If user named paths or "staged only", limit to those paths.
   - Done when: scan roots are known.

2. **Secrets + sensitive files** — Run the HIGH rows from the table (few batched Greps + Globs).
   - Done when: HIGH pass finished within the call budget.

3. **PII / internal (optional)** — Only if call budget remains and HIGH was clean or thin; one or two MEDIUM Greps max.
   - Done when: MEDIUM pass done or skipped for budget.

4. **Report** — Structured report below. Never print raw secret values (path + line + type only). Then stop.
   - Done when: report delivered and no further tool calls.

## Report format

```
## Leak Scan Report

### HIGH — must fix before push
| File | Line | Type | Detail |
|------|------|------|--------|
| path | N | AWS Key | AKIA… pattern |

### MEDIUM — review recommended
…

### LOW — informational
…

### .gitignore notes
- (only if an obvious sensitive path is tracked and should be ignored)
```

Zero findings → `No leaks detected.`

## Confidence

| Level | Criteria |
|-------|----------|
| HIGH | Known secret shape or sensitive file, not an obvious placeholder |
| MEDIUM | Suspicious but unverified |
| LOW | Docs / likely false positive |

## Optional pre-commit (user-requested only)

```bash
bash .agents/skills/ws-secrets-leak-review/scripts/install-hook.sh
```

Hook runs `bash .agents/skills/ws-secrets-leak-review/scripts/pre-commit.sh` on staged files (which calls `bash …/secrets_scanner.sh`). Override: `git commit --no-verify`.

## Rules

- Read-only: no `git add`, commit, or file edits
- Do not dump secret values into chat
- Do not run the scanner script as part of this skill
