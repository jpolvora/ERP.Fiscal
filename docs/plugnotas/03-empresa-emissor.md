# PlugNotas — Empresa emissora

```yaml
agent:
  when_to_read: Cadastro emissor no provedor, sync ambiente, POST /empresa, readiness emissão NF-e
  official: https://docs.plugnotas.com.br/#tag/Empresa
  zendesk: https://atendimento.tecnospeed.com.br/hc/pt-br/articles/360055614214-Cadastrando-Certificados-e-Empresas-via-API
  lib: PlugNotasHttpClient.CadastrarEmpresaAsync, PlugNotasIntegracaoProvider
```

---

## Papel no fluxo

A empresa cadastrada na PlugNotas concentra dados fiscais do emitente. Na emissão NF-e, a API **busca o cadastro** para completar XML, numeração e configurações — o JSON de envio não precisa repetir todos os dados cadastrais se emitente já estiver registrado.

Pré-requisito: **certificado** cadastrado ([`02-certificado-digital.md`](02-certificado-digital.md)).

---

## POST — Cadastrar empresa

```http
POST /empresa
x-api-key: {token}
Content-Type: application/json
```

### Campos principais

| Campo | Obrigatório NF-e | Descrição |
|-------|------------------|-----------|
| `cpfCnpj` | Sim | CPF/CNPJ apenas dígitos |
| `razaoSocial` | Sim | Razão social |
| `nomeFantasia` | Recomendado | Nome fantasia |
| `inscricaoEstadual` | Sim (NF-e) | Somente algarismos; `ISENTO` quando aplicável |
| `inscricaoMunicipal` | NFS-e | Obrigatório para serviço; dinâmico por município |
| `certificado` | Sim | ID retornado por `POST /certificado` |
| `simplesNacional` | Sim | `true` / `false` |
| `regimeTributario` | Sim | Ver tabela abaixo |
| `regimeTributarioEspecial` | Sim | Ver tabela abaixo |
| `incentivoFiscal` | Sim | `true` / `false` |
| `incentivadorCultural` | Sim | `true` / `false` |
| `email` | Sim | E-mail da empresa |
| `endereco` | Sim | Logradouro, número, bairro, CEP, `codigoCidade` (IBGE), `estado` |
| `telefone` | Opcional | `ddd`, `numero` |
| `nfe` | Sim (emissão produto) | Bloco de configuração NF-e |

### `regimeTributario`

| Valor | Significado |
|-------|-------------|
| 0 | Nenhum |
| 1 | Simples Nacional |
| 2 | Simples Nacional — Excesso |
| 3 | Regime Normal — Lucro Presumido |
| 4 | Normal — Lucro Real |
| 5 | MEI (CRT 4 no XML) |

### `regimeTributarioEspecial`

Valores 0–11 conforme Swagger (0 = sem regime especial, 5 = MEI, 7 = Lucro Real, etc.).

### Bloco `nfe` (NF-e)

```json
"nfe": {
  "ativo": true,
  "tipoContrato": 0,
  "config": {
    "producao": false,
    "impressaoFcp": true,
    "impressaoPartilha": true,
    "serie": 1,
    "numero": 1,
    "dfe": { "ativo": true },
    "email": { "envio": true }
  }
}
```

| Campo | Descrição |
|-------|-----------|
| `ativo` | Habilita emissão NF-e para a empresa |
| `tipoContrato` | `0` bilhetagem, `1` ilimitado |
| `config.producao` | `false` homologação, `true` produção SEFAZ |
| `config.serie` / `numero` | Numeração inicial quando automática desligada |
| `config.dfe.ativo` | Distribuição DF-e |
| `config.email.envio` | Envio automático de e-mail pós-autorização |

A lib `ERP.Fiscal` acrescenta objetos vazios `nfce`, `mdfe`, `nfcom` exigidos pela API e normaliza `tipoContrato` via `PlugNotasOptions.TipoContrato`.

---

## Exemplo mínimo (adaptado documentação oficial)

```json
{
  "cpfCnpj": "29062609000177",
  "inscricaoEstadual": "1234567850",
  "razaoSocial": "Empresa Exemplo LTDA",
  "nomeFantasia": "Exemplo",
  "certificado": "5af59d271f6e8f409178fbf3",
  "simplesNacional": true,
  "regimeTributario": 1,
  "regimeTributarioEspecial": 5,
  "incentivoFiscal": false,
  "incentivadorCultural": false,
  "endereco": {
    "logradouro": "Rua Exemplo",
    "numero": "100",
    "bairro": "Centro",
    "codigoCidade": "4115200",
    "estado": "PR",
    "cep": "87020-025"
  },
  "email": "fiscal@empresa.com.br",
  "nfe": {
    "ativo": true,
    "tipoContrato": 1,
    "config": {
      "producao": false,
      "serie": 1,
      "numero": 1,
      "email": { "envio": true }
    }
  }
}
```

---

## Respostas

**Sucesso:**

```json
{
  "message": "Cadastro efetuado com sucesso",
  "data": { "cnpj": "23995875000176" }
}
```

**Erro validação:**

```json
{
  "error": {
    "message": "Falha na validação do JSON de Empresa",
    "data": {
      "fields": {
        "certificado": "Preenchimento obrigatório"
      }
    }
  }
}
```

HTTP possíveis: 400, 401, 409.

---

## Outras rotas empresa (referência)

| Método | Rota | Uso |
|--------|------|-----|
| GET | `/empresa` | Listar (paginação ~150/registro) |
| GET | `/empresa/{cpfCnpj}` | Consultar uma empresa |
| PATCH | `/empresa/{cpfCnpj}/config` | Alterar config (ex.: `nfe.config.producao`) |
| PUT | `/empresa/{cpfCnpj}` | Alterar cadastro completo |

A lib implementa `CadastrarEmpresaAsync`, `ObterEmpresaAsync`, `AtualizarConfigEmpresaAsync` (sync ambiente produção/homologação).

---

## Implementação ERP.Fiscal

| Operação | Interface |
|----------|-----------|
| Cadastrar | `INfeIntegracaoProvider.CadastrarEmissorAsync(NfeEmissorData)` |
| Consultar | `ObterEmissorAsync` |
| Sync ambiente | `SincronizarAmbienteEmissorAsync` → PATCH config |

Contrato JSON interno: `PlugNotasEmpresaPayload` em `Contracts/`.

---

## Próximo passo

Com certificado + empresa ativos: [`04-nfe-fluxo-emissao.md`](04-nfe-fluxo-emissao.md).
