# PlugNotas — Certificado digital

```yaml
agent:
  when_to_read: Upload A1, consulta vencimento, compartilhamento matriz/filial, INfeIntegracaoProvider.CadastrarCertificadoAsync
  official: https://docs.plugnotas.com.br/#tag/Certificado
  zendesk: https://atendimento.tecnospeed.com.br/hc/pt-br/articles/360055614214-Cadastrando-Certificados-e-Empresas-via-API
  lib: PlugNotasHttpClient.CadastrarCertificadoAsync, PlugNotasIntegracaoProvider
```

---

## Requisitos do certificado

| Regra | Detalhe |
|-------|---------|
| Modelo | **A1** apenas (arquivo com chave privada) |
| Extensões aceitas | `.PFX`, `.P12`, `.CER`, `.P7` |
| Uso no ERP | Preferir `.pfx`/`.p12` com senha de instalação |
| A3 (token/smartcard) | **Não suportado** pela API |

---

## Compartilhamento matriz/filial

Um **mesmo `id` de certificado** pode ser vinculado a **várias empresas**. Não é necessário reenviar o arquivo para cada filial — reutilize o ID retornado no POST.

Para **renovar** certificado compartilhado: `PUT /certificado/{idCertificado}` cria novo registro; o ID retornado é **novo**.

---

## Endpoints

Base: `{baseUrl}/certificado` onde `baseUrl` é sandbox ou API oficial.

### POST — Cadastro de certificado

```http
POST /certificado
x-api-key: {token}
Content-Type: multipart/form-data
```

| Campo (form) | Obrigatório | Descrição |
|--------------|-------------|-----------|
| `arquivo` | Sim | Binário do certificado |
| `senha` | Sim | Senha de instalação do certificado |
| `email` | Não | E-mail para alertas de vencimento |

**Resposta 200/201:**

```json
{
  "message": "Cadastro efetuado com sucesso",
  "data": {
    "id": "5ecc441a4ea3b318cec7f999"
  }
}
```

**Erro 400 (senha incorreta):**

```json
{
  "error": {
    "message": "A senha utilizada na tentativa de upload do Certificado está incorreta.",
    "data": { "senha": "xxxx" }
  }
}
```

> Guarde `data.id` — obrigatório no POST `/empresa` (campo `certificado`).

---

### GET — Consultar certificados (lista)

```http
GET /certificado
x-api-key: {token}
```

Lista certificados da organização (paginação conforme Swagger).

---

### GET — Consultar certificado específico

```http
GET /certificado/{idCertificadoOrCpfCnpj}
x-api-key: {token}
```

Path aceita **ID MongoDB** (~24 hex) **ou** CPF/CNPJ da empresa.

**Campos típicos na resposta:**

| Campo | Descrição |
|-------|-----------|
| `id` | Identificador PlugNotas |
| `nome` | DN do certificado (gerado automaticamente) |
| `hash` | Hash do certificado |
| `vencimento` | Data/hora de expiração |
| `email` | E-mail de notificação |
| `cadastro` | ISO 8601 do upload |
| `cnpj` | CNPJ vinculado |

---

### PUT — Alterar certificado

```http
PUT /certificado/{idCertificado}
x-api-key: {token}
Content-Type: multipart/form-data
```

Mesmos campos do POST (`arquivo`, `senha`, `email`). Substitui certificado vinculado a múltiplas empresas sem recadastrar cada empresa.

**Resposta:**

```json
{
  "message": "Operação realizada com sucesso",
  "data": { "id": "5f1f67521af86a501f02c666" }
}
```

O `data.id` é um **novo** registro.

---

### DELETE — Deletar certificado

```http
DELETE /certificado/{idCertificado}
x-api-key: {token}
```

---

## Implementação na lib ERP.Fiscal

| Aspecto | Comportamento |
|---------|---------------|
| Cliente | `PlugNotasHttpClient.CadastrarCertificadoAsync` |
| Provider | `PlugNotasIntegracaoProvider.CadastrarCertificadoAsync` |
| Content-Type arquivo | `application/x-pkcs12` |
| Nome do campo multipart | `arquivo` (filename `certificado.pfx`) |
| DTO neutro entrada | `NfeCertificadoUpload` (`ArquivoBytes`, `Senha`, `EmailNotificacao`) |
| DTO neutro saída | `NfeProviderResult` com `IdProvedor`, `RawBody`, mensagens |

Consulta: `ObterCertificadoAsync` → `INfeIntegracaoProvider.ObterCertificadoAsync`.

---

## Checklist agente (certificado)

- [ ] Certificado é A1 (.pfx/.p12)
- [ ] Senha nunca retornada em DTOs de listagem (regra ERP)
- [ ] `IdProvedor` persistido após sucesso
- [ ] Ambiente correto (sandbox vs oficial) — cadastros não são compartilhados entre hosts
- [ ] Empresa cadastrada com `certificado: "{id}"` antes de emitir NF-e

---

## Próximo passo

[`03-empresa-emissor.md`](03-empresa-emissor.md) — vincular certificado à empresa e ativar NF-e.
