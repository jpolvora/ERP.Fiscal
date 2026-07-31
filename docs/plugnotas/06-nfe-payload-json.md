# PlugNotas — Payload JSON NF-e

```yaml
agent:
  when_to_read: Implementar NfePayloadBuilder no ERP, validar campos tributários, serializar POST /nfe
  official: https://docs.plugnotas.com.br/#tag/NFe/operation/addNFe
  note: Dicionário completo de centenas de campos está no Swagger — este doc cobre o contrato típico usado pelos consumidores
  lib: payload é string JSON pronta; PlugNotasNfePayloadAmbienteHelper injeta config.producao
```

---

## Formato de envio

```json
[
  {
    "idIntegracao": "guid32hexSemHifens",
    "config": { "producao": false },
    ...
  }
]
```

| Regra | Detalhe |
|-------|---------|
| Raiz | **Array** — um objeto por NF-e |
| `idIntegracao` | Obrigatório; único; correlaciona com ERP |
| `config.producao` | Injetado pela lib antes do HTTP se usar helper |
| `intermediador` | `"0"` = sem marketplace (padrão ERP) |

O exemplo oficial no Swagger representa **um caso** (Regime Normal, tributação isenta). Outros regimes exigem campos tributários diferentes.

---

## Documento NF-e — campos de cabeçalho

Referência cruzada layout NF-e (tags XML entre parênteses na doc PlugNotas).

| Campo JSON | Tag NF-e | Tipo | Observação |
|------------|----------|------|------------|
| `idIntegracao` | — | string | ID único ERP |
| `versaoManual` | A02 | string | Auto se omitido |
| `codigoNumerico` | B03 | string | Auto; não sequencial |
| `numero` | B08 | int | Obrigatório se numeração manual |
| `serie` | B07 | string | Série NF-e |
| `finalidade` | B25 | int | 1 Normal, 2 Complementar, 3 Ajuste, 4 Devolução, 5 Crédito, 6 Débito |
| `natureza` | B04 | string | Natureza da operação |
| `dataEmissao` | B09 | ISO 8601 | `YYYY-MM-DDTHH:mm:ss-03:00` ou `YYYY-MM-DD` |
| `dataSaida` | B10 | ISO 8601 | Opcional |
| `tipo` | B11 | int | 0 entrada, 1 saída |
| `presencial` | B25b | string | 0–9 indicador presença. Com valor `0` (não se aplica), a PlugNotas só aceita `finalidade` 2 (complementar) ou 3 (ajuste). |
| `consumidorFinal` | B25a | bool | Destinatário consumidor final |
| `tipoImpressao` | B21 | int | 0–3 DANFE |
| `tipoEmissao` | B22 | int | 1 normal, contingências 2/5/6/7 |
| `destinoOperacao` | B11a | int | 1 interna, 2 interestadual, 3 exterior |
| `informacoesComplementares` | Z03 | string | infCpl; `\|` quebra linha |
| `informacoesFisco` | Z02 | string | infAdFisco |

Campos de **Reforma Tributária** (NT 2025.002): `tipoNotaDebito`, `tipoNotaCredito`, grupos IBS/CBS nos itens — ver Swagger para cenários específicos.

---

## Emitente e destinatário

### `emitente`

Mínimo nos ERPs (cadastro completo já está na PlugNotas):

```json
"emitente": {
  "cpfCnpj": "12345678000199",
  "inscricaoEstadual": "1234567890"
}
```

### `destinatario`

```json
"destinatario": {
  "cpfCnpj": "98765432000100",
  "inscricaoEstadual": "ISENTO",
  "indicadorInscricaoEstadual": 9,
  "razaoSocial": "Cliente Exemplo LTDA",
  "email": "cliente@email.com",
  "endereco": {
    "logradouro": "Rua A",
    "numero": "50",
    "bairro": "Centro",
    "cep": "87000000",
    "codigoCidade": "4115200",
    "descricaoCidade": "Maringá",
    "estado": "PR"
  }
}
```

| `indicadorInscricaoEstadual` | Significado |
|------------------------------|-------------|
| 1 | Contribuinte ICMS |
| 2 | Isento |
| 9 | Não contribuinte |

---

## Itens (`itens[]`)

| Campo | Descrição |
|-------|-----------|
| `codigo` | Código interno produto |
| `descricao` | Descrição item |
| `ncm` | NCM 8 dígitos |
| `cest` | CEST quando aplicável |
| `cfop` | CFOP |
| `codigoBeneficioFiscal` | cBenef UF — omitir se vazio ou se o CST não admitir o campo |
| `valorUnitario.comercial` | Valor unitário |
| `valor` | Valor total item (vProd) |
| `unidade.comercial` / `tributavel` | Siglas ENCAT (ex.: `UN`, `TON`) |
| `quantidade.comercial` / `tributavel` | Quantidades |
| `tributos` | ICMS, PIS, COFINS (e IBS/CBS se aplicável) |

### Exemplo tributos (Simples / isento — adaptar por CST)

```json
"tributos": {
  "icms": {
    "origem": "0",
    "cst": "40",
    "baseCalculo": { "modalidadeDeterminacao": 0, "valor": 0 }
  },
  "pis": {
    "cst": "99",
    "baseCalculo": { "valor": 0, "quantidade": 0 },
    "aliquota": 0,
    "valor": 0
  },
  "cofins": {
    "cst": "07",
    "baseCalculo": { "valor": 0 },
    "aliquota": 0,
    "valor": 0
  }
}
```

