# ERP.Fiscal — instruções para agentes

> **Progressive disclosure:** este arquivo traz orientação universal para agentes de codificação e atua como o índice principal de regras e skills. Carregue os documentos e diretrizes adicionais **sob demanda** usando a ferramenta `view_file`.
> 
> - **Visão geral do repositório:** [`README.md`](README.md).
> - **Documentação PlugNotas (API/Integração):** [`docs/README.md`](docs/README.md).
> - **Skills e Diretrizes do Agente:** consulte o [Índice de Skills e Customizações](#skills-e-customizações-indice) abaixo.

Biblioteca de integração fiscal (NF-e via **PlugNotas**), consumidor-agnóstica. Stack: **ABP Module (.NET 10)**, backend-only, **sem EF Core, sem banco, sem entidades de domínio dos consumidores**.

---

## Skills e Customizações (Índice)

| Skill / Diretriz | Arquivo | Propósito e Contexto de Uso |
|:---|:---|:---|
| **sync-plugnotas-docs** | [`.agents/skills/sync-plugnotas-docs/SKILL.md`](file:///.agents/skills/sync-plugnotas-docs/SKILL.md) | **[Sempre neste repo]** Consulta [docs.plugnotas.com.br](https://docs.plugnotas.com.br), atualiza `docs/plugnotas/` no formato local (índice, progressive disclosure) e sugere melhorias. **Obrigatória** ao implementar features, corrigir bugs de integração ou sincronizar documentação. Regra Cursor: [`.cursor/rules/plugnotas-docs-sync.mdc`](.cursor/rules/plugnotas-docs-sync.mdc). |
| **code-review** | [`.agents/skills/code-review/SKILL.md`](file:///.agents/skills/code-review/SKILL.md) | Regras para execução de revisão de código local rigorosa comparando a branch atual com a principal. |
| **karpathy-guidelines** | [`.agents/skills/karpathy-guidelines/SKILL.md`](file:///.agents/skills/karpathy-guidelines/SKILL.md) | Boas práticas de codificação para evitar alucinações e erros comuns de LLMs. |
| **consume-erp-fiscal** | [`.agents/skills/erp-fiscal-consumer/SKILL.md`](file:///.agents/skills/erp-fiscal-consumer/SKILL.md) | **[Portável para Consumidores]** Guia de integração para ERPs que consomem esta biblioteca. Ensina a instalar/atualizar via NuGet/GitHub Packages, integrar com ABP, e gerenciar as fronteiras rígidas de código (domínio especializado local vs lógica fiscal neutra). |
| **security-check** | [`.agents/skills/security-check/SKILL.md`](file:///.agents/skills/security-check/SKILL.md) | **[Sempre neste repo]** Checagem contra vazamento de segredos, credenciais, chaves API e dados sensíveis antes de propor commits e concluir tarefas. |
| **release-nuget-package** | [`.agents/skills/release-nuget-package/SKILL.md`](file:///.agents/skills/release-nuget-package/SKILL.md) | **[Este repo]** Publicação automatizada dos pacotes NuGet (`ERP.Fiscal.Abstractions`, `ERP.Fiscal.PlugNotas`): resolve versão, dispara `Deploy Main`, tag, GitHub Release e validação em GitHub Packages / NuGet.org. Script: `scripts/release-nuget.sh`. |

> [!TIP]
> A skill **`consume-erp-fiscal`** deve ser copiada para a pasta `.agents/skills/consume-erp-fiscal/SKILL.md` no repositório de qualquer ERP que consuma esta lib. Isso garante que o agente trabalhando no ERP consumidor siga os padrões corretos de arquitetura.

---

## Sempre (toda sessão)

- Responder em **Português (pt-BR)**, salvo pedido contrário.
- Mudanças **cirúrgicas** — mínimo diff que resolve o pedido.
- Seguir padrões **ABP Framework** para módulos C# (.NET 10).
- **Documentação PlugNotas atualizada:** ao implementar features, corrigir integração ou alterar `Contracts/`/`Providers`/HTTP, seguir a skill [`sync-plugnotas-docs`](.agents/skills/sync-plugnotas-docs/SKILL.md) — consultar o Swagger em https://docs.plugnotas.com.br, cruzar com `docs/plugnotas/`, atualizar os `.md` afetados no mesmo trabalho e sugerir melhorias quando houver lacunas.
- Consultar a documentação PlugNotas via [`docs/README.md`](docs/README.md) (compilação local com índice); para schema completo de campos, usar o Swagger em https://docs.plugnotas.com.br
- Aplicar **SOLID** e **DRY**; preferir interfaces e abstrações reutilizáveis.
- **Checagem de segurança obrigatória:** antes de qualquer commit ou ao final de uma sessão, verificar se há vazamentos de chaves de API, certificados ou segredos usando a skill [`security-check`](.agents/skills/security-check/SKILL.md).

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
| Payload JSON (builder no ERP) | [`docs/plugnotas/06-nfe-payload-json.md`](docs/plugnotas/06-nfe-payload-json.md) |
| Mapeamento → `ERP.Fiscal.PlugNotas` | [`docs/plugnotas/07-mapeamento-erp-fiscal.md`](docs/plugnotas/07-mapeamento-erp-fiscal.md) |
| Consulta CNPJ/CEP (auxiliares) | [`docs/plugnotas/08-auxiliares-cnpj-cep.md`](docs/plugnotas/08-auxiliares-cnpj-cep.md) |

Não carregar todos os arquivos de uma vez — seguir a tabela **"Quando usar cada documento"** em [`docs/README.md`](docs/README.md).

---

## Fronteira da lib (regra crítica)

A lib cobre **transmissão HTTP, parsers, retry, classificação de erros, contratos PlugNotas e DTOs/helpers neutros**. Cada ERP consumidor mantém domínio, orquestração, histórico, blobs, permissões, localização e UI.

| Pertence à lib | Fica no ERP consumidor |
|---|---|
| `INfeEmissaoProvider`, `INfeIntegracaoProvider`, `INfeAuxiliaresProvider` | Agregados (`NotaFiscal`, `DocumentoFiscal`, `Emissor`/`Empresa`) |
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
├── src/
│   ├── ERP.Fiscal.Abstractions/    # zero dependências externas
│   │   ├── INfeEmissaoProvider.cs
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

---

## Interfaces públicas (contrato)

| Interface | Responsabilidade |
|-----------|------------------|
| `INfeEmissaoProvider` | Emitir, consultar, cancelar NF-e; obter XML/PDF |
| `INfeIntegracaoProvider` | Cadastro/consulta de certificado e emissor; sync de ambiente |
| `INfeAuxiliaresProvider` | Consulta CNPJ/CEP (formulários de cadastro) |
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

- [`.agents/skills/sync-plugnotas-docs/SKILL.md`](.agents/skills/sync-plugnotas-docs/SKILL.md) — sincronizar e manter `docs/plugnotas/` vs Swagger oficial
- [`.agents/skills/security-check/SKILL.md`](.agents/skills/security-check/SKILL.md) — Checagem de segurança e prevenção de vazamento de segredos
- [`docs/README.md`](docs/README.md) — **índice** da documentação PlugNotas compilada (carregar sob demanda)
- [`docs/plugnotas/README.md`](docs/plugnotas/README.md) — índice detalhado PlugNotas + regra de ouro lib vs consumidor
- [README.md](README.md) — visão geral e quick start
- [PlugNotas Swagger](https://docs.plugnotas.com.br) — schema canônico de campos (preferir para dúvidas de validação)
- [ABP Module Architecture](https://abp.io/docs/latest/framework/architecture/best-practices/module-architecture)
