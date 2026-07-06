# PlugNotas — Endpoints NF-e (referência)

```yaml
agent:
  when_to_read: Implementar chamada HTTP específica, cancelamento, CC-e, inutilização, download DANFE/XML
  official: https://docs.plugnotas.com.br/#tag/NFe
  zendesk: https://atendimento.tecnospeed.com.br/hc/pt-br/articles/24725044940951
  lib_implemented: ver coluna "ERP.Fiscal"
```

---

## Base URL

`{baseUrl}/nfe` — prefixo após host (`api.plugnotas.com.br` ou `api.sandbox.plugnotas.com.br`).

Todas as rotas exigem header `x-api-key`.

---

## Rotas principais (emissão e ciclo de vida)

| Operação | Método | Rota | Descrição | ERP.Fiscal |
|----------|--------|------|-----------|------------|
| Emitir | POST | `/nfe` | Envio assíncrono; body = array JSON | ✅ `EmitirNfeAsync` |
| Resumo por ID | GET | `/nfe/{idNotaOrChaveOrProtocol}/resumo` | Status e dados principais | ✅ `ObterNfeResumoPorIdAsync` |
| Resumo por integração | GET | `/nfe/{cnpj}/{idIntegracao}/resumo` | Correlação ERP ↔ PlugNotas | ✅ `ObterNfeResumoPorCnpjIdIntegracaoAsync` |
| Cancelar | POST | `/nfe/{idNota}/cancelamento` | Body: `{ "justificativa": "..." }` | ✅ `CancelarNfeAsync` |
| Status cancelamento | GET | `/nfe/{idNota}/cancelamento/status` | Situação do cancelamento | ❌ (ERP pode chamar futuro) |
| XML cancelamento | GET | `/nfe/{idNotaOrChave}/cancelamento/xml` | Download XML evento | ❌ |
| Carta correção | POST | `/nfe/{idNota}/cce` | CC-e assíncrona | ❌ |
| Status CC-e | GET | `/nfe/{idNotaOrChaveOrProtocol}/cce/status` | | ❌ |
| PDF CC-e | GET | `/nfe/{idNota}/cce/pdf` | | ❌ |
| XML CC-e | GET | `/nfe/{idNota}/cce/xml` | | ❌ |
| PDF DANFE | GET | `/nfe/{idNotaOrChave}/pdf` | Bytes PDF | ✅ `ObterPdfNfePorIdAsync` |
| Reimpressão PDF | POST | `/nfe/{idNotaOrChave}/pdf/reimpressao` | | ❌ |
| XML destinatário | GET | `/nfe/{idNotaOrChave}/xml` | XML autorizado | ✅ `ObterXmlNfePorIdAsync` |
| Pré-visualização | POST | `/nfe/preview` | DANFE preview assíncrono | ❌ |
| PDF preview | GET | `/nfe/{protocol}/preview` | Por protocolo preview | ❌ |
| E-mail | POST | `/nfe/{idNota}/email` | Envio/reenvio e-mail | ❌ |
| Consulta período | GET | `/nfe/consulta/periodo` | Query: cpfCnpj, datas | ❌ |
| Inutilização | POST | `/nfe/inutilizacao` | Assíncrona | ❌ |
| Status inutilização | GET | `/nfe/inutilizacao/{protocol}/status` | | ❌ |
| Importar XML | POST | (ver Swagger) | Importação externa | ❌ |

✅ = implementado em `PlugNotasHttpClient` / `INfeEmissaoProvider` na data desta doc.

---

## Status de consulta (`/resumo`)

Valores documentados (Zendesk + Swagger):

| Status | Significado |
|--------|-------------|
| `AGENDADO` | Agendada para processamento |
| `PROCESSANDO` | Em processamento SEFAZ |
| `CONCLUIDO` | Autorizada |
| `REJEITADO` | Rejeitada |
| `CANCELADO` | Cancelada |

Cancelamento assíncrono:

| Status | Significado |
|--------|-------------|
| `CONCLUIDO` | Cancelamento aceito |
| `AGUARDANDO PROCESSAMENTO` | Pendente |
| `REJEITADO` | Cancelamento rejeitado |

---

## Parâmetros de path

| Parâmetro | Formatos aceitos |
|-----------|------------------|
| `{idNota}` | ID MongoDB PlugNotas (~24 hex) |
| `{idNotaOrChave}` | ID ou chave NF-e (44 dígitos) |
| `{idNotaOrChaveOrProtocol}` | ID, chave ou protocolo |
| `{cnpj}` | CPF/CNPJ emitente (somente dígitos) |
| `{idIntegracao}` | Identificador único do ERP |
| `{protocol}` | GUID retornado em operações assíncronas (preview, inutilização) |

A lib normaliza GUID com hífens para formato `N` quando aplicável.

---

## Eventos NF-e (reforma tributária)

Swagger documenta eventos `POST` numerados (112110, 211110, etc.) e consultas associadas. **Fora do escopo atual** da lib `ERP.Fiscal` — consultar Swagger se necessário.

---

## Notas destinadas (DF-e)

Rotas separadas na tag **Notas Destinadas** — manifestação, sincronização. Não implementadas na lib.

---

## Referência rápida HTTP

```http
# Emissão
POST https://api.plugnotas.com.br/nfe
x-api-key: {token}
Content-Type: application/json

[{ "idIntegracao": "...", ... }]

# Consulta
GET https://api.plugnotas.com.br/nfe/66958a6505757b0e34f1344a/resumo
x-api-key: {token}

# PDF
GET https://api.plugnotas.com.br/nfe/66958a6505757b0e34f1344a/pdf
x-api-key: {token}
```

Substituir host por sandbox quando aplicável ([`01-ambientes-autenticacao.md`](01-ambientes-autenticacao.md)).

---

## Próximo passo

Estrutura do JSON de emissão: [`06-nfe-payload-json.md`](06-nfe-payload-json.md).
