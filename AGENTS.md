# ERP.Fiscal — instruções para agentes

> **Progressive disclosure:** este arquivo traz orientação universal para agentes de codificação e atua como o índice principal de regras e skills. Carregue os documentos e diretrizes adicionais **sob demanda** usando a ferramenta `view_file`.
> 
> - **Visão geral do repositório:** [`README.md`](README.md).
> - **Documentação PlugNotas (API/Integração):** [`docs/README.md`](docs/README.md).
> - **Segurança (segredos, Husky, auditoria Git):** [`docs/security/README.md`](docs/security/README.md).
> - **Skills de produto (este repo):** consulte o [Índice de Skills e Customizações](#skills-e-customizações-indice) abaixo.
> - **Catálogo de skills do workflow (hub consumer):** [`.agents/skills/ws-shared/AGENTS.md`](.agents/skills/ws-shared/AGENTS.md) — `ws-spec-to-pr`, pipeline `ws-*`, providers, reviews portáteis.
> - **Instalar / atualizar workflow-skills:** [§ Workflow-skills (install / update)](#workflow-skills-install--update).

Biblioteca de integração fiscal (NF-e via **PlugNotas**), consumidor-agnóstica. Stack: **ABP Module (.NET 10)**, backend-only, **sem EF Core, sem banco, sem entidades de domínio dos consumidores**.

---

## Workflow-skills (install / update)

Skills gerenciadas (`ws-*` + hub `ws-shared/`) vêm do repositório upstream [jpolvora/workflow-skills](https://github.com/jpolvora/workflow-skills). Cópias locais sob `.agents/skills/ws-*` são **sobrescritas** em `update`. Dados do consumidor em `.agents/skills/ws-shared/` (`config.json`, `STACK.md`, `MEMORY.md`, `memory/*`, `CHANGELOG.md`, `installed-skills.json`) **nunca** são apagados pelo instalador.

**Forma canônica:** `npx --yes github:jpolvora/workflow-skills` — **não** acrescentar `@latest` nem `@main`.

| Ação | Comando |
|------|---------|
| Install interativo | `npx --yes github:jpolvora/workflow-skills` |
| Install Full (non-TTY) | `npx --yes github:jpolvora/workflow-skills install --full --yes` |
| Install pacote Workflows | `npx --yes github:jpolvora/workflow-skills install --package workflows --yes` |
| Update skills rastreadas | `npx --yes github:jpolvora/workflow-skills update` |
| Update + skills novas do upstream | `npx --yes github:jpolvora/workflow-skills update --include-new` |
| Checar versão vs remoto | `npx --yes github:jpolvora/workflow-skills --check` |
| Versão instalada | `npx --yes github:jpolvora/workflow-skills --version` |
| Uninstall (preserva `ws-shared/` consumer) | `npx --yes github:jpolvora/workflow-skills uninstall --skills <id> --yes` |

**Após install/update:** rodar `/ws-check-harness`; se `config.json` tiver placeholders, `/ws-configure-project`.

**Ownership neste repo**

| Tipo | Pastas | Quem mantém |
|------|--------|-------------|
| Managed (workflow-skills) | `.agents/skills/ws-*`, templates em `ws-shared/` | Upstream — use `update`; não editar de forma permanente aqui |
| Product (ERP.Fiscal) | `security-check`, `sync-plugnotas-docs`, `code-review`, `erp-fiscal-consumer`, `erp-fiscal-sync`, `release-nuget-package` | Este repo — **não** remover no cleanup de migração |
| Removidas de propósito | legado `00-*`…`11-*`, `shared/`, `gabarito`, `caveman`, providers sem `ws-`, reviews genéricas não usadas | Não restaurar |

Hub de roteamento após install: [`.agents/skills/ws-shared/AGENTS.md`](.agents/skills/ws-shared/AGENTS.md). Doc humana completa: [README do workflow-skills](https://github.com/jpolvora/workflow-skills#install-update-and-uninstall).

---

## Skill loading (mandatory)

| Skill | Trigger | Path |
|:---|:---|:---|
| `ws-karpathy-guidelines` | Every prompt (surgical changes) | [`.agents/skills/ws-karpathy-guidelines/SKILL.md`](.agents/skills/ws-karpathy-guidelines/SKILL.md) |
| `ws-tdah` | Every prompt (action-first / compression; root override) | [`.agents/skills/ws-tdah/SKILL.md`](.agents/skills/ws-tdah/SKILL.md) |
| `ws-senior-developer` | Every prompt (delivery gate / scope; root override) | [`.agents/skills/ws-senior-developer/SKILL.md`](.agents/skills/ws-senior-developer/SKILL.md) |
| `sync-plugnotas-docs` | Features / integração PlugNotas / docs | [`.agents/skills/sync-plugnotas-docs/SKILL.md`](.agents/skills/sync-plugnotas-docs/SKILL.md) |
| `security-check` | Before commit / end of task / `/security-check` | [`.agents/skills/security-check/SKILL.md`](.agents/skills/security-check/SKILL.md) |
| `ws-self-learning` | Every task completion (anti-regression) | [`.agents/skills/ws-self-learning/SKILL.md`](.agents/skills/ws-self-learning/SKILL.md) |
| `ws-changelog` | Every task completion | [`.agents/skills/ws-changelog/SKILL.md`](.agents/skills/ws-changelog/SKILL.md) |
| `ws-sync-spec` | Every task completion (após mudanças de código — anti-drift de specs) | [`.agents/skills/ws-sync-spec/SKILL.md`](.agents/skills/ws-sync-spec/SKILL.md) |
| Workflow catalog | On demand (`/ws-spec-to-pr`, reviews, ship, …) | [`.agents/skills/ws-shared/AGENTS.md`](.agents/skills/ws-shared/AGENTS.md) |

**Cursor rules (Layer 0):** [`security-check.mdc`](.cursor/rules/security-check.mdc) · [`plugnotas-docs-sync.mdc`](.cursor/rules/plugnotas-docs-sync.mdc) · [`ask-question-gates.mdc`](.cursor/rules/ask-question-gates.mdc) (workflow gates → native `AskQuestion`; see [`.agents/skills/ws-shared/gates.md`](.agents/skills/ws-shared/gates.md)).

**Language:** conversational replies follow **pt-BR** (see Sempre), unless the user asks otherwise. Skill file bodies and pipeline artifacts stay **en-us** for portable packaged skills. Harness audits (`ws-check-harness`) reply in **en-us**.

**Completion criterion:** before first reply, load every “Every prompt” row above; before declaring a coding task done, run `security-check` + `ws-self-learning` + `ws-changelog` + `ws-sync-spec` (quando houver mudança de código com specs afetadas) as applicable.

## Precedence

1. Explicit user instructions for this turn  
2. This file (`AGENTS.md`) — product invariants + Skill loading (overrides `ws-shared` opt-in defaults for autoload)  
3. Workflow hub [`.agents/skills/ws-shared/AGENTS.md`](.agents/skills/ws-shared/AGENTS.md) — workflow routing  
4. Resolved guardrails ([External dependencies](#external-dependencies))  
5. Auto-load skills: `ws-karpathy-guidelines` (scope) → `ws-senior-developer` (delivery gate) → `ws-tdah` (action-first / compression; keep technical accuracy)  
6. Task-specific skills from the workflow catalog  

## Opt-outs

| Phrase | Effect |
|:---|:---|
| `stop ws-tdah` / `stop verbosity` / `normal mode` | Disable ws-tdah for the session |
| `stop ws-gabarito` / `sem ws-gabarito` | Same disable (retired gabarito alias) |
| `stop ws-senior-developer` | Disable ws-senior-developer autoload for the session |
| `skip karpathy` / `skip ws-karpathy-guidelines` | Skip surgical-scope skill when explicitly opted out |
| `skip ws-sync-spec` | Skip feature-spec sync at task completion |

## External dependencies

Portable resolution (first match): see [`.agents/skills/ws-shared/AGENTS.md` § External dependencies](.agents/skills/ws-shared/AGENTS.md#external-dependencies). Config: `.agents/skills/ws-shared/config.json` (from `config.json.example`; gitignored).

| Dependency | Resolve (first match) |
|:---|:---|
| `senior-developer` | `config.json` → `rules.seniorDeveloper` → [`.agents/skills/ws-senior-developer/SKILL.md`](.agents/skills/ws-senior-developer/SKILL.md) → global/user skill |
| `ws-karpathy-guidelines` | `config.json` → `rules.karpathyGuidelines` → [`.agents/skills/ws-karpathy-guidelines/SKILL.md`](.agents/skills/ws-karpathy-guidelines/SKILL.md) |
| Stack companion | `config.json` → `rules.stackFile` (default `.agents/skills/ws-shared/STACK.md`) |

**Code review proof:** use the checklist from the resolved `senior-developer` skill when present; otherwise apply evidence-based review from product `code-review` / pipeline `ws-code-review` as routed below.

---

## Skills e Customizações (Índice)

### Product (local — não gerenciadas pelo workflow-skills)

| Skill / Diretriz | Arquivo | Propósito e Contexto de Uso |
|:---|:---|:---|
| **sync-plugnotas-docs** | [`.agents/skills/sync-plugnotas-docs/SKILL.md`](.agents/skills/sync-plugnotas-docs/SKILL.md) | **[Sempre neste repo]** Consulta [docs.plugnotas.com.br](https://docs.plugnotas.com.br), atualiza `docs/plugnotas/` no formato local (índice, progressive disclosure) e sugere melhorias. **Obrigatória** ao implementar features, corrigir bugs de integração ou sincronizar documentação. Regra Cursor: [`.cursor/rules/plugnotas-docs-sync.mdc`](.cursor/rules/plugnotas-docs-sync.mdc). |
| **code-review** | [`.agents/skills/code-review/SKILL.md`](.agents/skills/code-review/SKILL.md) | Review local **ERP.Fiscal** (lib PlugNotas / .NET 10). Distinto de `ws-code-review` (Step 6 do pipeline). |
| **consume-erp-fiscal** | [`.agents/skills/erp-fiscal-consumer/SKILL.md`](.agents/skills/erp-fiscal-consumer/SKILL.md) | **[Portável para Consumidores]** Guia de integração para ERPs que consomem esta biblioteca (NuGet/GitHub Packages, ABP, fronteiras domínio vs lib). |
| **erp-fiscal-sync** | [`.agents/skills/erp-fiscal-sync/SKILL.md`](.agents/skills/erp-fiscal-sync/SKILL.md) | **[Portável]** Sync bidirecional vendored ↔ canônico (`promote` / `pull`); user-invoked `/erp-fiscal-sync`. |
| **security-check** | [`.agents/skills/security-check/SKILL.md`](.agents/skills/security-check/SKILL.md) | **[Sempre neste repo — canônico]** Segredos, credenciais, PII — Husky + docs. Scanner portátil on-demand: `ws-secrets-leak-review`. Índice: [`docs/security/README.md`](docs/security/README.md). Regra Cursor: [`.cursor/rules/security-check.mdc`](.cursor/rules/security-check.mdc). |
| **release-nuget-package** | [`.agents/skills/release-nuget-package/SKILL.md`](.agents/skills/release-nuget-package/SKILL.md) | Publicação NuGet (`ERP.Fiscal.Abstractions`, `ERP.Fiscal.PlugNotas`): versão, `Deploy Main`, tag/release, feeds. Script: `scripts/release-nuget.sh`. |

### Managed (`ws-*` — workflow-skills)

Roteamento completo: [`.agents/skills/ws-shared/AGENTS.md`](.agents/skills/ws-shared/AGENTS.md). Install/update: [§ acima](#workflow-skills-install--update).

| Exemplos | Uso |
|:---|:---|
| `ws-karpathy-guidelines`, `ws-tdah`, `ws-senior-developer`, `ws-self-learning`, `ws-changelog`, `ws-sync-spec` | Autoload / completion (ver Skill loading) |
| `ws-spec-to-pr`, `ws-spec-to-pr-lite`, pipeline `ws-write-spec`…`ws-fix-pr` | Entrega Spec → PR |
| `ws-code-review`, `ws-secrets-leak-review`, `ws-check-harness` | Review / segurança / auditoria de harness |
| `ws-github-provider`, `ws-azure-devops-provider`, `ws-local-spec-provider` | Providers SCM / specs |

> [!TIP]
> A skill **`consume-erp-fiscal`** (`name:` no frontmatter) vive em [`.agents/skills/erp-fiscal-consumer/SKILL.md`](.agents/skills/erp-fiscal-consumer/SKILL.md). Copie essa pasta para o ERP consumidor (ou o path que o consumidor usar para skills), para o agente seguir as fronteiras corretas.

---

## Sempre (toda sessão)

- Responder em **Português (pt-BR)**, salvo pedido contrário (ex.: `check-harness` → en-us).
- Mudanças **cirúrgicas** — mínimo diff que resolve o pedido.
- Seguir padrões **ABP Framework** para módulos C# (.NET 10).
- **Documentação PlugNotas atualizada:** ao implementar features, corrigir integração ou alterar `Contracts/`/`Providers`/HTTP, seguir a skill [`sync-plugnotas-docs`](.agents/skills/sync-plugnotas-docs/SKILL.md) — consultar o Swagger em https://docs.plugnotas.com.br, cruzar com `docs/plugnotas/`, atualizar os `.md` afetados no mesmo trabalho e sugerir melhorias quando houver lacunas.
- Consultar a documentação PlugNotas via [`docs/README.md`](docs/README.md) (compilação local com índice); para schema completo de campos, usar o Swagger em https://docs.plugnotas.com.br
- Aplicar **SOLID** e **DRY**; preferir interfaces e abstrações reutilizáveis.
- **Checagem de segurança obrigatória:** seguir [`docs/security/README.md`](docs/security/README.md) e a skill [`security-check`](.agents/skills/security-check/SKILL.md) — varredura uncommitted + versionados + temporários; Husky reforça o stage após `npm install`.

---

## Documentação (PlugNotas)

**Ponto de entrada:** [`docs/README.md`](docs/README.md) — índice da documentação oficial compilada em `.md`, com roteamento por contexto de tarefa.

**Manutenção:** a compilação local **deve permanecer alinhada** à [documentação oficial PlugNotas](https://docs.plugnotas.com.br). Use a skill [`sync-plugnotas-docs`](.agents/skills/sync-plugnotas-docs/SKILL.md) para verificar, atualizar e propor melhorias — especialmente antes/depois de mudanças em providers, parsers, contratos JSON e rotas HTTP. A regra [`.cursor/rules/plugnotas-docs-sync.mdc`](.cursor/rules/plugnotas-docs-sync.mdc) garante que agentes carreguem essa orientação em toda sessão neste repositório.

| Contexto | Documento |
|----------|-----------|
| Índice PlugNotas + regra de ouro lib vs ERP | [`docs/plugnotas/README.md`](docs/plugnotas/README.md) |
| API key, sandbox, hosts | [`docs/plugnotas/01-ambientes-autenticacao.md`](docs/plugnotas/01-ambientes-autenticacao.md) |
| Certificado A1 | [`docs/plugnotas/02-certificado-digital.md`](docs/plugnotas/02-certificado-digital.md) |
| Cadastro emissor/empresa | [`docs/plugnotas/03-empresa-emissor.md`](docs/plugnotas/03-empresa-emissor.md) |
| Fluxo assíncrono NF-e | [`docs/plugnotas/04-nfe-fluxo-emissao.md`](docs/plugnotas/04-nfe-fluxo-emissao.md) |
| Rotas HTTP NF-e | [`docs/plugnotas/05-nfe-endpoints.md`](docs/plugnotas/05-nfe-endpoints.md) |
| Rotas e fluxo NFS-e | [`docs/plugnotas/09-nfse-endpoints.md`](docs/plugnotas/09-nfse-endpoints.md) |
| Payload JSON (builder no ERP) | [`docs/plugnotas/06-nfe-payload-json.md`](docs/plugnotas/06-nfe-payload-json.md) |
| Mapeamento → `ERP.Fiscal.PlugNotas` | [`docs/plugnotas/07-mapeamento-erp-fiscal.md`](docs/plugnotas/07-mapeamento-erp-fiscal.md) |
| Consulta CNPJ/CEP (auxiliares) | [`docs/plugnotas/08-auxiliares-cnpj-cep.md`](docs/plugnotas/08-auxiliares-cnpj-cep.md) |

Não carregar todos os arquivos de uma vez — seguir a tabela **"Quando usar cada documento"** em [`docs/README.md`](docs/README.md).

---

## Segurança (segredos e privacidade)

**Ponto de entrada:** [`docs/security/README.md`](docs/security/README.md) — índice de roteamento (Husky, scripts, Gitleaks, histórico Git).

**Procedimento detalhado:** skill [`security-check`](.agents/skills/security-check/SKILL.md) — carregar ao propor commit, responder `/security-check` ou investigar vazamento.

| Contexto | Recurso |
|----------|---------|
| Índice e comandos npm | [`docs/security/README.md`](docs/security/README.md) |
| Checklist manual (fases A–F) | [`.agents/skills/security-check/SKILL.md`](.agents/skills/security-check/SKILL.md) |
| Bloqueio no `git commit` | `.husky/pre-commit` → `scripts/pre-commit-security-check.sh` |
| Auditoria read-only do histórico | `npm run security:audit-history` |
| Release / publicação NuGet | skill `security-check` + [`release-nuget-package`](.agents/skills/release-nuget-package/SKILL.md) |

Não duplicar checklists de segurança fora da skill — usar links.

---

## Fronteira da lib (regra crítica)

A lib cobre **transmissão HTTP, parsers, retry, classificação de erros, contratos PlugNotas e DTOs/helpers neutros**. Cada ERP consumidor mantém domínio, orquestração, histórico, blobs, permissões, localização e UI.

| Pertence à lib | Fica no ERP consumidor |
|---|---|
| `INfeEmissaoProvider`, `INfseEmissaoProvider`, `INfeIntegracaoProvider`, `INfeAuxiliaresProvider` | Agregados (`NotaFiscal`, `DocumentoFiscal`, `Emissor`/`Empresa`) |
| Contratos JSON espelhando a API PlugNotas (`Contracts/`) | `NfePayloadBuilder` (domínio → JSON PlugNotas) |
| `PlugNotasHttpClient`, parsers, resolvers, options | App services de orquestração + transições de estado |
| DTOs neutros (`NfeEmissaoResult`, `NfeProviderResult`, …) | Tradução de resultados → histórico, mensagens localizadas |
| Contrato `INfeAmbientePolicy` | Implementação da policy (Settings/`appsettings` locais) |
| Helpers/interfaces auxiliares neutros sobre payloads PlugNotas, primitives, options ou DTOs da própria lib | Regras de negócio fiscal, tributos, natureza de operação |

**Nunca** introduzir Entity Framework, DbContext, migrations, entidades, enums, status internos ou DTOs de domínio dos ERPs neste repositório.

**Regra de entrada:** providers recebem **payload JSON já montado** + **ambiente efetivo** + identificadores de correlação (`idIntegracao`, CNPJ). Retornam DTOs neutros com status, ids, mensagens, raw body e flags transient/permanent.

### O que pode entrar no `ERP.Fiscal`

- Classes e DTOs limitados a **parâmetros de comunicação/transmissão**, inputs/outputs e estruturas documentadas pela PlugNotas.
- Abstrações provider-agnósticas (`INfe*Provider`, results, modelos neutros) necessárias para o ERP conversar com a lib.
- Helpers que operam apenas sobre:
  - `string`, números, `bool`, enums/objetos **da própria lib**;
  - payloads PlugNotas em `Contracts/`;
  - options da própria lib;
  - DTOs neutros já expostos pela lib.
- Interfaces auxiliares que ajudem os consumidores a padronizar o uso da lib **sem carregar vocabulário de domínio do ERP**.

### O que não pode entrar no `ERP.Fiscal`

- Builders que recebam agregados do consumidor (`Empresa`, `Cliente`, `DocumentoFiscal`, etc.) para montar payloads.
- Mapeadores que dependam de enums, status, policies ou regras de negócio do ERP consumidor.
- Tradução de resultados técnicos para mensagens localizadas, histórico, blobs, workflow ou status internos do ERP.
- Qualquer helper "genérico" que só funcione porque conhece tipos do consumidor.

### Teste mental antes de extrair código

Se a extração exigir conhecer **quem consome a lib**, o código está no lugar errado.

- Se depende de tipos/nomes do ERP consumidor: **fica no consumidor**.
- Se depende da documentação/contrato PlugNotas ou de tipos neutros da própria lib: **pode entrar na lib**.
- Em caso de dúvida, prefira manter no ERP e só extrair após neutralizar a entrada/saída.

---

## Estrutura da solução

```
ERP.Fiscal/
├── ERP.Fiscal.slnx
├── common.props                    # net10.0, nullable, LangVersion latest
├── package.json                    # Husky + npm scripts security:*
├── .husky/pre-commit               # hook: scripts/pre-commit-security-check.sh
├── scripts/
│   ├── pre-commit-security-check.sh
│   └── audit-history-secrets.sh    # auditoria histórico (read-only)
├── docs/
│   ├── README.md                   # índice docs (PlugNotas + roteamento)
│   ├── security/README.md          # índice segurança
│   └── plugnotas/                  # compilação PlugNotas
├── src/
│   ├── ERP.Fiscal.Abstractions/    # zero dependências externas
│   │   ├── INfeEmissaoProvider.cs
│   │   ├── INfseEmissaoProvider.cs
│   │   ├── INfeIntegracaoProvider.cs
│   │   ├── INfeAuxiliaresProvider.cs
│   │   ├── INfeAmbientePolicy.cs   # contrato; impl. no ERP
│   │   ├── NfeAmbiente.cs
│   │   ├── NfeIntegracaoModels.cs
│   │   └── Results/
│   └── ERP.Fiscal.PlugNotas/       # Volo.Abp.Core + Microsoft.Extensions.Http
│       ├── PlugNotasFiscalModule.cs
│       ├── Configuration/          # Options, resolvers, constants
│       ├── Contracts/              # DTOs JSON da API PlugNotas
│       ├── Http/                   # PlugNotasHttpClient (internal)
│       ├── Parsers/
│       ├── Payload/                # helpers genéricos em JSON
│       └── Providers/              # implementações das interfaces
└── test/
    └── ERP.Fiscal.PlugNotas.Tests/ # unit tests (HttpClient fake, parsers, retry)
```

---

## Onde colocar código novo

| Tarefa | Destino |
|--------|---------|
| Nova interface ou DTO neutro de resultado | `src/ERP.Fiscal.Abstractions/` |
| Implementação PlugNotas de uma interface | `src/ERP.Fiscal.PlugNotas/Providers/` |
| Contrato JSON espelhando endpoint PlugNotas | `src/ERP.Fiscal.PlugNotas/Contracts/` |
| Parser de resposta/erro HTTP | `src/ERP.Fiscal.PlugNotas/Parsers/` |
| Options, resolvers, constants | `src/ERP.Fiscal.PlugNotas/Configuration/` |
| Cliente HTTP ou extensão de DI | `src/ERP.Fiscal.PlugNotas/Http/` ou `Extensions/` |
| Helper neutro que opera em payload/params PlugNotas ou types da própria lib | `src/ERP.Fiscal.PlugNotas/Payload/` |
| Registro DI / módulo ABP | `PlugNotasFiscalModule.cs` |
| Testes unitários | `test/ERP.Fiscal.PlugNotas.Tests/` espelhando a pasta de origem |
| Script de segurança (pre-commit / auditoria) | `scripts/pre-commit-security-check.sh`, `scripts/audit-history-secrets.sh` |
| Índice ou doc de segurança | `docs/security/README.md` (roteamento); skill `.agents/skills/security-check/SKILL.md` (procedimento) |

---

## Interfaces públicas (contrato)

| Interface | Responsabilidade |
|-----------|------------------|
| `INfeEmissaoProvider` | Emitir, consultar, cancelar NF-e; obter XML/PDF |
| `INfseEmissaoProvider` | Emitir, consultar, cancelar NFS-e; obter XML/PDF |
| `INfeIntegracaoProvider` | Cadastro/consulta de certificado e emissor; sync de ambiente |
| `INfeAuxiliaresProvider` | Consulta CNPJ/CEP/municípios NFS-e (formulários de cadastro) |
| `INfeAmbientePolicy` | Resolver ambiente efetivo (ex.: forçar Sandbox) — **implementar no ERP** |

---

## Consumo em um ERP ABP

```csharp
[DependsOn(typeof(PlugNotasFiscalModule))]
public class MeuErpApplicationModule : AbpModule { }
```

Configuração (`appsettings.json`):

```json
{
  "PlugNotas": {
    "SandboxApiKey": "",
    "ProductionApiKey": "",
    "OnlySandbox": true,
    "TipoContrato": 1,
    "Retry": { "MaxAttempts": 3, "BaseDelayMs": 1000 }
  }
}
```

**Produção e consumidores (ex.: FiscalWR):** `PackageReference` via GitHub Packages — feed `https://nuget.pkg.github.com/jpolvora/index.json`, IDs `ERP.Fiscal.Abstractions` e `ERP.Fiscal.PlugNotas`. Ver [README.md](README.md#consumir-via-github-packages) e [`docs/consumers/azure-devops.md`](docs/consumers/azure-devops.md).

**Desenvolvimento desta lib:** `dotnet pack` local ou `ProjectReference` no clone deste repositório.

O ERP injeta `INfeEmissaoProvider` etc. e implementa `INfeAmbientePolicy` localmente.

---

## Build e testes

```bash
dotnet build ERP.Fiscal.slnx
dotnet test ERP.Fiscal.slnx
```

Antes de concluir alterações:

1. `dotnet build` sem erros.
2. `dotnet test` — cobrir parsers, resolvers, classificação de erro, retry e providers com `FakeHttpMessageHandler`.
3. Não adicionar dependências desnecessárias em `ERP.Fiscal.Abstractions` (deve permanecer com zero deps externas).

---

## Padrões de implementação

### ABP e DI

- Registrar serviços em `PlugNotasFiscalModule.ConfigureServices`.
- Usar `ITransientDependency` ou registro explícito conforme lifetime adequado.
- `PlugNotasHttpClient` e detalhes HTTP são **internal** à implementação PlugNotas.
- Async em toda a cadeia; sufixo `Async`; passar `CancellationToken`.
- Antes de adicionar uma abstração/helper, confirmar se ela continua útil e correta **sem qualquer referência ao ERP consumidor**.

### HTTP e erros

- Retry configurável via `PlugNotasOptions.Retry`.
- Classificar erros transient vs permanent (`PlugNotasHttpErrorClassifier`).
- Preservar `RawBody` nos DTOs de resultado para diagnóstico no ERP.
- Timeouts: NF-e ~2 min; auxiliares ~30 s (já configurados no módulo).

### Testes

- Mockar HTTP com `FakeHttpMessageHandler` (não chamar API real em CI).
- Testar parsers com fixtures JSON reais (sucesso, erro 4xx/5xx, body parcial).
- `InternalsVisibleTo` já expõe internals para o projeto de testes.

---

## Anti-patterns (proibido)

| Não fazer | Fazer |
|-----------|-------|
| EF Core, DbContext, migrations | Manter lib stateless |
| Entidades/DTOs de domínio do ERP | DTOs neutros em `Abstractions` ou contratos PlugNotas em `Contracts/` |
| Montar payload NF-e a partir de agregados | Receber `string payloadJson` pronto |
| `INfeAmbientePolicy` na lib | Contrato na lib; implementação no ERP |
| Hardcode de API keys | `PlugNotasOptions` + resolvers |
| Helpers/mapeadores que recebem `Empresa`, `Cliente`, `DocumentoFiscal`, enums ou status do ERP | Helpers neutros baseados em payloads PlugNotas, options e primitives |
| Traduzir retorno técnico para workflow/status do consumidor | Retornar DTOs neutros; ERP decide histórico, mensagens e estado |
| Duplicar lógica PlugNotas no consumidor | Centralizar HTTP/parsers/contratos neutros nesta lib |

---

## CI/CD (GitHub Actions)

A esteira de integração e entrega contínua é dividida em workflows específicos sob `.github/workflows/`:

- [**`validate-pr.yml`**](.github/workflows/validate-pr.yml): Executado em Pull Requests direcionados à `main`. Roda build, testes unitários e executa o **Smoke Test** de pacotes NuGet em um ambiente isolado (limpando e definindo o diretório temporário `NUGET_PACKAGES`) para evitar poluição do cache global.
- [**`deploy-main.yml`**](.github/workflows/deploy-main.yml): Executado em pushes na branch `main` ou tags `v*`.
  - Pushes na `main`: compila, testa, publica o pacote de desenvolvimento no GitHub Packages e realiza o **bump automático** de versão (commita e pusha com `[skip ci]`).
  - Tags `v*`: resolve a versão correspondente à tag, compila, testa e publica os pacotes de release oficial no GitHub Packages e no NuGet.org (usando `secrets.NUGET_API_KEY`).
- [**`cursor-code-review.yml`**](.github/workflows/cursor-code-review.yml): Executado em Pull Requests para a `main`. Roda o agente de revisão remota do **Cursor Reviewer**. Para evitar problemas de referências locais do Git no clone destacado, o workflow faz checkout da branch de origem do PR e faz fetch explícito de `main:refs/heads/main` localmente antes do diff.

---

## Cursor Reviewer (code review agêntico em PR)

Pipeline CI em [`.github/workflows/cursor-code-review.yml`](.github/workflows/cursor-code-review.yml) (GitHub Actions) e, opcionalmente, [`azure-pipelines-cursor-code-review.yml`](azure-pipelines-cursor-code-review.yml) (Azure DevOps) — execução **remota** via `run.sh` (repositório [cursor-reviewer](https://github.com/jpolvora/cursor-reviewer)); **não há** subprojeto local em `scripts/cursor-reviewer/`. **Não confundir** com a **skill** interna [`.agents/skills/code-review/SKILL.md`](.agents/skills/code-review/SKILL.md) (pré-push / simulação local).

**Gatilhos:** workflow `cursor-code-review.yml` em PRs para `main`; ou pipeline ADO `azure-pipelines-cursor-code-review.yml` (Build Validation).

**Pré-requisitos GitHub:** secret `CURSOR_API_KEY` em Settings → Secrets and variables → Actions. O workflow usa `GITHUB_TOKEN` com `pull-requests: write` para publicar threads. Para garantir o diff sem erro, o workflow do GitHub Actions faz checkout do branch de origem da PR (`ref: head.ref`) e cria o branch local `refs/heads/main` via fetch, satisfazendo a comparação de `--source-branch` e `--target-branch` passadas para o runner.

**Pré-requisitos ADO:** variable group com `CURSOR_API_KEY`; Build Service com *Contribute to pull requests* e *View work items*; *Allow scripts to access the OAuth token* habilitado. Detalhes: [README do cursor-reviewer](https://github.com/jpolvora/cursor-reviewer#-integração-em-cicd).

### Dry-run local (`cursor-reviewer`)

Quando o usuário pedir para simular o review localmente, usar o runner remoto `run.sh` do repositório público `cursor-reviewer`; **não** procurar um subprojeto local neste repo.

**Pré-requisitos validados:**

- `CURSOR_API_KEY` disponível no ambiente do shell atual (em Windows, confirmar com `echo "CURSOR_API_KEY set: ${CURSOR_API_KEY:+yes}"`).
- Node.js `22.13+` (CI GitHub Actions usa `24.x`; localmente qualquer LTS ≥ 22.13).
- Repositório Git com a branch alvo disponível localmente/remotamente (default: `refs/heads/main` / `origin/main`).

**Comando recomendado:**

```bash
curl -fsSL https://raw.githubusercontent.com/jpolvora/cursor-reviewer/main/run.sh | bash -s -- --dry-run --verbose --target-branch refs/heads/main
```

**Comportamento observado:**

- O runner clona a branch `release` do `cursor-reviewer` em `.tmp-cursor-reviewer`, executa `npm ci --omit=dev`, roda `node dist/index.js` apontando para o repo atual e limpa a pasta temporária ao final.
- Em modo local, o diff usado é `main...HEAD` da branch atual; para incluir mudanças não commitadas, acrescentar `--include-uncommitted`.
- A stack do ERP.Fiscal é autodetectada como **.NET/ABP** (`.slnx`, `.csproj`); o runner também lê `AGENTS.md` e regras em `.cursor/rules/` quando existirem.
- `--dry-run` não publica threads reais; imprime o JSON/previews do que seria publicado.
- Mesmo com findings, a execução termina com `exit 0`; considerar o resumo final do reviewer, não apenas o código de saída.

---

## Referências

- [`docs/security/README.md`](docs/security/README.md) — **índice** segurança (Husky, auditoria histórico, roteamento)
- [`.agents/skills/security-check/SKILL.md`](.agents/skills/security-check/SKILL.md) — procedimento de checagem e remediação
- [`.agents/skills/sync-plugnotas-docs/SKILL.md`](.agents/skills/sync-plugnotas-docs/SKILL.md) — sincronizar e manter `docs/plugnotas/` vs Swagger oficial
- [`docs/README.md`](docs/README.md) — **índice** da documentação PlugNotas compilada (carregar sob demanda)
- [`docs/plugnotas/README.md`](docs/plugnotas/README.md) — índice detalhado PlugNotas + regra de ouro lib vs consumidor
- [README.md](README.md) — visão geral e quick start
- [PlugNotas Swagger](https://docs.plugnotas.com.br) — schema canônico de campos (preferir para dúvidas de validação)
- [ABP Module Architecture](https://abp.io/docs/latest/framework/architecture/best-practices/module-architecture)
