# Padrão de integração ERP.Fiscal nos consumidores ABP

Template canônico para FiscalWR, MarchanteERP, FlorestalERP e novos ERPs.

## Pacotes e versão

```xml
<!-- common.props -->
<ErpFiscalPackageVersion>0.1.13</ErpFiscalPackageVersion>
```

| Projeto | Pacote |
|---------|--------|
| Domain | `ERP.Fiscal.Abstractions` |
| Application | `ERP.Fiscal.PlugNotas` |

Feed: [nuget.org](https://www.nuget.org) (releases). Desenvolvimento da lib: feed local `../ERP.Fiscal.sync/artifacts`.

## Branch de trabalho

Padronização fiscal: `feat/ERP.Fiscal`.

## appsettings.json

```json
{
  "PlugNotas": {
    "SandboxApiKey": "",
    "ProductionApiKey": "",
    "OnlySandbox": true,
    "TipoContrato": 1,
    "Retry": { "MaxAttempts": 3, "BaseDelayMs": 1000 },
    "ResponsavelTecnico": {
      "CpfCnpj": "", "Nome": "", "Email": "",
      "TelefoneDdd": "", "TelefoneNumero": ""
    }
  }
}
```

Variáveis de ambiente: `PlugNotas__SandboxApiKey`, `PlugNotas__ProductionApiKey`, `PlugNotas__OnlySandbox`.

**Fonte canônica de `OnlySandbox`:** `PlugNotas:OnlySandbox` em appsettings (não ABP Settings).

## Módulo Application

```csharp
[DependsOn(typeof(PlugNotasFiscalModule))]
public class XxxApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<INfeAmbientePolicy, PlugNotasDefaultAmbientePolicy>();
        context.Services.AddTransient<IFiscalAmbientePolicy, FiscalAmbientePolicy>(); // opcional: UI + enum local
        context.Services.AddTransient<IOperacoesAuxiliares, PlugNotasLibOperacoesAuxiliares>();
    }
}
```

`PlugNotasFiscalModule` registra: `INfeEmissaoProvider`, `INfeIntegracaoProvider`, `INfeAuxiliaresProvider`, `INfeDestinadaProvider`.

## Ambientes

| `NfeAmbiente` | Host API | Uso |
|---------------|----------|-----|
| `Sandbox` | `api.sandbox.plugnotas.com.br` | Dev/mock; forçado quando `OnlySandbox=true` |
| `Homologacao` | `api.plugnotas.com.br` | Homologação SEFAZ |
| `Producao` | `api.plugnotas.com.br` | Produção SEFAZ |

Cada ERP mantém enum local (`AmbienteFiscal`) + `NfeAmbienteMapper` (extensões `ToNfeAmbiente` / `ToAmbienteFiscal`).

## Emissão NF-e (fluxo canônico)

1. Montar JSON no ERP (`NfePayloadBuilder` / `DocumentoFiscalNfePayloadBuilder`).
2. Validar: `PlugNotasNfePayloadReadiness.Avaliar(doc)`.
3. Resolver ambiente: `INfeAmbientePolicy.GetAmbienteEfetivoAsync`.
4. Transmitir: **`INfeEmissaoProvider.EmitirCompletoAsync(payload, cnpj, idIntegracao, ambiente)`**.
5. Persistir `NfeProcessamentoResult` (status, chave, XML/PDF, histórico, blobs).

**Não** chamar `PlugNotasNfePayloadAmbienteHelper.AplicarProducaoNoPayloadJson` antes de `EmitirCompletoAsync` — a lib aplica internamente.

`EmitirAsync` direto: apenas testes ou reenvios customizados.

## Integração emissor/certificado

- Builder local: `NfeEmissorPayloadBuilder` → `NfeEmissorData`.
- HTTP: `INfeIntegracaoProvider` (`CadastrarEmissorAsync`, `CadastrarCertificadoAsync`, etc.).

## Auxiliares e destinadas

| Lib | Adapter ERP típico |
|-----|-------------------|
| `INfeAuxiliaresProvider` | `PlugNotasLibOperacoesAuxiliares` → `IOperacoesAuxiliares` |
| `INfeDestinadaProvider` | `PlugNotasLibConsultaNFeDestinadaClient` (FiscalWR) |

`PlugNotasHttpClient` valida CPF/CNPJ (11 ou 14 dígitos) **antes** de chamadas HTTP em `ListarNfeDestinadaAsync` e `ObterNfeResumoPorCnpjIdIntegracaoAsync`; entrada inválida retorna erro local (`HttpStatusCode=0`) sem contatar a API.

## O que fica no ERP (não promover)

- Payload builders (domínio → JSON)
- Entidades (`NotaFiscal`, `DocumentoFiscal`, `Emissor`)
- Orquestração de workflow, histórico, blobs, UI
- Regras tributárias, CFOP, natureza de operação
- `IFiscalProviderClient` + stubs (FiscalWR, dev-only) — ver seção abaixo

## Stubs dev-only (FiscalWR)

`FiscalProviderClientModeSelector` no Host substitui implementações reais por stubs quando:
- `OnlySandbox=true`, ou
- Dev/Test sem `ProductionApiKey`

Padrão opcional; Marchante/Florestal injetam providers diretamente.

## Matriz de responsabilidades

| Na lib | No ERP |
|--------|--------|
| HTTP, retry, parsers | Payload builders |
| `INfe*Provider` | App services de orquestração |
| `PlugNotasDefaultAmbientePolicy` | `IFiscalAmbientePolicy` (UI) |
| `PlugNotasNfePayloadReadiness` | Validação de domínio pré-montagem |
| Contratos JSON PlugNotas | Mapeamento domínio → contratos |
| `NfeEmissaoMensagemHelper` | Mensagens localizadas (chaves `Nfe:*`) |
