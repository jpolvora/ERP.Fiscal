# ERP.Fiscal — instruções para agentes

> **Progressive disclosure:** este arquivo traz orientação universal para agentes de codificação.
> - Documentação PlugNotas compilada (índice): [`docs/README.md`](docs/README.md) — carregar **sob demanda** o `.md` indicado pelo contexto.
> - Visão geral do repositório: [`README.md`](README.md).

Biblioteca de integração fiscal (NF-e via **PlugNotas**), consumidor-agnóstica. Stack: **ABP Module (.NET 10)**, backend-only, **sem EF Core, sem banco, sem entidades de domínio dos consumidores**.

---

## Sempre (toda sessão)

- Responder em **Português (pt-BR)**, salvo pedido contrário.
- Mudanças **cirúrgicas** — mínimo diff que resolve o pedido.
- Seguir padrões **ABP Framework** para módulos C# (.NET 10).
- Consultar a documentação PlugNotas via [`docs/README.md`](docs/README.md) (compilação local com índice); para schema completo de campos, usar o Swagger em https://docs.plugnotas.com.br
- Aplicar **SOLID** e **DRY**; preferir interfaces e abstrações reutilizáveis.

---

## Documentação (PlugNotas)

**Ponto de entrada:** [`docs/README.md`](docs/README.md) — índice da documentação oficial compilada em `.md` (jul/2026), com roteamento por contexto de tarefa.

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

Referência de projeto (dev) ou `PackageReference` (NuGet, pós-estabilização). O ERP injeta `INfeEmissaoProvider` etc. e implementa `INfeAmbientePolicy` localmente.

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

## Referências

- [`docs/README.md`](docs/README.md) — **índice** da documentação PlugNotas compilada (carregar sob demanda)
- [`docs/plugnotas/README.md`](docs/plugnotas/README.md) — índice detalhado PlugNotas + regra de ouro lib vs consumidor
- [README.md](README.md) — visão geral e quick start
- [PlugNotas Swagger](https://docs.plugnotas.com.br) — schema canônico de campos (preferir para dúvidas de validação)
- [ABP Module Architecture](https://abp.io/docs/latest/framework/architecture/best-practices/module-architecture)
