# Changelog

## 2026-07-17

### Harness audit corrections (`check-harness`)

- Root `AGENTS.md`: added Skill loading / Precedence / Opt-outs / External dependencies; delegated workflow catalog to `.agents/AGENTS.md`; replaced `file://` skill links; clarified `code-review` vs `06-code-review` and `security-check` vs `secrets-leak-review`.
- Fixed broken relative links in `security-check` and `erp-fiscal-consumer` (`../../../` to repo root).
- Installed `.cursor/rules/ask-question-gates.mdc`; bootstrapped `.agents/skills/shared/config.json` + `stack.md` for ERP.Fiscal; gitignored shared `config.json`.
- Softened missing `bin/skill-dependencies.json` link in packaged `shared/AGENTS.md`; noted product-only skills in `.agents/AGENTS.md`.
