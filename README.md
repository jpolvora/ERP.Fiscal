# ERP.Fiscal

Biblioteca .NET de integração fiscal (NF-e via [PlugNotas](https://docs.plugnotas.com.br)), pensada para consumo por aplicações [ABP](https://abp.io). Centraliza HTTP, parsers, retry, classificação de erros e contratos neutros — **sem acoplar domínio de ERP**.

Pacotes publicados automaticamente no **GitHub Packages** a cada push em `main`. Releases com tag `v*` também podem ser publicadas no [nuget.org](https://www.nuget.org).

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
| [`ERP.Fiscal.Abstractions`](https://github.com/jpolvora/ERP.Fiscal/pkgs/nuget/ERP.Fiscal.Abstractions) | Interfaces (`INfe*Provider`) e DTOs neutros | Nenhuma externa |
| [`ERP.Fiscal.PlugNotas`](https://github.com/jpolvora/ERP.Fiscal/pkgs/nuget/ERP.Fiscal.PlugNotas) | Implementação PlugNotas + módulo ABP | `ERP.Fiscal.Abstractions`, Volo.Abp.Core, Microsoft.Extensions.Http |

**Target:** .NET 10 (`net10.0`)

### Versionamento

| Gatilho | Versão | Onde publica |
|---------|--------|--------------|
| Push em `main` | `0.1.0-preview.{run}` | GitHub Packages + artefato CI |
| Tag `v1.0.0` | `1.0.0` | GitHub Packages + nuget.org (se `NUGET_API_KEY` configurado) |

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
  <PackageReference Include="ERP.Fiscal.Abstractions" Version="0.1.0-*" />
  <PackageReference Include="ERP.Fiscal.PlugNotas" Version="0.1.0-*" />
</ItemGroup>
```

| Versão | Quando usar |
|--------|-------------|
| `0.1.0-*` | Último preview de `main` (floating) |
| `0.1.0-preview.42` | Preview específico (número do workflow run) |
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

### 7. Alternativa: nuget.org (releases com tag)

Após tag `v1.0.0`, se `NUGET_API_KEY` estiver configurado neste repositório, os pacotes também aparecem em:

```text
https://api.nuget.org/v3/index.json
```

Nesse caso, basta `PackageReference` sem feed GitHub — apenas nuget.org.

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

## Desenvolvimento (código-fonte)

### Estrutura

```text
ERP.Fiscal/
├── src/
│   ├── ERP.Fiscal.Abstractions/    # interfaces + DTOs neutros
│   └── ERP.Fiscal.PlugNotas/       # HTTP, parsers, providers, módulo ABP
├── test/
│   └── ERP.Fiscal.PlugNotas.Tests/ # 91 testes unitários
├── samples/
│   └── ERP.Fiscal.PackageSmokeTest/
├── docs/plugnotas/                 # documentação PlugNotas compilada
└── .github/workflows/ci.yml
```

### Build e testes

```bash
dotnet build ERP.Fiscal.slnx
dotnet test ERP.Fiscal.slnx
```

### Gerar pacotes localmente

```bash
dotnet pack ERP.Fiscal.slnx -c Release -o ./artifacts/packages
```

### CI (este repositório)

Workflow [`.github/workflows/ci.yml`](.github/workflows/ci.yml):

1. **build** — restore, build, test, pack
2. **Publish** — GitHub Packages (push); nuget.org (tag `v*`)
3. **package-smoke-test** — restaura pacotes do GitHub e executa o sample

Artefatos `.nupkg` / `.snupkg` disponíveis em **Actions → Artifacts** de cada run.

---

## Documentação

| Recurso | Link |
|---------|------|
| Índice PlugNotas (agentes/devs) | [`docs/README.md`](docs/README.md) |
| Ambientes e API key | [`docs/plugnotas/01-ambientes-autenticacao.md`](docs/plugnotas/01-ambientes-autenticacao.md) |
| Fluxo emissão NF-e | [`docs/plugnotas/04-nfe-fluxo-emissao.md`](docs/plugnotas/04-nfe-fluxo-emissao.md) |
| Mapeamento lib ↔ API | [`docs/plugnotas/07-mapeamento-erp-fiscal.md`](docs/plugnotas/07-mapeamento-erp-fiscal.md) |
| Swagger PlugNotas (canônico) | [docs.plugnotas.com.br](https://docs.plugnotas.com.br) |
| Instruções para agentes IA | [`AGENTS.md`](AGENTS.md) |

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
