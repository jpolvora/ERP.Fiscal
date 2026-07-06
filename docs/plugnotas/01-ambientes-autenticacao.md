# PlugNotas — Ambientes e autenticação

```yaml
agent:
  when_to_read: Configurar PlugNotasOptions, resolver base URL, escolher API key, entender OnlySandbox
  official: https://docs.plugnotas.com.br
  related: 02-certificado-digital.md, 03-empresa-emissor.md
```

---

## Visão rápida

A API PlugNotas é **REST/HTTPS**. Toda requisição autenticada usa o header:

```http
x-api-key: {token-da-software-house}
```

O token é obtido no portal PlugNotas (conta da software house integradora).

---

## Hosts HTTP

| Ambiente | Base URL | Uso |
|----------|----------|-----|
| **Sandbox** | `https://api.sandbox.plugnotas.com.br` | Testes sem SEFAZ real; cadastro e emissão simulados |
| **API oficial** | `https://api.plugnotas.com.br` | Homologação e produção SEFAZ |

Sandbox e API oficial são **cadastros separados**. Ao sair do sandbox, é necessário **recadastrar certificado e empresa** na API oficial.

---

## Homologação vs produção (SEFAZ)

Na API oficial, homologação e produção usam o **mesmo host** (`api.plugnotas.com.br`). A distinção ocorre em:

1. **Cadastro da empresa** — `nfe.config.producao` no POST/PATCH `/empresa` (`false` = homologação, `true` = produção).
2. **Payload NF-e** — cada documento no array POST `/nfe` pode incluir `config.producao` (a lib `ERP.Fiscal` injeta via `PlugNotasNfePayloadAmbienteHelper`).

| Intenção SEFAZ | Host | `config.producao` no JSON NF-e |
|----------------|------|--------------------------------|
| Sandbox | `api.sandbox.plugnotas.com.br` | `false` |
| Homologação | `api.plugnotas.com.br` | `false` |
| Produção | `api.plugnotas.com.br` | `true` |

---

## Chaves API (`appsettings`)

Configuração típica nos ERPs consumidores:

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

| Setting | Descrição |
|---------|-----------|
| `SandboxApiKey` | Token para `api.sandbox.plugnotas.com.br`. Se vazio, a lib usa chave pública documentada do sandbox |
| `ProductionApiKey` | Token para homologação/produção em `api.plugnotas.com.br` |
| `OnlySandbox` | Se `true`, força runtime sandbox independente da intenção cadastrada no emissor |
| `TipoContrato` | Modelo de faturamento PlugNotas no cadastro empresa (`0` bilhetagem, `1` ilimitado) |

A policy `INfeAmbientePolicy` é implementada **no ERP** (não na lib).

---

## Pré-requisitos antes da emissão NF-e

Ordem obrigatória na PlugNotas (documentação oficial):

1. Obter **token** (`x-api-key`) no portal.
2. **Cadastrar certificado digital** — `POST /certificado` → guardar `data.id`.
3. **Cadastrar empresa** — `POST /empresa` com campo `certificado` = id do passo 2 e `nfe.ativo: true`.
4. **Emitir NF-e** — `POST /nfe` com JSON no body.

Alternativa: cadastro via portal web (APP2), mas a integração ERP usa as rotas acima.

---

## Content-Type por tipo de rota

| Operação | Content-Type |
|----------|--------------|
| Certificado (upload) | `multipart/form-data` |
| Empresa, NF-e emissão/cancelamento | `application/json` |
| PDF/XML (download) | Resposta binária ou XML — GET sem body |

---

## Códigos HTTP comuns

| HTTP | Significado típico |
|------|-------------------|
| 200/201 | Sucesso |
| 400 | Validação JSON/campos, senha certificado incorreta |
| 401 | Token inválido ou ausente |
| 409 | Conflito (ex.: empresa já cadastrada) |

Erros seguem padrão:

```json
{
  "error": {
    "message": "Descrição legível",
    "data": { }
  }
}
```

Sucesso cadastro:

```json
{
  "message": "Cadastro efetuado com sucesso",
  "data": { "id": "..." }
}
```

---

## Referências cruzadas

- Certificado: [`02-certificado-digital.md`](02-certificado-digital.md)
- Empresa: [`03-empresa-emissor.md`](03-empresa-emissor.md)
- Mapeamento lib: [`07-mapeamento-erp-fiscal.md`](07-mapeamento-erp-fiscal.md)
