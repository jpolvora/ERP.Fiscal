# ERP.Fiscal

Biblioteca .NET de integração fiscal (NF-e via [PlugNotas](https://docs.plugnotas.com.br)), pensada para consumo por aplicações ABP. Centraliza HTTP, parsers, retry, classificação de erros e contratos neutros — sem acoplar domínio de ERP.

## Projetos

| Projeto | Responsabilidade |
|---|---|
| `src/ERP.Fiscal.Abstractions` | Interfaces provider-agnósticas (`INfeEmissaoProvider`, `INfeIntegracaoProvider`, `INfeAuxiliaresProvider`, `INfeAmbientePolicy`) e DTOs neutros de resultado. Zero dependências externas. |
| `src/ERP.Fiscal.PlugNotas` | Implementação PlugNotas das abstrações acima: módulo ABP plugável (`PlugNotasFiscalModule`), configuração, cliente HTTP interno, parsers e providers. |

## Escopo

A lib cobre **transmissão, verificação, tratamento de erros e retorno** da integração PlugNotas
(NF-e, certificado digital e cadastro de emissor/empresa). Cada aplicação consumidora:

- monta o **payload JSON** da NF-e a partir do seu próprio domínio (produtos, cliente, tributos) e
  passa a string pronta para `INfeEmissaoProvider.EmitirAsync`;
- monta os DTOs neutros de emissor/certificado (`NfeEmissorData`, `NfeCertificadoUpload`) a partir das
  suas próprias entidades;
- implementa `INfeAmbientePolicy` localmente (depende de configuração/Settings da aplicação).

Entidades, DTOs de domínio e regras de negócio fiscais **não** entram na lib.

## Documentação

Índice da documentação PlugNotas compilada em Markdown: [`docs/README.md`](docs/README.md).

| Documento | Conteúdo |
|-----------|----------|
| [`docs/plugnotas/README.md`](docs/plugnotas/README.md) | Índice PlugNotas, escopo e regra lib vs consumidor |
| [`docs/plugnotas/01-ambientes-autenticacao.md`](docs/plugnotas/01-ambientes-autenticacao.md) | Hosts, API key, sandbox |
| [`docs/plugnotas/02-certificado-digital.md`](docs/plugnotas/02-certificado-digital.md) | Certificado A1 |
| … | Ver índice completo em [`docs/README.md`](docs/README.md) |

Instruções para agentes de IA: [`AGENTS.md`](AGENTS.md).

## Dependências

| Pacote | Versão | Uso |
|--------|--------|-----|
| [Volo.Abp.Core](https://www.nuget.org/packages/Volo.Abp.Core) | 10.3.0 | Módulo ABP plugável |
| [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) | 10.0.0 | `HttpClient` factory |

Documentação oficial da API: [docs.plugnotas.com.br](https://docs.plugnotas.com.br)

## Uso (consumo em um ERP ABP)

```csharp
[DependsOn(
    typeof(PlugNotasFiscalModule),
    // ... demais dependências do módulo Application
)]
public class MeuErpApplicationModule : AbpModule
{
}
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

## Build e testes

```bash
dotnet build ERP.Fiscal.slnx
dotnet test ERP.Fiscal.slnx
```

### Pacotes NuGet

O workflow [`.github/workflows/ci.yml`](.github/workflows/ci.yml) gera dois pacotes a cada build:

| Pacote | Descrição |
|--------|-----------|
| `ERP.Fiscal.Abstractions` | Interfaces e DTOs neutros |
| `ERP.Fiscal.PlugNotas` | Implementação PlugNotas (ABP) |

- **Push em `main`:** artefatos `0.1.0-preview.{run}` (download em Actions → Artifacts).
- **Tag `v*`** (ex.: `v1.0.0`): mesma versão no pack; publicação em [nuget.org](https://www.nuget.org) se o secret `NUGET_API_KEY` estiver configurado no repositório.

Pack local:

```bash
dotnet pack ERP.Fiscal.slnx -c Release -o ./artifacts/packages
```

## Licença

[MIT](LICENSE)
