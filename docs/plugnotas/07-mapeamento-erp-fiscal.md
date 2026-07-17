# PlugNotas — Mapeamento ERP.Fiscal

```yaml
agent:
  when_to_read: Alterar providers, parsers, HTTP client, options ou testes da lib PlugNotas
  code_root: src/ERP.Fiscal.PlugNotas
  abstractions: src/ERP.Fiscal.Abstractions
```

---

## Fronteira (regra crítica)

```
ERP (domínio, builder, orquestração)
    │  payloadJson: string
    │  NfeAmbiente + INfeAmbientePolicy (impl. ERP)
    ▼
ERP.Fiscal.Abstractions (interfaces + DTOs neutros)
    ▼
ERP.Fiscal.PlugNotas (HTTP, parsers, retry)
    ▼
api.plugnotas.com.br | api.sandbox.plugnotas.com.br
```

---

## Interfaces públicas

| Interface | Responsabilidade | Provider |
|-----------|------------------|----------|
| `INfeEmissaoProvider` | Emitir, consultar, cancelar, XML, PDF (NF-e) | `PlugNotasNfeEmissaoProvider` |
| `INfseEmissaoProvider` | Emitir, consultar, cancelar, XML, PDF (NFS-e) | `PlugNotasNfseEmissaoProvider` |
| `INfeIntegracaoProvider` | Certificado + empresa + sync ambiente | `PlugNotasIntegracaoProvider` |
| `INfeAuxiliaresProvider` | CNPJ, CEP, municípios NFS-e | `PlugNotasAuxiliaresProvider` |
| `INfeDestinadaProvider` | NF-e destinadas (DF-e) | `PlugNotasDestinadaProvider` |
| `INfeAmbientePolicy` | Ambiente efetivo (OnlySandbox) | **Implementar no ERP** ou `PlugNotasDefaultAmbientePolicy` |

Registro: `PlugNotasFiscalModule` — `[DependsOn(typeof(PlugNotasFiscalModule))]` no módulo Application do ERP.

---

## Mapeamento endpoint → HTTP client

| Documento PlugNotas | Método HTTP client | Provider |
|---------------------|-------------------|----------|
| POST `/certificado` | `CadastrarCertificadoAsync` | `CadastrarCertificadoAsync` |
| GET `/certificado/{id}` | `ObterCertificadoAsync` | `ObterCertificadoAsync` |
| POST `/empresa` | `CadastrarEmpresaAsync` | `CadastrarEmissorAsync` |
| GET `/empresa/{cnpj}` | `ObterEmpresaAsync` | `ObterEmissorAsync` |
| PATCH `/empresa/{cnpj}/config` | `AtualizarConfigEmpresaAsync` | `SincronizarAmbienteEmissorAsync` |
| POST `/nfe` | `EmitirNfeAsync` (+ retry) | `EmitirAsync` |
| GET `/nfe/{id}/resumo` | `ObterNfeResumoPorIdAsync` | `ConsultarPorIdAsync` |
| GET `/nfe/{cnpj}/{idIntegracao}/resumo` | `ObterNfeResumoPorCnpjIdIntegracaoAsync` | `ConsultarPorIdIntegracaoAsync` |
| POST `/nfe/{id}/cancelamento` | `CancelarNfeAsync` | `CancelarAsync` |
| GET `/nfe/{id}/xml` | `ObterXmlNfePorIdAsync` | `ObterXmlAsync` |
| GET `/nfe/{id}/pdf` | `ObterPdfNfePorIdAsync` | `ObterPdfAsync` |
| GET `/cnpj/{cnpj}` | — (`PlugNotasAuxiliaresProvider`) | `ConsultarCnpjAsync` |
| GET `/cep/{cep}` | — (`PlugNotasAuxiliaresProvider`) | `ConsultarCepAsync` |
| POST `/nfse` | `EmitirNfseAsync` (+ retry) | `INfseEmissaoProvider.EmitirAsync` |
| GET `/nfse/consultar/{id}` | `ObterNfseResumoPorIdAsync` | `ConsultarPorIdAsync` |
| GET `/nfse/{cnpj}/{idIntegracao}/resumo` | `ObterNfseResumoPorCnpjIdIntegracaoAsync` | `ConsultarPorIdIntegracaoAsync` |
| POST `/nfse/{id}/cancelamento` | `CancelarNfseAsync` | `CancelarAsync` |
| GET `/nfse/xml/{id}` | `ObterXmlNfsePorIdAsync` | `ObterXmlAsync` |
| GET `/nfse/pdf/{id}` | `ObterPdfNfsePorIdAsync` | `ObterPdfAsync` |
| GET `/nfse/cidades` | — (`PlugNotasAuxiliaresProvider`, cache) | `ConsultarMunicipiosAsync` |
| GET `/nfse/cidades/{codigoIbge}` | — (`PlugNotasAuxiliaresProvider`) | `ConsultarMunicipioPorIbgeAsync` |

