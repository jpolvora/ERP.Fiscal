# Segurança — segredos, privacidade e histórico Git

> **Progressive disclosure:** este arquivo é o **índice de roteamento** para segurança no `ERP.Fiscal`. Carregue sob demanda — procedimentos detalhados ficam na skill [`.agents/skills/security-check/SKILL.md`](../../.agents/skills/security-check/SKILL.md).

---

## Quando usar cada recurso

| Contexto da tarefa | Carregar / executar |
|--------------------|---------------------|
| Antes de **propor commit** ou encerrar sessão | Skill [`security-check`](../../.agents/skills/security-check/SKILL.md) — fases A–F (uncommitted, tracked, temporários) |
| Validar **apenas o stage** (simular Husky) | `npm run security:pre-commit` → [`scripts/pre-commit-security-check.sh`](../../scripts/pre-commit-security-check.sh) |
| Auditar **histórico Git** (read-only) | `npm run security:audit-history` → [`scripts/audit-history-secrets.sh`](../../scripts/audit-history-secrets.sh) |
| Preview de **remediação** com `git-filter-repo` | Expressões em [`scripts/audit-filter-expressions.txt`](../../scripts/audit-filter-expressions.txt); ver skill § auditoria de histórico |
| **Publicar NuGet** / release | Skill [`release-nuget-package`](../../.agents/skills/release-nuget-package/SKILL.md) + `security-check` |
| Vazamento **já no remoto** | Skill `security-check` § remediação + [git-filter-repo](https://github.com/newren/git-filter-repo) em clone fresco |

---

## Setup único (desenvolvedor)

```bash
npm install   # ativa Husky via prepare → .husky/pre-commit
```

Requisitos opcionais para auditoria de histórico:

- `rg` (ripgrep), `python3`, `pip install git-filter-repo`
- Gitleaks: baixado automaticamente com `npm run security:audit-history` (flag `--install-gitleaks` no script)

---

## Automação no repositório

| Camada | Artefato | Escopo |
|--------|----------|--------|
| **Pre-commit** | [`.husky/pre-commit`](../../.husky/pre-commit) | Bloqueia commit se o **stage** tiver segredos ou arquivos proibidos |
| **Allowlist Gitleaks** | [`.gitleaks.toml`](../../.gitleaks.toml) | Falsos positivos conhecidos (sandbox público PlugNotas, placeholders) |
| **Relatórios locais** | `.security-audit/` | Saída do script de auditoria (gitignored) |
| **Ferramentas locais** | `.tools/gitleaks/` | Binário Gitleaks baixado sob demanda (gitignored) |

O hook **complementa** — não substitui — a varredura manual da skill (tracked + temporários ignorados).

---

## Superfícies de varredura (skill `security-check`)

| Superfície | O que inclui |
|------------|--------------|
| **Uncommitted** | Staged, unstaged, untracked |
| **Versionados** | `git ls-files` (HEAD) |
| **Temporários** | `.tmp*`, caches Postman, planos locais — mesmo no `.gitignore` |

---

## Referências cruzadas

| Documento | Função |
|-----------|--------|
| [`../../AGENTS.md`](../../AGENTS.md) | Índice mestre do harness (skills + PlugNotas + segurança) |
| [`../README.md`](../README.md) | Índice geral `docs/` (PlugNotas + consumidores) |
| [`../../README.md`](../../README.md) | Quick start humano (build, testes, segurança) |
| [`.cursor/rules/security-check.mdc`](../../.cursor/rules/security-check.mdc) | Regra Cursor — carregar skill antes de commits |
