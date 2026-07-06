---
name: consume-erp-fiscal
description: Orientações e regras para integrar, consumir e propor melhorias à biblioteca ERP.Fiscal a partir de projetos ERP consumidores.
---

# Consumindo e Evoluindo o ERP.Fiscal

Esta skill orienta o agente de IA e os desenvolvedores do projeto ERP consumidor a consumir e interagir de forma correta e eficiente com a biblioteca de integração fiscal **`ERP.Fiscal`** (NF-e via PlugNotas), respeitando a arquitetura ABP, as fronteiras da biblioteca e promovendo código genérico para a lib principal quando necessário.

---

## 1. Como Instalar esta Skill no Consumidor

Para que o agente de IA do projeto consumidor (ex.: `FiscalWR`) utilize estas instruções automaticamente, copie esta skill para o diretório de customizações do repositório consumidor:

1. No repositório do **ERP consumidor**, crie a pasta `.agents/skills/consume-erp-fiscal/` (caso utilize customizações de agente locais).
2. Salve este arquivo como `SKILL.md` dentro de `.agents/skills/consume-erp-fiscal/SKILL.md`.
3. Certifique-se de que o agent do consumidor está configurado para ler essa pasta de skills.

---

## 2. Instalação e Atualização dos Pacotes NuGet

A biblioteca `ERP.Fiscal` é distribuída em dois pacotes NuGet:
1. **`ERP.Fiscal.Abstractions`**: Contém interfaces (`INfe*Provider`), DTOs neutros de resultados e enums. Deve ser instalado nos projetos de domínio ou aplicação do ERP consumidor.
2. **`ERP.Fiscal.PlugNotas`**: Contém a implementação concreta para a API PlugNotas e o módulo ABP. Deve ser instalado no projeto host ou de infraestrutura/web do ERP consumidor.

### A. Escolha do Feed NuGet

