# Template — documento PlugNotas local

Use ao criar ou reescrever arquivos em `docs/plugnotas/`.

---

## Estrutura

```markdown
# PlugNotas — {Título do tópico}

\`\`\`yaml
agent:
  when_to_read: {uma linha — quando o agente deve carregar este arquivo}
  official: https://docs.plugnotas.com.br{#/tag/TagSeAplicavel}
  related: {outros-arquivos.md, separados por vírgula}
  zendesk: {URL opcional}
  lib_implemented: {opcional — ver 05 ou 07}
\`\`\`

---

## Visão rápida

{2–4 frases: o que é, pré-requisitos, link mental com ERP.Fiscal}

---

## {Seção principal}

{Conteúdo explicativo — preferir tabelas e listas a parágrafos longos}

| Coluna A | Coluna B |
|----------|----------|
| … | … |

---

## {Detalhes / edge cases}

{Códigos HTTP, exemplos JSON enxutos, Content-Type}

\`\`\`json
{ "exemplo": "mínimo necessário" }
\`\`\`

---

## Referências cruzadas

- {Tópico relacionado}: [\`NN-nome.md\`](NN-nome.md)
- Mapeamento lib: [\`07-mapeamento-erp-fiscal.md\`](07-mapeamento-erp-fiscal.md)
```

---

## Regras de estilo

1. **Português (pt-BR)** — mesmo idioma dos docs existentes.
2. **Progressive disclosure** — agente lê só este arquivo quando o contexto pede; não repetir conteúdo de outros `.md` (linkar).
3. **Tabelas de rotas** — colunas: Operação | Método | Rota | Descrição | ERP.Fiscal (`✅` / `❌`).
4. **Exemplos JSON** — só trechos relevantes; schema completo fica no Swagger.
5. **Fronteira lib** — lembrar que payload NF-e é montado no ERP; lib recebe `string payloadJson`.
6. **Separadores** — `---` entre seções principais.
7. **Sem data hardcoded no corpo** — data de verificação fica nos READMEs índice.

---

## Diagrama mermaid (opcional)

Usar em fluxos (ex.: certificado → empresa → POST /nfe):

```markdown
\`\`\`mermaid
flowchart LR
  A[Passo 1] --> B[Passo 2]
\`\`\`
```

---

## Anti-patterns

| Evitar | Fazer |
|--------|-------|
| Copiar Swagger inteiro | Extrair campos/rotas usados pela lib e consumidores |
| Documentar NFSe/NFCe em detalhe | Mencionar só se impactar NF-e; escopo em `plugnotas/README.md` |
| `NfePayloadBuilder` na lib | Documentar estrutura JSON em `06`; builder fica no ERP |
| Múltiplos índices competindo | `docs/README.md` é entrada única; `plugnotas/README.md` é índice da pasta |
