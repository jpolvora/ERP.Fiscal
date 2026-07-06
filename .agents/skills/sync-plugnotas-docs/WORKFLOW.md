# Workflow detalhado — sync PlugNotas

Complemento de [SKILL.md](SKILL.md). Carregar só ao executar sync ou investigação profunda.

---

## 1. Definir escopo

Perguntas:

- Qual endpoint, provider ou contrato mudou?
- Qual arquivo local mapeia? (tabela em SKILL.md)
- É só verificação ou reescrita?

**Escopos comuns:**

| Escopo | Arquivos típicos |
|--------|------------------|
| Autenticação / hosts | `01` |
| Certificado | `02`, `07` |
| Empresa emissor | `03`, `07` |
| Emissão assíncrona | `04`, `05` |
| Nova rota NF-e | `05`, `07` |
| Campo no JSON NF-e | `06` (+ `Contracts/` no código) |
| Refatoração providers | `07` |

---

## 2. Coletar fonte oficial

### Swagger

1. Abrir https://docs.plugnotas.com.br
2. Navegar à tag relevante (NFe, Certificado, Empresa, CNPJ, CEP…)
3. Anotar: método, path, path params, query, body schema, responses, códigos de erro

`WebFetch` em URLs diretas de tag ajuda quando o HTML é legível; use browser MCP se necessário.

### Zendesk (complemento)

Artigos já referenciados nos docs locais:

- [NF-e — primeiros passos](https://atendimento.tecnospeed.com.br/hc/pt-br/articles/24725044940951)
- [Certificado e empresa via API](https://atendimento.tecnospeed.com.br/hc/pt-br/articles/360055614214)

Buscar artigos novos se o Swagger mencionar comportamento não óbvio.

---

## 3. Comparar com local

**Primeiro passo:** executar o diff automático:

```bash
python .agents/skills/sync-plugnotas-docs/scripts/diff-routes.py --cache .tmp-postman-cache.json
```

Interpretação do relatório:

| Seção | Significado | Ação |
|-------|-------------|------|
| Lacuna | Rota na API, não na doc | Adicionar em `02`–`08` conforme escopo |
| Extra | Rota na doc, não no Postman | Verificar Swagger; remover se obsoleta |
| Inconsistencia | Doc ✅ sem código | Corrigir doc ou implementar na lib |
| Info | Código sem ✅ na tabela | Opcional: marcar em `05`/`07`/`08` |

Rotas em `KNOWN_POSTMAN_GAPS` (ex.: `PATCH /empresa/{cnpj}/config`) são esperadas — a coleção Postman pode omiti-las.

Checklist manual complementar:

```
Rotas
  [ ] Método ou path diferente
  [ ] Rota nova no Swagger ausente em 05
  [ ] Rota local marcada ✅ mas removida/depreciada na API

Campos / schema
  [ ] Campo obrigatório novo em POST /nfe
  [ ] Renomeação ou tipo alterado
  [ ] Enum de status incompleto em 04/05

Comportamento
  [ ] Fluxo assíncrono (polling vs webhook)
  [ ] Sandbox vs produção (hosts, cadastros separados)
  [ ] Content-Type (multipart vs json)

Lib
  [ ] Provider implementa rota não documentada
  [ ] Doc diz ✅ mas código não tem método
```

Registrar cada item como linha no plano de edição.

---

## 4. Editar arquivos locais

Ordem sugerida:

1. Arquivo temático (`01`–`06`)
2. `07-mapeamento-erp-fiscal.md` se código mudou
3. `docs/plugnotas/README.md` — tabela de documentos
4. `docs/README.md` — "Quando usar cada documento"
5. `AGENTS.md` — só se nova entrada de roteamento ou skill

**Diff mínimo:** alterar só seções afetadas; preservar tom e estrutura existentes.

---

## 5. Validar contra código

O script `diff-routes.py` já cruza com `CODE_ROUTES` (mapa estático do HttpClient). Ao adicionar método em `PlugNotasHttpClient` ou `PlugNotasAuxiliaresProvider`, **atualize `CODE_ROUTES`** no script.

Comandos úteis adicionais:

```bash
# Rotas HTTP na implementação
rg "api\.(sandbox\.)?plugnotas|/nfe|/certificado|/empresa" src/ERP.Fiscal.PlugNotas

# Métodos do provider público
rg "Task<" src/ERP.Fiscal.PlugNotas/Providers src/ERP.Fiscal.Abstractions
```

Coluna **ERP.Fiscal** deve bater com `PlugNotasHttpClient` e interfaces em `Abstractions`.

---

## 6. Atualizar metadata

Nos dois READMEs índice, atualizar linha `Última verificação` com mês/ano corrente.

---

## 7. Exemplo de saída ao usuário

```markdown
## Sync PlugNotas — {tópico}

**Fonte:** [docs.plugnotas.com.br](https://docs.plugnotas.com.br) (verificado em YYYY-MM-DD)

### Alterações
- `05-nfe-endpoints.md`: adicionada rota GET …
- `07-mapeamento-erp-fiscal.md`: coluna ERP.Fiscal para …

### Sem alteração necessária
- `01-ambientes-autenticacao.md` — conferido, alinhado

### Sugestões de melhoria na documentação
- **[Lacuna]** CC-e não tem fluxo em `04`; considerar `08-nfe-cce.md` se formos implementar
- **[Índice]** Incluir linha em docs/README para auxiliares CNPJ/CEP
```

---

## 8. Sync completo (auditoria periódica)

Quando o usuário pedir sync geral:

1. Percorrer tags Swagger do escopo NF-e + integração (certificado, empresa, auxiliares)
2. Revisar `01`–`07` em sequência
3. Atualizar todas as datas e índices
4. Entregar relatório consolidado com sugestões

Estimativa: não ler todos os `.md` de uma vez na mesma sessão de implementação pontual — usar progressive disclosure por tarefa.
