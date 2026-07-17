# PlugNotas — Endpoints NFS-e (referência)

```yaml
agent:
  when_to_read: Implementar emissão/consulta/cancelamento NFS-e ou download XML/PDF via PlugNotas
  official: https://docs.plugnotas.com.br/#tag/NFSe
  related: 07-mapeamento-erp-fiscal.md, 08-auxiliares-cnpj-cep.md
  lib_implemented: INfseEmissaoProvider / PlugNotasNfseEmissaoProvider
```

---

## Visão rápida

NFS-e na PlugNotas é **assíncrona**: `POST /nfse` devolve id/protocolo; o ERP consulta status até `CONCLUIDO` (autorizada) ou rejeição. A lib (`INfseEmissaoProvider.EmitirCompletoAsync`) faz poll + busca XML/PDF. **Montagem do JSON** (prestador, tomador, serviços, ISS) fica no ERP consumidor.

Municípios homologados: ver [`08-auxiliares-cnpj-cep.md`](08-auxiliares-cnpj-cep.md) (`GET /nfse/cidades`).

---

## Base URL

`{baseUrl}/nfse` — host `api.plugnotas.com.br` ou `api.sandbox.plugnotas.com.br`.

Todas as rotas exigem header `x-api-key`.

---

## Rotas (ciclo de vida)

| Operação | Método | Rota | ERP.Fiscal |
|----------|--------|------|------------|
| Emitir | POST | `/nfse` | ✅ `EmitirNfseAsync` → `INfseEmissaoProvider.EmitirAsync` |
| Consultar por ID | GET | `/nfse/consultar/{id}` | ✅ `ObterNfseResumoPorIdAsync` |
| Resumo por integração | GET | `/nfse/{cnpj}/{idIntegracao}/resumo` | ✅ `ObterNfseResumoPorCnpjIdIntegracaoAsync` |
| Cancelar | POST | `/nfse/{id}/cancelamento` | ✅ `CancelarNfseAsync` |
| XML | GET | `/nfse/xml/{id}` | ✅ `ObterXmlNfsePorIdAsync` |
| PDF | GET | `/nfse/pdf/{id}` | ✅ `ObterPdfNfsePorIdAsync` |
| Cidades | GET | `/nfse/cidades` | ✅ via `INfeAuxiliaresProvider` |
| Cidade por IBGE | GET | `/nfse/cidades/{codigoIbge}` | ✅ via `INfeAuxiliaresProvider` |

---

## Fluxo assíncrono (lib)

1. ERP monta `payloadJson` (array com um documento) e valida com `PlugNotasNfsePayloadReadiness`.
2. `EmitirCompletoAsync` → `POST /nfse`.
3. Poll (até 6×, 2s) em `GET /nfse/consultar/{id}` (ou resumo por CNPJ/`idIntegracao`).
4. Situação `CONCLUIDO` → mapeada para `NfeSituacao.Autorizada`; `ChaveAcesso` recebe `codigoVerificacao` / número conforme parser.
5. Se autorizada: `GET` XML + PDF.

**Sem** `config.producao` no JSON NFS-e — ambiente só via `NfeAmbiente` no HTTP client.

---

## Payload (POST `/nfse`)

Body = **array** JSON. Contratos C#: `PlugNotasNfseDocumentPayload` em `ERP.Fiscal.PlugNotas.Contracts`.

Campos mínimos estruturais (readiness):

- `idIntegracao`, `competencia` (yyyy-MM-dd)
- `prestador.cpfCnpj`, `prestador.inscricaoMunicipal`
- `servico[]` com `codigo`, `codigoCidadeIncidencia` (IBGE 7), `discriminacao`, `iss` (exigibilidade 1–7, aliquota), `valor.servico` > 0

Mapeamento domínio → JSON: responsabilidade do ERP (ex.: `DocumentoFiscalNfsePayloadBuilder`).

---

## DTOs de resultado

A interface reutiliza `NfeEmissaoResult`, `NfeConsultaResult`, `NfeProcessamentoResult`, etc. Semântica NFS-e:

| Campo neutro | Uso típico NFS-e |
|--------------|------------------|
| `ChaveAcesso` | Código de verificação / correlato municipal |
| `NumeroNota` | `numeroNfse` |
| `Situacao` Autorizada | `situacao: CONCLUIDO` |

---

## Referências cruzadas

- Mapeamento lib: [`07-mapeamento-erp-fiscal.md`](07-mapeamento-erp-fiscal.md)
- Municípios: [`08-auxiliares-cnpj-cep.md`](08-auxiliares-cnpj-cep.md)
- Padrão consumidor: [`../consumers/padrao-integracao.md`](../consumers/padrao-integracao.md)
