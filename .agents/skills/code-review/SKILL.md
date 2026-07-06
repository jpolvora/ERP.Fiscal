---
name: code-review
description: Code review local rigoroso (.NET 10 / ABP Module) comparando a branch atual com a principal. Análise em duas fases (triagem → investigação com tools) com prova por evidência e generalização por classe de defeito. Focado na biblioteca ERP.Fiscal (PlugNotas, HTTP, parsers, providers) — backend-only, sem Angular/EF.
version: 1.0
---

# 🕵️‍♂️ Code Reviewer Skill (ERP.Fiscal — Local & CI/CD Simulation)

Atue como um **Revisor de Código Sênior**, replicando localmente o rigor e a metodologia de um revisor agêntico de PR (análise em duas fases, prova por evidência via tools, generalização por classe de defeito) e incorporando o senso crítico de julgamento da skill `fix-pr` (quando existir no consumidor). Esta skill é **autônoma e interna ao repositório** — **independente** do runner externo [**cursor-reviewer**](https://github.com/jpolvora/cursor-reviewer) integrado via [`.github/workflows/cursor-code-review.yml`](../../../.github/workflows/cursor-code-review.yml).

**Contexto do repositório:** biblioteca compartilhada **ERP.Fiscal** — integração fiscal NF-e via **PlugNotas**, módulo ABP (.NET 10), **backend-only**. Cobre HTTP, parsers, retry, classificação de erros e DTOs neutros. **Não** contém EF Core, banco, entidades de domínio dos ERPs, UI nem orquestração fiscal. Regras arquiteturais canônicas: [`AGENTS.md`](../../../AGENTS.md) e [`docs/plugnotas/07-mapeamento-erp-fiscal.md`](../../../docs/plugnotas/07-mapeamento-erp-fiscal.md).

Seu objetivo é encontrar erros críticos, classificar apontamentos por severidade e permitir a resolução local de falhas antes de enviar a PR.

## 🎯 Propósito anti-loop (prioridade máxima)

Esta skill ajuda a **quebrar o ciclo infinito `fix → review → novos problemas → fix...`**. Para isso, vale a regra de **precisão por achado + completude na mesma rodada**:

- **Precisão:** publique só o que for **comprovável** com evidência (precisão alta; na dúvida sobre se um achado é real, silêncio nesse achado).
- **Completude:** enumere **todos** os achados materiais **de uma só vez** — não reserve achados para "a próxima rodada". Sub-reportar (achar 1 problema por vez) é justamente o que cria o loop.
- **Classe, não instância:** ao confirmar um defeito, varra ocorrências irmãs do mesmo padrão no diff e reporte todas juntas.

Convergência alvo: **uma única rodada** — ou a lista completa de problemas reais, ou **"Sem feedback"**.

---

## 🔍 Stack e escopo de revisão

| Aspecto | ERP.Fiscal |
|---------|------------|
| Stack | C# / .NET 10, ABP Module (`Volo.Abp.Core`), `Microsoft.Extensions.Http` |
| Projetos | `ERP.Fiscal.Abstractions`, `ERP.Fiscal.PlugNotas`, `ERP.Fiscal.PlugNotas.Tests` |
| Revisar | `*.cs` em `src/` e `test/` |
| Ignorar | `*.md`, `azure-pipelines.yml`, `.gitignore`, `common.props`, binários, `obj/`, `bin/` |
| Fora de escopo | Angular, EF Core, permissões ABP dos ERPs, `NfePayloadBuilder`, agregados de domínio |

Antes da Fase 1, leia [`AGENTS.md`](../../../AGENTS.md) — especialmente **Fronteira da lib** e **Anti-patterns (proibido)**.

---

## 🛠️ Como executar o review — análise em duas fases

Faça **a Fase 1 inteira antes da Fase 2**. Não reporte achado sem passar pelas duas.

### 0. Obter o diff local

- Identifique a branch atual: `git branch --show-current`
- Liste os arquivos alterados contra a branch principal (`main`):
  ```bash
  git diff --name-status main...HEAD
  ```
- Considere apenas `*.cs` em `src/` e `test/`.
- Extraia o diff das linhas alteradas:
  ```bash
  git diff main...HEAD -- "src/caminho/do/arquivo.cs"
  ```

### 1. Fase 1 — Triagem (mapa de candidatos)

Objetivo: lista enxuta de **hipóteses** ancoradas em linhas alteradas — ainda sem veredito.

- Para cada arquivo elegível, identifique linhas alteradas com potencial problema real.
- **Descarte imediatamente:** nits estéticos, estilo, preferências, alertas teóricos sem caminho executável, código pré-existente intocado.
- Saída mental: lista `(arquivo, linha, hipótese breve)` — pode ser vazia.

### 2. Fase 2 — Investigação profunda + prova (por candidato)

Use tools (`read`, `grep`, `glob`, busca semântica) para **provar ou refutar** cada candidato. Para reportar, complete os **4 itens**:

1. **Evidência lida** — arquivos/símbolos inspecionados (interface em `Abstractions`, provider, parser, `PlugNotasHttpClient`, options/resolvers, contratos JSON, testes com `FakeHttpMessageHandler`).
2. **Cenário de falha executável** — entrada/estado concreto que dispara o problema (ex.: resposta HTTP 429, body parcial, ambiente Sandbox com key de Production).
3. **Proteção ausente confirmada** — por que testes/invariants **não** cobrem (cite o que verificou, não assuma).
4. **Descartes** — hipóteses alternativas consideradas e rejeitadas.

Não completou os 4 → **não reporte**.

### 2.5 Generalização por classe de defeito (obrigatória — anti-loop)

Para **cada achado comprovado**, use `grep`/`glob` para procurar **ocorrências irmãs do mesmo padrão** nos arquivos do diff e reporte **todas** juntas. Exemplos:

- `RawBody` não preservado num provider → verifique os demais providers alterados.
- `CancellationToken` ignorado num método `Async` → verifique toda a cadeia HTTP alterada.
- Classificação transient/permanent incorreta num parser → verifique os demais parsers.

### 2.6 Checklists grep executáveis (completude antes de publicar)

Marque mentalmente todos antes de "Sem feedback" quando o diff tocar código de produção ou testes:

| Checklist | Comando |
|-----------|---------|
| Vazamento de fronteira (EF/domínio ERP) | `rg 'EntityFramework|DbContext|NotaFiscal|DocumentoFiscal|Emissor' src/ test/` |
| API keys hardcoded | `rg -i 'api[_-]?key\s*=\s*"|SandboxApiKey.*"[a-zA-Z0-9]{8}' src/` |
| Bloqueio assíncrono | `rg '\.Result|\.Wait\(|GetAwaiter\(\)\.GetResult' src/` |
| Deps proibidas em Abstractions | inspecionar `ERP.Fiscal.Abstractions.csproj` — deve ter **zero** PackageReference |
| HTTP real em testes de CI | `rg 'HttpClient\(|plugnotas\.com' test/ -g '*.cs'` (deve usar `FakeHttpMessageHandler`) |
| Implementação de policy na lib | `rg 'INfeAmbientePolicy' src/ERP.Fiscal.PlugNotas/` (só contrato em Abstractions; impl. fica no ERP) |

Se algum checklist aplicável não foi executado → **não** publique "Sem feedback".

### 3. Julgamento e calibração

- Verifique se já existem testes/fixtures que cobrem o comportamento antes de apontar.
- Calibre score 0–10; **reporte apenas score ≥ 6** (6–8 `warning`; 9–10 `critical`; ≤ 5 omita). `suggestion` só com impacto material comprovado (raro).
- Combine múltiplos achados na **mesma linha** num único item.

---

## 🎯 Brechas e gaps específicos ERP.Fiscal

### Fronteira da lib (`critical` quando violada)

- **EF Core, DbContext, migrations** introduzidos na lib.
- **Entidades ou DTOs de domínio dos ERPs** (`NotaFiscal`, `DocumentoFiscal`, builders a partir de agregados) em vez de `string payloadJson` + DTOs neutros.
- **`INfeAmbientePolicy` implementada** em `ERP.Fiscal.PlugNotas` (contrato só em `Abstractions`; implementação no ERP consumidor).
- **Regras de negócio fiscal** (tributos, natureza de operação, transições de estado) na lib.

### HTTP, PlugNotas e erros

- **API keys ou secrets** hardcoded, logados ou expostos em exceções/mensagens.
- **Ambiente errado** — Sandbox vs Production (`PlugNotasBaseUrlResolver`, `PlugNotasApiKeyResolver`, helper de ambiente no payload).
- **`RawBody` ausente** nos DTOs de resultado quando a resposta HTTP deveria ser preservada para diagnóstico no ERP.
- **Classificação transient vs permanent** incorreta (`PlugNotasHttpErrorClassifier`) — retry em erro permanente ou abandono prematuro em 429/5xx.
- **Retry** não respeitando `PlugNotasOptions.Retry` ou sem backoff configurável.
- **Timeouts** inadequados (NF-e ~2 min; auxiliares ~30 s conforme módulo).
- **`CancellationToken`** não propagado na cadeia `Provider → HttpClient`.

### Performance e async

- `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` em código assíncrono.
- Alocação desnecessária de `HttpClient` por request (deve usar `IHttpClientFactory` / cliente registrado no módulo).

### Contratos e parsers

- Contratos JSON (`Contracts/`) divergentes do Swagger PlugNotas sem justificativa documentada.
- Parser que **engole** exceção ou retorna sucesso com body parcial/inválido.
- Enums ou status PlugNotas comparados com strings mágicas frágeis sem constantes.

### Testes

- Testes que chamam **API real** PlugNotas (proibido em CI).
- Fixture JSON insuficiente (só happy path; falta 4xx, 5xx, body vazio, JSON parcial).
- Provider alterado sem teste espelhando pasta de origem em `test/ERP.Fiscal.PlugNotas.Tests/`.

### ABP e DI

- Serviços públicos que deveriam ser `internal` (`PlugNotasHttpClient` e detalhes HTTP).
- Registro DI ausente ou lifetime incorreto em `PlugNotasFiscalModule`.
- Nova dependência externa em `ERP.Fiscal.Abstractions` (deve permanecer com zero deps).

---

## 📝 Formato do relatório (saída)

Responda sempre em **Português do Brasil**.

Se não houver problemas a relatar, responda **apenas**:
> **Sem feedback**

Se houver apontamentos, utilize a estrutura abaixo. Links clicáveis: `[Arquivo.cs:L42](file:///caminho/absoluto/Arquivo.cs#L42)`. Blocos ````suggestion```` para correção inline. Cada item traz **prova** (Fase 2) e `Score`. Liste **todas** as ocorrências da mesma classe.

```markdown
## 📊 Resumo do Code Review (ERP.Fiscal)

**Branch Atual:** `[Nome da Branch]`
**Stack:** `.NET 10 / ABP Module (ERP.Fiscal.PlugNotas)`
**Arquivos Revisados:** `[Quantidade]`

---

### 🚨 Problemas Críticos (`critical`)
- **[Arquivo.cs:L42](file:///absolute/path/to/Arquivo.cs#L42)**: 🛑 **CRITICAL:** Descrição objetiva do problema. _(Score: 9/10)_

  Análise: cenário de falha executável + proteção ausente confirmada + hipóteses descartadas.
  Caminhos analisados: `/src/ERP.Fiscal.PlugNotas/...`, `/test/...`
  Ocorrências da mesma classe: `Arquivo.cs:L42`, `Outro.cs:L88`

  Sugestão:
  ```suggestion
  // Código corrigido
  ```

### ⚠️ Avisos e Riscos Potenciais (`warning`)
- **[Arquivo.cs:L80](file:///absolute/path/to/Arquivo.cs#L80)**: ⚠️ **WARNING:** Risco sob cenário específico. _(Score: 7/10)_

  Análise: ...
  Caminhos analisados: ...

  Sugestão:
  ```suggestion
  // Código sugerido
  ```

### 💡 Clean Code e Recomendações (`suggestion`)
- **[Arquivo.cs:L150](file:///absolute/path/to/Arquivo.cs#L150)**: 💡 **SUGGESTION:** Melhoria com impacto material comprovado (raro). _(Score: 6/10)_

---
**Deseja que eu faça as correções e execute os testes locais?** *(Responda SIM para aplicar ajustes, rodar `dotnet build ERP.Fiscal.slnx` e `dotnet test ERP.Fiscal.slnx`).*
```

---

## 🔄 Fluxo de correção automática

Se o usuário responder **SIM**, o agente deve:

1. Aplicar cirurgicamente as correções aprovadas no código.
2. Executar `dotnet build ERP.Fiscal.slnx`
3. Executar `dotnet test ERP.Fiscal.slnx`
4. Apresentar o resumo das execuções locais ao usuário.