Cliente interno NF-e/NFS-e/cadastro: `PlugNotasHttpClient`. Auxiliares: `PlugNotasAuxiliaresProvider` com `HttpClient` próprio.

Contratos JSON NFS-e: `PlugNotasNfseDocumentPayload` (`Contracts/`). Readiness: `PlugNotasNfsePayloadReadiness`. Doc de rotas: [`09-nfse-endpoints.md`](09-nfse-endpoints.md).

---

## Resolução ambiente e token

| Componente | Função |
|------------|--------|
| `PlugNotasBaseUrlResolver` | `NfeAmbiente` → base URL |
| `PlugNotasApiKeyResolver` | Ambiente → `x-api-key` |
| `PlugNotasAmbienteConstants` | URLs e chave pública sandbox |
| `PlugNotasOptions` | Configuração `appsettings` |

| `NfeAmbiente` | Base URL |
|---------------|----------|
| `Sandbox` | `api.sandbox.plugnotas.com.br` |
| `Homologacao` | `api.plugnotas.com.br` |
| `Producao` | `api.plugnotas.com.br` |

---

## Parsers

| Parser | Entrada | Saída útil |
|--------|---------|------------|
| `PlugNotasNfeEmissaoRespostaParser` | Body POST `/nfe` | `IdDocumentoProvedor`, distingue protocolo lote vs SEFAZ |
| `PlugNotasNfeConsultaRespostaParser` | Body GET `/resumo` | `StatusPlugNotas`, `cStat`, `SituacaoResumida` |
| `PlugNotasHttpErrorClassifier` | HTTP status | Retry transient vs permanent |

DTOs neutros: `NfeEmissaoResult`, `NfeConsultaResult`, `NfeProviderResult`, etc. em `Abstractions/Results/`.

---

## Payload helpers (JSON genérico)

| Helper | Função |
|--------|--------|
| `PlugNotasNfePayloadAmbienteHelper` | Injeta `config.producao` e `intermediador` no array JSON |
| `PlugNotasNfeTributosPayloadHelper` | Monta `itens[].tributos` (Simples Nacional / regime normal) |
| `PlugNotasNfeCodigoBeneficioFiscalHelper` | Normaliza e omite cBenef conforme o CST |
| `PlugNotasNfeTotalIcmsCst51Helper` | Preenche `total.valorIcms` quando há item CST ICMS 51 |
| `PlugNotasNfeNaturezaCamposHelper` | Regras `finalidade` (1–6) e combinação `presencial`/`finalidade` |
| `PlugNotasNfePayloadReadiness` | Validação mínima do documento NF-e antes de transmitir (inclui natureza) |
| `PlugNotasNfsePayloadReadiness` | Validação estrutural mínima do documento NFS-e antes de transmitir |

Usado pelo **ERP** antes de chamar `INfeEmissaoProvider` / `INfseEmissaoProvider` — não depende de entidades.

---

## Contratos JSON internos (cadastro)

| Classe | Rota |
|--------|------|
| `PlugNotasEmpresaPayload` | POST `/empresa` |
| `PlugNotasNfeConfigPayload` | Bloco `nfe` |

Internal — não expostos aos ERPs; ERP monta `NfeEmissorData` neutro.

---

## Retry e timeouts

| Operação | Retry | Timeout típico |
|----------|-------|----------------|
| POST `/nfe` | Sim (`PlugNotasOptions.Retry`) | ~2 min |
| Cadastro / consultas | Não | ~30 s (auxiliares) |

---

## Testes

Projeto: `test/ERP.Fiscal.PlugNotas.Tests/`

- `FakeHttpMessageHandler` — mock HTTP
- Fixtures JSON reais para parsers
- Testes multipart certificado

**Nunca** chamar API real em CI.

---

## Onde colocar código novo

| Tarefa | Pasta |
|--------|-------|
| Nova operação PlugNotas | `Http/PlugNotasHttpClient.cs` + `Providers/` |
| Novo parser | `Parsers/` |
| Novo DTO neutro | `Abstractions/` |
| Contrato JSON API | `Contracts/` |
| Options/DI | `Configuration/`, `PlugNotasFiscalModule.cs` |

---

## Documentação PlugNotas (esta pasta)

| Doc | Tema |
|-----|------|
| [`01-ambientes-autenticacao.md`](01-ambientes-autenticacao.md) | Hosts e tokens |
| [`02-certificado-digital.md`](02-certificado-digital.md) | Certificado |
| [`03-empresa-emissor.md`](03-empresa-emissor.md) | Empresa |
| [`04-nfe-fluxo-emissao.md`](04-nfe-fluxo-emissao.md) | Fluxo assíncrono |
| [`05-nfe-endpoints.md`](05-nfe-endpoints.md) | Rotas NF-e |
| [`06-nfe-payload-json.md`](06-nfe-payload-json.md) | JSON emissão |
| [`08-auxiliares-cnpj-cep.md`](08-auxiliares-cnpj-cep.md) | CNPJ/CEP auxiliares |

Índice geral: [`../README.md`](../README.md).
