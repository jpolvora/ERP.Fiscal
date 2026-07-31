# PlugNotas — índice de referência para agentes

> Compilação derivada de [docs.plugnotas.com.br](https://docs.plugnotas.com.br) e artigos Zendesk TecnoSpeed. **Não substitui** o Swagger para dicionário completo de campos.
>
> **Última verificação:** 2026-07-12 — fonte [docs.plugnotas.com.br](https://docs.plugnotas.com.br) (coleção Postman oficial + Zendesk). Skill: [sync-plugnotas-docs](../../.agents/skills/sync-plugnotas-docs/SKILL.md). NFS-e: emissão documentada em `09-nfse-endpoints.md`.

---

## Escopo desta pasta

Cobertura alinhada ao consumo via **`ERP.Fiscal.PlugNotas`**:

- Autenticação e ambientes (sandbox / homologação / produção)
- Certificado digital A1
- Cadastro de empresa emissora
- Emissão, consulta, cancelamento e download NF-e
- Emissão NFS-e (mínimo): POST `/nfse`, consulta, cancelamento, XML/PDF

Fora de escopo aqui: NFCe, MDFe, NFCom, webhooks (mencionados apenas onde impactam NF-e/NFS-e).

---

## Documentos (carregar sob demanda)

| # | Arquivo | Conteúdo |
|---|---------|----------|
| 01 | [`01-ambientes-autenticacao.md`](01-ambientes-autenticacao.md) | Hosts, `x-api-key`, sandbox vs API oficial, cadastros separados |
| 02 | [`02-certificado-digital.md`](02-certificado-digital.md) | POST/GET/PUT/DELETE `/certificado`, multipart, respostas |
| 03 | [`03-empresa-emissor.md`](03-empresa-emissor.md) | POST `/empresa`, vínculo certificado, config NF-e |
| 04 | [`04-nfe-fluxo-emissao.md`](04-nfe-fluxo-emissao.md) | Emissão assíncrona, status, polling vs webhook |
| 05 | [`05-nfe-endpoints.md`](05-nfe-endpoints.md) | Tabela de rotas NF-e + parâmetros de path |
| 06 | [`06-nfe-payload-json.md`](06-nfe-payload-json.md) | Estrutura JSON POST `/nfe` (campos usados pelos consumidores) |
| 07 | [`07-mapeamento-erp-fiscal.md`](07-mapeamento-erp-fiscal.md) | Interfaces, classes e parsers da lib |
| 08 | [`08-auxiliares-cnpj-cep.md`](08-auxiliares-cnpj-cep.md) | GET `/cnpj`, `/cep`, `/nfse/cidades` — formulários de cadastro |
| 09 | [`09-nfse-endpoints.md`](09-nfse-endpoints.md) | Rotas NFS-e + fluxo assíncrono + readiness |

---

## Regra de ouro para agentes

| Camada | Responsabilidade |
|--------|------------------|
| **ERP consumidor** | Montar payload JSON (`NfePayloadBuilder` / builder NFS-e), orquestrar estado, UI, domínio |
| **`ERP.Fiscal.PlugNotas`** | HTTP, retry, parsers, DTOs neutros — recebe JSON pronto |
| **PlugNotas** | Validação de schema, XML, SEFAZ/prefeitura, numeração automática, DANFE/PDF |

Nunca montar payload NF-e/NFS-e a partir de agregados do ERP dentro da lib. Ver [`07-mapeamento-erp-fiscal.md`](07-mapeamento-erp-fiscal.md).