- **Releases Estáveis (Recomendado)**: Utilize o [nuget.org](https://www.nuget.org). Não exige autenticação no `dotnet restore`.
- **Pre-releases / Previews**: Utilize o feed do **GitHub Packages** (`https://nuget.pkg.github.com/jpolvora/index.json`). Exige autenticação.

### B. Configurando o Feed GitHub Packages (se necessário)

Se for necessário utilizar pacotes preview do GitHub Packages, crie ou edite o arquivo `nuget.config` na raiz da solution do consumidor:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/jpolvora/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="github">
      <package pattern="ERP.Fiscal.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

> [!WARNING]
> Nunca commite credenciais no arquivo `nuget.config`. Para autenticar em ambiente local ou CI/CD (ex.: Azure DevOps, GitHub Actions), passe as credenciais via linha de comando ou variáveis de ambiente.

#### Autenticação CLI local:
```bash
dotnet nuget add source "https://nuget.pkg.github.com/jpolvora/index.json" \
  --name github \
  --username "SEU_USUARIO_GITHUB" \
  --password "SEU_PERSONAL_ACCESS_TOKEN" \
  --store-password-in-clear-text
```

### C. Adicionando/Atualizando os Pacotes

Para instalar ou atualizar a versão do pacote, utilize o CLI do .NET na pasta do projeto correspondente do consumidor:

```bash
# No projeto de Domínio/Aplicação (onde define interfaces ou usa DTOs)
dotnet add package ERP.Fiscal.Abstractions --version X.Y.Z

# No projeto Host/Infraestrutura (onde registra os módulos e roda o app)
dotnet add package ERP.Fiscal.PlugNotas --version X.Y.Z
```

---

## 3. Verificação e Integração no Código Consumidor

### A. Registro do Módulo ABP

No módulo principal da aplicação do consumidor (ex.: `SeuErpHttpApiHostModule.cs` ou `SeuErpApplicationModule.cs`), adicione a dependência do módulo `PlugNotasFiscalModule`:

```csharp
using ERP.Fiscal.PlugNotas;

[DependsOn(
    typeof(PlugNotasFiscalModule)
    // outros módulos...
)]
public class SeuErpHostModule : AbpModule
{
    // ...
}
```

### B. Configuração no `appsettings.json`

Configure as credenciais e comportamento da API no arquivo de configuração do ERP consumidor:

```json
{
  "PlugNotas": {
    "SandboxApiKey": "sua-chave-sandbox-aqui",
    "ProductionApiKey": "sua-chave-producao-aqui",
    "OnlySandbox": true,
    "TipoContrato": 1,
    "Retry": {
      "MaxAttempts": 3,
      "BaseDelayMs": 1000
    }
  }
}
```

### C. Implementando a Policy de Ambiente (`INfeAmbientePolicy`)

O consumidor **deve** prover uma implementação para a interface `INfeAmbientePolicy` para resolver o ambiente fiscal efetivo da requisição.

**Padrão canônico (recomendado):** registrar `PlugNotasDefaultAmbientePolicy` da lib, que lê `PlugNotas:OnlySandbox` em appsettings:

```csharp
context.Services.AddTransient<INfeAmbientePolicy, PlugNotasDefaultAmbientePolicy>();
```

Opcionalmente, implementar `IFiscalAmbientePolicy` local para UI (`NfeAmbienteDisponibilidadeDto`) e enum `AmbienteFiscal` do domínio, delegando `INfeAmbientePolicy` à lib.

**Fonte de `OnlySandbox`:** sempre `PlugNotas:OnlySandbox` em appsettings (não ABP Settings).

### D. Emissão NF-e (`EmitirCompletoAsync`)

Fluxo canônico de transmissão:

1. Montar `payloadJson` no ERP (`NfePayloadBuilder`).
2. Validar com `PlugNotasNfePayloadReadiness.Avaliar` (lib).
3. `INfeAmbientePolicy.GetAmbienteEfetivoAsync`.
4. **`INfeEmissaoProvider.EmitirCompletoAsync(payload, cnpj, idIntegracao, ambiente)`** — a lib aplica `config.producao` e faz poll SEFAZ.
5. Persistir `NfeProcessamentoResult`.

Não chamar `PlugNotasNfePayloadAmbienteHelper` manualmente antes de `EmitirCompletoAsync`.

### F. Mensagens de erro (`NfeEmissaoMensagemHelper`)

Use `NfeEmissaoMensagemHelper.MontarMensagemErro` para compor mensagens técnicas a partir de `NfeEmissaoResult`/`NfeProcessamentoResult`, passando strings localizadas do ERP (transiente, permanente, ação). A lib não conhece chaves `Nfe:*` — só concatena e limita tamanho.

Documentação completa: [`docs/consumers/padrao-integracao.md`](../../docs/consumers/padrao-integracao.md) (no repositório ERP.Fiscal).

### E. Exemplo legado de policy customizada

```csharp
using System.Threading.Tasks;
using ERP.Fiscal.Abstractions;
using Volo.Abp.DependencyInjection;

namespace SeuErp.Fiscal;

public class SeuErpNfeAmbientePolicy : INfeAmbientePolicy, ITransientDependency
{
    private readonly ISeuConfiguracaoAppService _configuracao;

    public SeuErpNfeAmbientePolicy(ISeuConfiguracaoAppService configuracao)
    {
        _configuracao = configuracao;
    }

    public async Task<NfeAmbiente> GetAmbienteEfetivoAsync(string cnpjEmissor)
    {
        // Regra de negócio do seu ERP para definir se o emissor CNPJ emite em homologacao/producao
        var emProducao = await _configuracao.IsProducaoAsync(cnpjEmissor);
        return emProducao ? NfeAmbiente.Producao : NfeAmbiente.Sandbox;
    }
}
```

---

## 4. Fronteira e Limites de Código (Regra Crítica)

Para manter a integridade arquitetural do ecossistema, o código do ERP consumidor e a biblioteca principal possuem fronteiras rígidas. Lembre-se desta divisão ao propor novos códigos no consumidor:

| Pertence ao ERP Consumidor (Customizado) | Pertence à Lib `ERP.Fiscal` (Neutro) |
| :--- | :--- |
| **Entidades e Domínio Local**: Agregados como `NotaFiscal`, `Emissor`, `Empresa`, `ConfiguracaoFiscal`. | **Zero dependências externas** (em `Abstractions`): Abstrações neutras, DTOs de resultados e enums fiscais. |
| **Payload Builder**: A classe que lê o domínio do ERP (`NotaFiscal`) e mapeia/monta o payload JSON bruto da PlugNotas (`string payloadJson`). | **Contratos PlugNotas**: Estrutura de classes C# que representam exatamente o JSON aceito pela API da PlugNotas (`Contracts/`). |
| **Orquestração e Workflows**: Transições de estado locais da nota fiscal, persistência em banco, controle de filas, retry de negócio e envio de e-mails. | **Comunicação HTTP e Resiliência**: `PlugNotasHttpClient`, parsers de retornos HTTP, classificação de erros (transientes/permanentes) e retry HTTP. |
| **Histórico e Logs de Negócio**: Onde e como salvar as respostas da API, XMLs e PDFs no banco/Storage do ERP. | **DTOs Neutros de Comunicação**: Retornos padronizados com flags (`NfeEmissaoResult`, `NfeProviderResult`, `RawBody`). |
| **Interface com Usuário (UI)**: Telas de faturamento, wizards, listagens e formulários de emissão. | **Helpers Fiscais Neutros**: Utilitários para manipular strings, validações básicas do padrão PlugNotas ou conversões neutras. |

---

## 5. Evolução e Promoção de Features para a Lib Principal

Se durante o desenvolvimento no ERP consumidor surgir a necessidade de novas propriedades, novas chamadas de API, ou novos fluxos fiscais, siga a seguinte lógica de promoção:

### A. Regra do Teste Mental
Se a nova lógica ou propriedade necessita de conhecimento sobre o domínio ou o banco de dados do seu ERP, **ela deve ficar no consumidor**. 
Se a lógica necessita apenas do conhecimento do funcionamento da API da PlugNotas, da estrutura JSON da NF-e, ou de validações cadastrais neutras (como CNPJ e CEP), **ela deve ser desenvolvida ou promovida na biblioteca principal `ERP.Fiscal`**.

### B. O que deve ser promovido à lib `ERP.Fiscal`:
- **Novos campos no payload da NF-e**: Propriedades ausentes na modelagem de envio de notas fiscais do PlugNotas.
- **Novos endpoints de consulta**: Consultas de notas enviadas, cancelamentos, cartas de correção, etc., que a PlugNotas disponibiliza, mas que ainda não estão mapeados no `INfeEmissaoProvider`.
- **Evolução de Certificados/Empresas**: Melhorias no fluxo de envio de certificados digitais (A1) ou cadastro de emissores via `INfeIntegracaoProvider`.
- **Melhorias de CEP / CNPJ**: Adição de parâmetros de consulta ou melhoria de tratamento de erros das APIs de CEP e CNPJ no `INfeAuxiliaresProvider`.
- **Tratamento de Erros e Retries**: Novos códigos de erro HTTP da PlugNotas que precisam ser classificados como transientes ou permanentes.

### C. Como promover e evoluir:
1. Abra um Pull Request ou realize as alterações diretamente no repositório principal `jpolvora/ERP.Fiscal`.
2. Adicione os testes unitários correspondentes em `ERP.Fiscal.PlugNotas.Tests`.
3. Garanta que o pipeline do GitHub Actions valida e publica o pacote de preview.
4. Atualize a referência no ERP consumidor para a nova versão preview/estável via NuGet.
5. **Nunca** contorne limitações da lib criando clientes HTTP alternativos no ERP consumidor para chamar a API da PlugNotas diretamente! Lógica de comunicação com a PlugNotas pertence obrigatoriamente à lib.
