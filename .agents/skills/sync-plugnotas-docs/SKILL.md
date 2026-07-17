---
name: sync-plugnotas-docs
description: Consulta a documentação oficial PlugNotas (https://docs.plugnotas.com.br/), compara com docs/plugnotas/ locais e atualiza os .md com o mesmo formato (índice, progressive disclosure, YAML agent). Use ao implementar features/correções na lib, ao sincronizar docs, ou quando houver divergência entre código e documentação local.
version: 1.0
---

# Sync PlugNotas Docs

Skill **obrigatória neste repositório** para manter `docs/plugnotas/` alinhada à [documentação oficial PlugNotas](https://docs.plugnotas.com.br/).

**Índice local:** [`docs/README.md`](../../../docs/README.md) → [`docs/plugnotas/README.md`](../../../docs/plugnotas/README.md).

---

## Quando executar

| Gatilho | Ação mínima |
|---------|-------------|
| Nova feature ou endpoint na lib | Verificar Swagger **antes** de codar; atualizar doc afetada **depois** |
| Correção de bug em parser/provider/HTTP | Conferir se a doc local descrevia o comportamento errado; corrigir |
| Usuário pede sync/atualização de docs | Sync completo da área indicada (ou escopo NF-e inteiro) |
| Dúvida de campo, rota ou validação | Swagger primeiro; se útil para agentes futuros, persistir em `.md` |
| PR que altera `Contracts/`, `Providers/`, rotas HTTP | Checar `07-mapeamento-erp-fiscal.md` e doc de endpoint correspondente |

**Regra:** não implementar nem corrigir integração PlugNotas confiando só na memória do modelo — **sempre** cruzar com a fonte oficial.

---

## Fontes oficiais (ordem de prioridade)

| Prioridade | Fonte | Uso |
|------------|-------|-----|
| 1 | [Swagger — docs.plugnotas.com.br](https://docs.plugnotas.com.br) | Schema, rotas, parâmetros, exemplos, códigos HTTP — **canônico** |
| 2 | [Zendesk TecnoSpeed](https://atendimento.tecnospeed.com.br/hc/pt-br) | Fluxos assíncronos, tutoriais cadastro, edge cases |
| 3 | [Postman PlugNotas](https://documenter.getpostman.com/view/3720339/2sB3WpSh1R) | Exemplos de request/response para validar fixtures |

Ferramentas: `WebFetch` na URL do Swagger/tag; browser MCP se a página exigir renderização; `Grep` no código da lib; **script de diff** (abaixo).

### Script de diff automático

Na raiz do repositório:

```bash
python .agents/skills/sync-plugnotas-docs/scripts/diff-routes.py
```

Com cache local (evita re-download):

```bash
python .agents/skills/sync-plugnotas-docs/scripts/diff-routes.py --cache .tmp-postman-cache.json
```

Opções úteis:

| Flag | Efeito |
|------|--------|
| `--scope nfe` | Só NF-e (repita para `certificado`, `empresa`, `cnpj`, `cep`) |
| `--cache PATH` | Salvar/reusar coleção Postman JSON |
| `--strict` | Exit code 1 se houver lacunas na documentação |

O script baixa a [coleção Postman oficial](https://documenter.getpostman.com/view/3720339/2sB3WpSh1R) (espelho do Swagger), compara com `docs/plugnotas/` e com `CODE_ROUTES` no próprio script (mapa do `PlugNotasHttpClient` / auxiliares). **Ao adicionar rota na lib**, atualize `CODE_ROUTES` em [`scripts/diff-routes.py`](scripts/diff-routes.py).

Wrapper bash: [`scripts/diff-routes.sh`](scripts/diff-routes.sh).

---

## Workflow de sincronização

```
1. Escopo     → Qual tag/rota/tópico? (NF-e, certificado, empresa, auxiliares…)
2. Diff auto  → python .agents/skills/sync-plugnotas-docs/scripts/diff-routes.py [--scope …]
3. Oficial    → Ler Swagger + artigos Zendesk ligados (se diff apontar lacunas)
4. Local      → Ler .md correspondente em docs/plugnotas/
5. Diff       → Listar divergências (rota nova, campo, status, HTTP, texto obsoleto)
6. Atualizar  → Editar .md no formato padrão (ver DOC-TEMPLATE.md)
7. Índices    → docs/README.md, docs/plugnotas/README.md, AGENTS.md se roteamento mudar
8. Mapeamento → Se mudou lib: atualizar 07-mapeamento-erp-fiscal.md (coluna ERP.Fiscal)
9. Metadata   → Atualizar bloco "Última verificação" nos READMEs
10. Melhorias → Registrar sugestões (seção final deste workflow)
11. Re-diff   → Rodar diff-routes.py novamente até OK
```

Detalhes passo a passo: [WORKFLOW.md](WORKFLOW.md).

---

## Formato dos arquivos `.md`

Cada documento em `docs/plugnotas/` segue o template em [DOC-TEMPLATE.md](DOC-TEMPLATE.md).

Resumo:

- Título `# PlugNotas — {Tópico}`
- Bloco ` ```yaml agent: ... ``` ` no topo (`when_to_read`, `official`, `related`)
- **Progressive disclosure:** visão rápida → detalhes → tabelas → referências cruzadas
- Tabelas para rotas HTTP, status, settings, mapeamento lib (`✅` / `❌`)
- Diagrama mermaid só quando clarifica fluxo assíncrono ou pré-requisitos
- **Não** duplicar o Swagger inteiro — extrair o que agentes e devs da lib precisam
- Manter escopo alinhado a `ERP.Fiscal.PlugNotas` (NF-e, certificado, empresa, auxiliares)

### Numeração e novos arquivos

| # | Arquivo | Tópico |
|---|---------|--------|
| 01 | `01-ambientes-autenticacao.md` | Hosts, API key, sandbox |
| 02 | `02-certificado-digital.md` | `/certificado` |
| 03 | `03-empresa-emissor.md` | `/empresa` |
| 04 | `04-nfe-fluxo-emissao.md` | Assíncrono, polling, status |
| 05 | `05-nfe-endpoints.md` | Rotas `/nfe` |
| 06 | `06-nfe-payload-json.md` | Body POST `/nfe` |
| 07 | `07-mapeamento-erp-fiscal.md` | Código da lib |
| 08 | `08-auxiliares-cnpj-cep.md` | GET `/cnpj`, `/cep` |

Novo tópico dentro do escopo da lib: próximo número (`08-...md`), entrada nas tabelas de índice e em `AGENTS.md`.

---

## Integração com implementação de código

Ao **implementar** na lib:

1. Identificar tag Swagger (ex.: `#tag/NFe`, `#tag/Certificado`).
2. Ler doc local do tópico; se ausente ou datada, sync antes ou em paralelo.
3. Implementar contratos/parsers/providers conforme oficial.
4. Atualizar coluna **ERP.Fiscal** em `05-nfe-endpoints.md` ou seção equivalente em `07`.
5. Se novo contrato JSON: documentar campos relevantes em `06` ou contrato referenciado.

Ao **corrigir** bug:

1. Confirmar comportamento esperado no Swagger (não assumir).
2. Se a doc local estava errada, corrigir a doc no **mesmo PR** que o código.
3. Mencionar na resposta ao usuário o que mudou na doc.

---

## Bloco de metadata (atualizar após cada sync)

Em [`docs/README.md`](../../../docs/README.md) e [`docs/plugnotas/README.md`](../../../docs/plugnotas/README.md), manter:

```markdown
> **Última verificação:** YYYY-MM — fonte [docs.plugnotas.com.br](https://docs.plugnotas.com.br). Skill: [sync-plugnotas-docs](SKILL.md).
```

Substituir `YYYY-MM` pela data real da verificação.

---

## Sugerir melhorias na documentação

Ao final de cada sync ou sessão de feature, avaliar e **reportar ao usuário** (não só aplicar silenciosamente):

| Tipo | Exemplo |
|------|---------|
| Lacuna | Rota no Swagger sem menção local |
| Obsoleto | Host, status ou campo renomeado na API |
| Estrutura | Tópico grande demais → quebrar em novo `08-...md` |
| Índice | Entrada faltando em `docs/README.md` "Quando usar cada documento" |
| Código | Implementação ✅ na lib mas ❌ na tabela de endpoints |
| Zendesk | Artigo oficial útil não linkado no bloco `agent` |
| Consumidor | Esclarecimento de fronteira lib vs ERP faltando |

Formato sugerido na resposta:

```markdown
### Sugestões de melhoria na documentação
- **[Lacuna]** …
- **[Obsoleto]** …
```

Aplicar melhorias óbvias e seguras no mesmo turno; pedir confirmação só para reestruturações grandes.

---

## Checklist rápido (antes de concluir tarefa fiscal)

- [ ] Swagger consultado para rotas/campos tocados
- [ ] Doc local atualizada se houve divergência
- [ ] Índices (`docs/README.md`, `plugnotas/README.md`) consistentes
- [ ] `07-mapeamento-erp-fiscal.md` reflete providers/parsers alterados
- [ ] Data "Última verificação" atualizada se houve sync
- [ ] Sugestões de melhoria comunicadas ao usuário

---

## Recursos

- [DOC-TEMPLATE.md](DOC-TEMPLATE.md) — template de arquivo
- [WORKFLOW.md](WORKFLOW.md) — workflow detalhado e exemplos de diff
- [`scripts/diff-routes.py`](scripts/diff-routes.py) — diff automático Postman vs docs vs código
- [`AGENTS.md`](../../../AGENTS.md) — fronteira da lib
- [`docs/plugnotas/07-mapeamento-erp-fiscal.md`](../../../docs/plugnotas/07-mapeamento-erp-fiscal.md) — mapa código ↔ API
