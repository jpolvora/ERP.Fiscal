# ERP.Fiscal

Biblioteca .NET de integração fiscal (NF-e via [PlugNotas](https://docs.plugnotas.com.br)), pensada para consumo por aplicações [ABP](https://abp.io). Centraliza HTTP, parsers, retry, classificação de erros e contratos neutros — **sem acoplar domínio de ERP**.

Pacotes publicados automaticamente no **GitHub Packages** a cada push em `main` e no **[nuget.org](https://www.nuget.org)** (versões `0.1.x` e releases com tag `v*`).

---

## Links dos pacotes

| Pacote | nuget.org | GitHub Packages |
|--------|-----------|-----------------|
| **ERP.Fiscal.Abstractions** | [nuget.org/…/0.1.3](https://www.nuget.org/packages/ERP.Fiscal.Abstractions/0.1.3) | [pkgs/nuget/ERP.Fiscal.Abstractions](https://github.com/jpolvora/ERP.Fiscal/pkgs/nuget/ERP.Fiscal.Abstractions) |
| **ERP.Fiscal.PlugNotas** | [nuget.org/…/0.1.3](https://www.nuget.org/packages/ERP.Fiscal.PlugNotas/0.1.3) | [pkgs/nuget/ERP.Fiscal.PlugNotas](https://github.com/jpolvora/ERP.Fiscal/pkgs/nuget/ERP.Fiscal.PlugNotas) |

> Última versão publicada: **0.1.3**. Novas versões `0.1.x` seguem o contador em [`nuget.props`](nuget.props) após cada merge em `main`.

---

## Funcionalidades

| Área | O que a lib faz |
|------|-----------------|
| **Emissão NF-e** | `INfeEmissaoProvider` — emitir, consultar (por id ou `idIntegracao`), cancelar, obter XML/PDF, fluxo com polling (`EmitirCompletoAsync`) |
| **Integração cadastral** | `INfeIntegracaoProvider` — certificado A1, cadastro/consulta de emissor, sync de ambiente (`producao`) |
| **Auxiliares** | `INfeAuxiliaresProvider` — consulta CNPJ e CEP (formulários de cadastro) |
| **Ambiente** | Contrato `INfeAmbientePolicy` (implementado no consumidor) + resolvers sandbox/produção |
| **Resiliência** | Retry configurável, classificação transient/permanent, `RawBody` nos resultados |
| **PlugNotas** | Módulo ABP `PlugNotasFiscalModule`, options via `appsettings`, chave sandbox pública documentada |
| **Qualidade** | 91 testes unitários, CI GitHub Actions, smoke test de consumo NuGet |

### Fronteira lib vs consumidor

| Fica na lib | Fica no consumidor |
|-------------|-------------------|
| HTTP, parsers, retry, contratos PlugNotas, DTOs neutros | Entidades de domínio, payload builders, orquestração, histórico, UI |
| Recebe `string payloadJson` pronta | Monta JSON NF-e a partir do domínio |
| Contrato `INfeAmbientePolicy` | Implementação da policy (Settings locais) |

---

## Pacotes NuGet

| Pacote | Descrição | Dependências |
|--------|-----------|--------------|
| [ERP.Fiscal.Abstractions](https://www.nuget.org/packages/ERP.Fiscal.Abstractions) | Interfaces (`INfe*Provider`) e DTOs neutros | Nenhuma externa |
| [ERP.Fiscal.PlugNotas](https://www.nuget.org/packages/ERP.Fiscal.PlugNotas) | Implementação PlugNotas + módulo ABP | `ERP.Fiscal.Abstractions`, Volo.Abp.Core, Microsoft.Extensions.Http |

Feeds: [nuget.org](https://www.nuget.org) (recomendado) ou [GitHub Packages](https://github.com/jpolvora/ERP.Fiscal/pkgs/nuget) — ver [Links dos pacotes](#links-dos-pacotes).

**Target:** .NET 10 (`net10.0`)

### Versionamento

| Gatilho | Versão | Onde publica |
|---------|--------|--------------|
| PR para `main` | — (pack local `0.1.{N}-pr.*` só para smoke test) | — |
| Merge/push em `main` | `0.1.{N}` | GitHub Packages + [nuget.org](https://www.nuget.org) + artefato CI |
| Tag `v1.0.0` | `1.0.0` | GitHub Packages + nuget.org (se `NUGET_API_KEY` configurado) |

O contador `PackagePatchNumber` é commitado automaticamente na `main` após cada publicação bem-sucedida (`chore: bump package version … [skip ci]`).

---

## Consumir via GitHub Packages

### URL do feed (importante)

O feed NuGet do GitHub é por **owner/organização**, não por repositório:

```text
https://nuget.pkg.github.com/jpolvora/index.json
```

| Campo | Valor |
|-------|-------|
| **Owner GitHub** | `jpolvora` |
| **Repositório dos fontes** | `jpolvora/ERP.Fiscal` |
| **URL do feed NuGet** | `https://nuget.pkg.github.com/jpolvora/index.json` |
| **Package IDs** | `ERP.Fiscal.Abstractions`, `ERP.Fiscal.PlugNotas` |

> Pacotes NuGet no GitHub **exigem autenticação** no restore, mesmo quando o repositório é público.

### Visibilidade do pacote

Pacotes publicados pelo CI deste repositório (via `GITHUB_TOKEN`) ficam vinculados ao owner **`jpolvora`** e herdam a visibilidade do repositório fonte.

| Cenário | O que fazer |
|---------|-------------|
| Consumidor na **mesma conta** (`jpolvora`) | `GITHUB_TOKEN` em Actions ou PAT com `read:packages` |
| Consumidor em **outra org/conta** | PAT com `read:packages` (e `repo` se o pacote for privado) com acesso de leitura aos pacotes de `jpolvora` |
| Repo público, pacote público | Auth **ainda é obrigatória** no `dotnet restore` |

### 1. Autenticação (máquina de desenvolvimento)

Crie um [Personal Access Token](https://github.com/settings/tokens) (classic) com escopo **`read:packages`**. Se o repositório for privado, inclua também **`repo`**.

**Opção A — CLI (recomendado, uma vez por máquina):**

```bash
dotnet nuget add source "https://nuget.pkg.github.com/jpolvora/index.json" \
  --name github \
  --username SEU_USUARIO_GITHUB \
  --password SEU_PAT \
  --store-password-in-clear-text
```

**Opção B — variável de ambiente (CI local / scripts):**

```bash
export NUGET_AUTH_TOKEN=SEU_PAT
# username = GitHub username; password = PAT
```

**Opção C — Visual Studio:** *Tools → NuGet Package Manager → Package Sources* → adicionar a URL acima e credenciais.

### 2. `nuget.config` no projeto consumidor

Coloque na **raiz da solution** do seu ERP (ou em `samples/`, como referência). O `dotnet restore` procura o arquivo no diretório atual e nos pais.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
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

Referência completa: [`samples/nuget.config`](samples/nuget.config).

O `packageSourceMapping` garante que apenas `ERP.Fiscal.*` venha do GitHub; o restante continua no nuget.org.

### 3. `PackageReference` no `.csproj`

No módulo **Application** (ou host) do seu ERP ABP:

```xml
<ItemGroup>
  <PackageReference Include="ERP.Fiscal.Abstractions" Version="0.1.*" />
  <PackageReference Include="ERP.Fiscal.PlugNotas" Version="0.1.*" />
</ItemGroup>
```

| Versão | Quando usar |
|--------|-------------|
| `0.1.*` | Última versão `0.1.x` de `main` (floating) |
| `0.1.42` | Versão específica (`PackagePatchNumber` no merge que gerou o pacote) |
| `1.0.0` | Release estável (tag `v1.0.0`) |

Restore:

```bash
dotnet restore
```

### 4. Registrar o módulo ABP

```csharp
using ERP.Fiscal.PlugNotas;
using Volo.Abp.Modularity;

[DependsOn(typeof(PlugNotasFiscalModule))]
public class MeuErpApplicationModule : AbpModule
{
}
```

Implemente `INfeAmbientePolicy` no consumidor (ex.: ler `OnlySandbox` dos Settings locais).

### 5. Configuração (`appsettings.json`)

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
| `SandboxApiKey` | Token sandbox; se vazio, usa chave pública documentada da PlugNotas |
| `ProductionApiKey` | Obrigatória para homologação/produção |
| `OnlySandbox` | Força runtime sandbox independente do cadastro do emissor |

Detalhes: [`docs/plugnotas/01-ambientes-autenticacao.md`](docs/plugnotas/01-ambientes-autenticacao.md).

### 6. GitHub Actions no consumidor

No workflow do ERP que restaura pacotes:

```yaml
permissions:
  contents: read
  packages: read

steps:
  - uses: actions/checkout@v4

  - uses: actions/setup-dotnet@v4
    with:
      dotnet-version: 10.0.x

  - name: Authenticate GitHub Packages
    run: >
      dotnet nuget add source "https://nuget.pkg.github.com/jpolvora/index.json"
      --name github
      --username "${{ github.repository_owner }}"
      --password "${{ secrets.GITHUB_TOKEN }}"
      --store-password-in-clear-text

  - run: dotnet restore
  - run: dotnet build --no-restore
```

Se o consumidor estiver em **outra org/conta**, o PAT precisa de acesso de leitura aos pacotes de `jpolvora` (pacote público no GitHub Packages ainda exige auth).

**Azure DevOps:** [`docs/consumers/azure-devops.md`](docs/consumers/azure-devops.md).

### 7. Alternativa: só nuget.org (sem feed GitHub)

Se o consumidor não usar GitHub Packages, basta o feed público:

```text
https://api.nuget.org/v3/index.json
```

```xml
<PackageReference Include="ERP.Fiscal.Abstractions" Version="0.1.3" />
<PackageReference Include="ERP.Fiscal.PlugNotas" Version="0.1.3" />
```

Ou versão flutuante: `0.1.*` para a última `0.1.x` publicada.

Páginas: [Abstractions](https://www.nuget.org/packages/ERP.Fiscal.Abstractions) · [PlugNotas](https://www.nuget.org/packages/ERP.Fiscal.PlugNotas)

---

## Exemplo mínimo (smoke test)

Projeto de referência que consome **somente NuGet** (sem project reference ao código-fonte):

[`samples/ERP.Fiscal.PackageSmokeTest`](samples/ERP.Fiscal.PackageSmokeTest)

```bash
# autenticar (ver seção acima)
dotnet test samples/ERP.Fiscal.PackageSmokeTest/ERP.Fiscal.PackageSmokeTest.csproj -c Release
```

Com pacotes locais (desenvolvimento desta lib):

```bash
dotnet pack ERP.Fiscal.slnx -c Release -o ./artifacts/packages -p:PackageVersion=0.1.0-local
dotnet nuget add source ./artifacts/packages --name local-erp-fiscal
dotnet test samples/ERP.Fiscal.PackageSmokeTest -c Release -p:ErpFiscalPackageVersion=0.1.0-local
```

Ou use [`samples/nuget.config.local.example`](samples/nuget.config.local.example) copiado como `nuget.config.local`.

---

## Integração com Consumidores & Agentes (IA)

Esta biblioteca define uma separação rígida de responsabilidades entre o seu ecossistema central (neutro) e as regras específicas dos ERPs consumidores. 

Para facilitar a integração e guiar o desenvolvimento por agentes de IA e desenvolvedores em projetos consumidores (ex.: `FiscalWR`), disponibilizamos uma **Skill de Agente portátil**:

- **Skill portátil:** [`consume-erp-fiscal`](.agents/skills/erp-fiscal-consumer/SKILL.md) (localizada em [`.agents/skills/erp-fiscal-consumer/SKILL.md`](.agents/skills/erp-fiscal-consumer/SKILL.md)).

### Como Instalar a Skill no ERP Consumidor

1. Crie a pasta `.agents/skills/consume-erp-fiscal/` na raiz do repositório do seu **ERP consumidor**.
2. Copie o arquivo [`SKILL.md`](.agents/skills/erp-fiscal-consumer/SKILL.md) deste repositório e cole nesta pasta recém-criada do ERP consumidor.
3. Isso ativará instruções automatizadas para o agente do consumidor, ensinando-o a configurar pacotes NuGet, registrar módulos ABP, e seguir o padrão de limites/fronteiras entre o ERP e a biblioteca `ERP.Fiscal`.

---

## Desenvolvimento (código-fonte)

### Estrutura

```text
ERP.Fiscal/
├── package.json                    # Husky + npm scripts security:*
├── .husky/pre-commit               # hook → scripts/pre-commit-security-check.sh
├── scripts/
│   ├── pre-commit-security-check.sh
│   └── audit-history-secrets.sh
├── src/
│   ├── ERP.Fiscal.Abstractions/    # interfaces + DTOs neutros
│   └── ERP.Fiscal.PlugNotas/       # HTTP, parsers, providers, módulo ABP
├── test/
│   └── ERP.Fiscal.PlugNotas.Tests/ # 91 testes unitários
├── samples/
│   └── ERP.Fiscal.PackageSmokeTest/
├── docs/
│   ├── README.md                   # índice docs (roteamento)
│   ├── security/README.md          # índice segurança
│   └── plugnotas/                  # documentação PlugNotas compilada
├── .github/workflows/validate-pr.yml
└── .github/workflows/deploy-main.yml
```

### Build e testes

```bash
dotnet build ERP.Fiscal.slnx
dotnet test ERP.Fiscal.slnx
```

### Segurança (segredos e privacidade)

**Índice de roteamento:** [`docs/security/README.md`](docs/security/README.md) — quando usar Husky, scripts, Gitleaks e auditoria de histórico.

**Procedimento para agentes:** [`.agents/skills/security-check/SKILL.md`](.agents/skills/security-check/SKILL.md) — varredura obrigatória em uncommitted, versionados e temporários (mesmo ignorados).

Setup único na raiz do clone:

```bash
npm install
```

| Comando | Função |
|---------|--------|
| `npm run security:pre-commit` | Simula o hook — valida apenas o **stage** |
| `npm run security:audit-history` | Auditoria read-only de HEAD + histórico Git + Gitleaks |

Cada `git commit` executa automaticamente `scripts/pre-commit-security-check.sh`. O hook **complementa** a skill — não substitui varredura de arquivos tracked e caches locais.

### Gerar pacotes localmente

```bash
dotnet pack ERP.Fiscal.slnx -c Release -o ./artifacts/packages
```

### CI/CD (GitHub Actions)

1. [**Validate PR**](.github/workflows/validate-pr.yml) (`validate-pr.yml`) — executado em Pull Requests para a `main`: faz restore, build, teste unitário, pack local e executa o **Smoke Test** isolado.
2. [**Deploy Main**](.github/workflows/deploy-main.yml) (`deploy-main.yml`) — executado em merges/pushes na `main` e tags `v*`:
   - Em push na `main`: resolve versão, build, teste, pack, publish no GitHub Packages e faz bump de versão automático.
   - Em tag `v*`: pack e publish da versão estável no GitHub Packages e nuget.org (se configurado).

**Code review agêntico (PR → `main` only):** [`.github/workflows/cursor-code-review.yml`](.github/workflows/cursor-code-review.yml)

---

## Documentação

| Recurso | Link |
|---------|------|
| Índice docs (roteamento agentes) | [`docs/README.md`](docs/README.md) |
| Índice PlugNotas | [`docs/plugnotas/README.md`](docs/plugnotas/README.md) |
| Segurança (Husky, auditoria Git) | [`docs/security/README.md`](docs/security/README.md) |
| Ambientes e API key | [`docs/plugnotas/01-ambientes-autenticacao.md`](docs/plugnotas/01-ambientes-autenticacao.md) |
| Fluxo emissão NF-e | [`docs/plugnotas/04-nfe-fluxo-emissao.md`](docs/plugnotas/04-nfe-fluxo-emissao.md) |
| Mapeamento lib ↔ API | [`docs/plugnotas/07-mapeamento-erp-fiscal.md`](docs/plugnotas/07-mapeamento-erp-fiscal.md) |
| Swagger PlugNotas (canônico) | [docs.plugnotas.com.br](https://docs.plugnotas.com.br) |
| Instruções para agentes IA | [`AGENTS.md`](AGENTS.md) |
| Padrão de integração nos consumidores | [`docs/consumers/padrao-integracao.md`](docs/consumers/padrao-integracao.md) |
| Skill: Consumo em ERPs | [`.agents/skills/erp-fiscal-consumer/SKILL.md`](.agents/skills/erp-fiscal-consumer/SKILL.md) |
| Skill: Checagem de segurança | [`.agents/skills/security-check/SKILL.md`](.agents/skills/security-check/SKILL.md) |

---

## Dependências da lib

| Pacote | Versão |
|--------|--------|
| [Volo.Abp.Core](https://www.nuget.org/packages/Volo.Abp.Core) | 10.3.0 |
| [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) | 10.0.0 |

`ERP.Fiscal.Abstractions` não possui dependências NuGet externas.

---

## Licença

[MIT](LICENSE)