PlugNotas **calcula impostos ausentes** quando possível, mas o ERP deve enviar CST/regime coerente com cadastro da empresa.

Montagem reutilizável do bloco `tributos` (Simples Nacional e regime normal): `PlugNotasNfeTributosPayloadHelper` em `ERP.Fiscal.PlugNotas/Payload/`.

`PlugNotasNfeCodigoBeneficioFiscalHelper` normaliza o cBenef e só o inclui para os CSTs
`20`, `30`, `40`, `41`, `50`, `51`, `70` e `90`. A ausência do código não deve
bloquear localmente a emissão; a exigência varia por UF e é validada pelo provedor/SEFAZ.

---

## Totalizadores (`total`)

| Campo JSON | Tipo | Quando enviar |
|------------|------|----------------|
| `valorIcms` | decimal | **Somente** quando há item com `tributos.icms.cst` = `51` (diferimento). Valor = soma dos `tributos.icms.valor` dos itens CST 51. Casing conforme Swagger/Postman oficial (`valorIcms`, não `valorICMS`). |

Workaround Tecnospeed/PlugNotas para evitar rejeição Sefaz *"Total do ICMS difere do somatório dos itens"*. O agrupador `total` deve conter **apenas** `valorIcms` nesse cenário; omitir `total` quando não houver CST 51.

### Posição na árvore JSON (POST `/nfe`)

O corpo é um **array na raiz**; cada elemento é um documento. `total` fica na **raiz do documento**, como irmão de `itens` — **não** dentro de `itens` nem de `tributos`:

```
[nfe]                          ← array na raiz do POST
└── documento[0]
    ├── idIntegracao, natureza, finalidade, ...
    ├── itens[]
    │   └── [i]
    │       └── tributos
    │           └── icms
    │               ├── cst
    │               └── valor          ← ICMS do item (ex.: CST 51)
    └── total
        └── valorIcms                  ← soma dos itens[i].tributos.icms.valor com CST 51
```

Contrato C#: `PlugNotasNfeDocumentPayload.Total` → JSON `total`; `PlugNotasNfeTotalPayload.ValorIcms` → JSON `valorIcms` (`[JsonPropertyName("valorIcms")]`).

```json
"total": {
  "valorIcms": 5100
}
```

Helper da lib (chamar no builder do ERP **antes** de serializar o POST array):

```csharp
PlugNotasNfeTotalIcmsCst51Helper.AplicarTotalValorIcmsQuandoCst51(doc);
```

Para payload já serializado, use
`AplicarTotalValorIcmsQuandoCst51NoPayloadJson(payloadJson)`.

---

## Pagamentos (`pagamentos[]`)

```json
"pagamentos": [
  {
    "aVista": true,
    "meio": "01",
    "valor": 1000.00
  }
]
```

`meio`: tabela NF-e (01 dinheiro, 15 boleto, 99 outros, etc.).

---

## Transporte (`transporte`)

```json
"transporte": {
  "modalidadeFrete": 9,
  "transportadora": {
    "cpfCnpj": "11222333000181",
    "razaoSocial": "Transportadora XYZ"
  },
  "veiculo": {
    "placa": "ABC1D23",
    "uf": "PR"
  }
}
```

`modalidadeFrete`: 0–9 conforme NF-e (9 = sem frete).

---

## Responsável técnico (`responsavelTecnico`)

Opcional; pode ser sobrescrito via `PlugNotasOptions.ResponsavelTecnico` na lib.

```json
"responsavelTecnico": {
  "cpfCnpj": "12345678000199",
  "nome": "Empresa Software",
  "email": "dev@software.com",
  "telefone": { "ddd": "44", "numero": "30379500" }
}
```

---

## Config por documento

```json
"config": {
  "producao": false,
  "email": { "envio": true }
}
```

Helper da lib:

```csharp
PlugNotasNfePayloadAmbienteHelper.AplicarProducaoNoPayloadJson(json, NfeAmbiente.Producao);
```

---

## Contrato C# de referência (consumidor)

Tipos espelhando JSON (replicar/adaptar na aplicação consumidora):

- `PlugNotasNfeDocumentPayload` — documento raiz
- `PlugNotasNfeTotalPayload` — agrupador `total` (workaround CST 51)
- `PlugNotasNfeItemPayload`, `PlugNotasNfeTributosItemPayload`
- `PlugNotasNfeEmitentePayload`, `PlugNotasNfeDestinatarioPayload`

Contratos parciais da API PlugNotas espelhados na lib: `src/ERP.Fiscal.PlugNotas/Contracts/PlugNotasNfeDocumentContracts.cs`.

Builders de payload a partir do domínio **não** pertencem a `ERP.Fiscal` — ficam no consumidor.

---

## Validações PlugNotas (POST)

- Schema JSON (campos obrigatórios, tipos, enums)
- `finalidade`: inteiros **1–6** apenas
- `presencial` = `"0"` (não se aplica): `finalidade` deve ser **2** (complementar) ou **3** (ajuste) — combinação inválida gera `error.data.fields.documento[0].finalidade`
- Regras de negócio (IE, CFOP, CST compatível, etc.)
- Empresa e certificado devem existir no mesmo ambiente (host)

Helper neutro na lib: `PlugNotasNfeNaturezaCamposHelper` + `PlugNotasNfePayloadReadiness.Avaliar` (validação pré-transmissão no ERP).

Erros retornam HTTP 400 com `error.message` e `error.data.fields` quando aplicável.

---

## Próximo passo

Como a lib consome o JSON: [`07-mapeamento-erp-fiscal.md`](07-mapeamento-erp-fiscal.md).
