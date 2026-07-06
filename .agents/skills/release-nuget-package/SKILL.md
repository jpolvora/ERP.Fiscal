---
name: release-nuget-package
description: >-
  Publica pacotes ERP.Fiscal (Abstractions + PlugNotas) no GitHub Packages e NuGet.org:
  resolve versão em nuget.props, dispara workflow Deploy Main, cria tag/release e valida feeds.
  Use quando o usuário pedir release, publish NuGet, bump de versão, tag v*, deploy em main
  ou publicar pacote no nuget.org.
version: 1.0
---

# Release NuGet — ERP.Fiscal

Skill para o **fluxo completo de publicação** dos pacotes `ERP.Fiscal.Abstractions` e `ERP.Fiscal.PlugNotas`.

**Script:** [`scripts/release.sh`](scripts/release.sh) — wrapper na raiz: [`scripts/release-nuget.sh`](../../../scripts/release-nuget.sh).

**Workflow CI:** [`.github/workflows/deploy-main.yml`](../../../.github/workflows/deploy-main.yml) (`Deploy Main`).

---

## Quando executar

| Gatilho | Ação |
|---------|------|
| "publicar pacote", "release NuGet", "deploy nuget.org" | `release.sh publish` |
| "qual a última versão publicada?" | `release.sh status` |
| Após release | `release.sh verify [versão]` |
| Merge de `develop` + release | `release.sh publish --merge-develop` |

Antes de commitar ou publicar, aplicar a skill [`security-check`](../security-check/SKILL.md) (sem segredos em `nuget.props`, logs ou mensagens de commit).

---

## Modelo de versionamento (regra de ouro)

| Arquivo | Papel |
|---------|-------|
| [`nuget.props`](../../../nuget.props) | `VersionPrefix` + `PackagePatchNumber` → versão **a publicar no próximo deploy** |
| [`scripts/ci-package-version.sh`](../../../scripts/ci-package-version.sh) | `resolve` / `bump` do patch |
| CI em push `main` | Publica `0.1.{N}`, depois commita bump `{N+1}` com `[skip ci]` |
| Tag `v*` | Publica versão da tag; **não** faz bump automático |

**Não** incrementar `PackagePatchNumber` manualmente antes do release — o CI faz o bump **após** publicação bem-sucedida.

---

## Pré-requisitos

| Item | Verificação |
|------|-------------|
| `gh` autenticado | `gh auth status` |
| Push em `main` | permissão no repositório `jpolvora/ERP.Fiscal` |
| Secret `NUGET_API_KEY` | GitHub → Settings → Secrets → Actions (publicação nuget.org) |
| Working tree limpa | `git status` sem alterações pendentes |
| Código na `main` | merge de `develop` antes, ou `--merge-develop` |

---

## Fluxo automatizado (recomendado)

Na raiz do repositório:

```bash
# 1. Diagnóstico
bash scripts/release-nuget.sh status

# 2. Publicação completa
bash scripts/release-nuget.sh publish

# 3. Conferência (NuGet.org pode demorar alguns minutos para indexar)
bash scripts/release-nuget.sh verify
```

### O que `publish` faz

```
1. checkout main + pull
2. resolve versão (ex.: 0.1.9) de nuget.props
3. commit vazio "chore: trigger release v0.1.9"
4. push main → dispara Deploy Main
5. aguarda CI (build, test, pack, push GitHub Packages + NuGet.org)
6. CI faz bump automático em nuget.props (próxima versão)
7. tag v0.1.9 no commit trigger
8. push tag + GitHub Release
9. verify nos feeds
```

**Por que commit vazio em `main`?** Push de tag sozinho nem sempre dispara o workflow de forma confiável; push em `main` é o gatilho canônico e ainda aciona bump automático.

---

## Opções úteis

```bash
bash scripts/release-nuget.sh publish --dry-run          # plano sem alterar remoto
bash scripts/release-nuget.sh publish --merge-develop    # merge develop → main antes
bash scripts/release-nuget.sh publish --force            # republicar versão existente no GH Packages
bash scripts/release-nuget.sh publish --no-tag           # só CI, sem tag/release
bash scripts/release-nuget.sh publish --no-watch         # dispara CI sem aguardar
bash scripts/release-nuget.sh verify 0.1.9               # versão específica
```

---

## Workflow manual (agente)

Se o script não puder rodar, seguir esta ordem:

```
- [ ] security-check (sem segredos)
- [ ] git fetch origin && git checkout main && git pull
- [ ] (opcional) git merge origin/develop && git push
- [ ] VERSION=$(bash scripts/ci-package-version.sh resolve)
- [ ] git commit --allow-empty -m "chore: trigger release v${VERSION}"
- [ ] RELEASE_SHA=$(git rev-parse HEAD) && git push origin main
- [ ] gh run watch $(gh run list --workflow deploy-main.yml --branch main --limit 1 --json databaseId -q '.[0].databaseId')
- [ ] git pull origin main   # traz bump do bot
- [ ] git tag -fa "v${VERSION}" "${RELEASE_SHA}" -m "Release v${VERSION}"
- [ ] git push origin "v${VERSION}" --force
- [ ] gh release create "v${VERSION}" --target "${RELEASE_SHA}" (ou gh release edit)
- [ ] bash scripts/release-nuget.sh verify "${VERSION}"
```

---

## O que o CI publica

| Destino | Pacotes |
|---------|---------|
| GitHub Packages | `ERP.Fiscal.Abstractions`, `ERP.Fiscal.PlugNotas` (+ `.snupkg`) |
| NuGet.org | mesmos `.nupkg` (requer `NUGET_API_KEY`) |
| Artefato Actions | `nuget-packages-{versão}` |

Feed GitHub: `https://nuget.pkg.github.com/jpolvora/index.json`

---

## Troubleshooting

| Sintoma | Causa provável | Ação |
|---------|----------------|------|
| Tag existe, pacote não | Tag criada sem workflow | `publish` (commit em main) ou recriar tag após push |
| NuGet.org sem versão, GH OK | Indexação lenta | Aguardar 5–15 min; `verify` de novo |
| Workflow não bumpou | Commit com `[skip ci]` | Normal para bump do bot; release trigger **não** deve usar `[skip ci]` |
| `already exists` no push | Versão republicada | `--force` ou incrementar via merge em main |
| Falha NuGet.org | `NUGET_API_KEY` ausente/inválida | Configurar secret no repositório |

Logs: `gh run view <id> --log | grep -i nuget`

---

## Saída esperada ao usuário

Após release bem-sucedido, informar:

1. Versão publicada (`0.1.{N}`)
2. Próxima versão em `nuget.props`
3. Link da GitHub Release
4. Links nuget.org dos pacotes
5. Se NuGet.org ainda indexando, avisar que GitHub Packages já está disponível

---

## Referências

- [`AGENTS.md`](../../../AGENTS.md) — índice de skills
- [`README.md`](../../../README.md) — consumo dos pacotes
- [`.github/workflows/deploy-main.yml`](../../../.github/workflows/deploy-main.yml)
- [`.github/workflows/validate-pr.yml`](../../../.github/workflows/validate-pr.yml) — smoke test PR (não publica)
