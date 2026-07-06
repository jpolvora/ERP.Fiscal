# PlugNotas — Consultas auxiliares (CNPJ e CEP)

```yaml
agent:
  when_to_read: Formulários de cadastro no ERP, INfeAuxiliaresProvider, preencher endereço/razão social
  official: https://docs.plugnotas.com.br
  related: 03-empresa-emissor.md, 07-mapeamento-erp-fiscal.md
  lib: PlugNotasAuxiliaresProvider
```

---

## Visão rápida

Rotas auxiliares para **autocomplete de cadastro** (CNPJ e CEP). Não fazem parte do fluxo de emissão NF-e, mas são expostas pela lib via `INfeAuxiliaresProvider`.

Autenticação: header `x-api-key` (mesma software house). Host: sandbox ou API oficial conforme `PlugNotasOptions.OnlySandbox` ([`01-ambientes-autenticacao.md`](01-ambientes-autenticacao.md)).

---

## Endpoints

Base: `{baseUrl}` — mesmo host da API fiscal (`api.plugnotas.com.br` ou `api.sandbox.plugnotas.com.br`).

| Operação | Método | Rota | Descrição | ERP.Fiscal |
|----------|--------|------|-----------|------------|
| Consultar CNPJ | GET | `/cnpj/{cnpj}` | Dados cadastrais e endereço | ✅ `ConsultarCnpjAsync` |
| Consultar CEP | GET | `/cep/{cep}` | Logradouro, bairro, município, UF, IBGE | ✅ `ConsultarCepAsync` |

Path: CPF/CNPJ e CEP **somente dígitos** (a lib normaliza via `FiscalDigitsHelper`).

---

## GET `/cnpj/{cnpj}`

```http
GET /cnpj/18187168000160
x-api-key: {token}
```

**Campos úteis na resposta** (JSON snake_case na API; parser interno em `PlugNotasConsultaCnpjDados`):

| Campo API | Uso no ERP |
|-----------|------------|
| `razao_social` / `nome` | Razão social ou nome |
| `endereco.logradouro`, `numero`, `complemento`, `bairro` | Endereço |
| `endereco.municipio`, `endereco.uf`, `endereco.cep` | Cidade/UF/CEP |
| `telefone`, `email` | Contato |

DTO neutro: `NfeConsultaCnpjResult` (`Sucesso`, `RazaoSocial`, `Mensagem`, `RawResponse`, …).

---

## GET `/cep/{cep}`

```http
GET /cep/87020025
x-api-key: {token}
```

**Campos úteis na resposta** (`PlugNotasConsultaCepDados`):

| Campo API | Uso no ERP |
|-----------|------------|
| `logradouro`, `bairro`, `complemento` | Endereço |
| `municipio`, `uf`, `cep` | Localidade |
| `ibge` / `codigo_ibge` / `codigo_municipio` | Código IBGE (normalizado no provider) |

DTO neutro: `NfeConsultaCepResult`.

---

## Implementação ERP.Fiscal

| Aspecto | Comportamento |
|---------|---------------|
| Provider | `PlugNotasAuxiliaresProvider` |
| Interface | `INfeAuxiliaresProvider` |
| HTTP | `HttpClient` dedicado (~30 s timeout no módulo) |
| Ambiente | `OnlySandbox` → sandbox; senão `ProductionApiKey` ou fallback sandbox |
| Retry | Não |

---

## Erros

Mesmo padrão da API PlugNotas (`error.message`, HTTP 4xx/5xx). `RawResponse` preservado nos DTOs para diagnóstico no ERP.

---

## Referências cruzadas

- Empresa (cadastro completo): [`03-empresa-emissor.md`](03-empresa-emissor.md)
- Mapeamento lib: [`07-mapeamento-erp-fiscal.md`](07-mapeamento-erp-fiscal.md)
