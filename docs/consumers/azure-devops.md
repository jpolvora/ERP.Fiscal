# Consumir ERP.Fiscal — Azure DevOps

Guia para pipelines **Azure DevOps** que restauram `ERP.Fiscal.Abstractions` e `ERP.Fiscal.PlugNotas`.

> **Releases públicas (recomendado):** use [nuget.org](https://www.nuget.org) — ex. [Abstractions 0.1.3](https://www.nuget.org/packages/ERP.Fiscal.Abstractions/0.1.3), [PlugNotas 0.1.3](https://www.nuget.org/packages/ERP.Fiscal.PlugNotas/0.1.3). **Sem auth** no `dotnet restore`. Basta `PackageReference` + feed padrão `nuget.org`.
>
> As seções abaixo aplicam-se quando o consumidor precisa do feed **GitHub Packages** (previews ou antes da validação no nuget.org).

## GitHub Packages — feed (por owner, não por repositório)

```text
https://nuget.pkg.github.com/jpolvora/index.json
```

| Campo | Valor |
|-------|-------|
| Owner GitHub | `jpolvora` |
| Repositório dos fontes | `jpolvora/ERP.Fiscal` |
| Package IDs | `ERP.Fiscal.Abstractions`, `ERP.Fiscal.PlugNotas` |

> Pacotes no GitHub Packages **exigem autenticação** no `dotnet restore`, mesmo com repositório público.

## Variáveis do pipeline

Configure no pipeline ou em Variable Group (marque o PAT como **secreto**):

| Variável | Tipo | Descrição |
|----------|------|-----------|
| `GITHUB_PACKAGES_USERNAME` | texto | Usuário GitHub com acesso de leitura aos pacotes (ex.: `jpolvora`) |
| `GITHUB_PACKAGES_PAT` | secreto | PAT classic com escopo **`read:packages`** (+ `repo` se o pacote for privado) |

Se o mesmo PAT já usado para GHCR tiver `read:packages`, pode reutilizar `GHCR_USERNAME` / `GHCR_TOKEN` no consumidor.

## `nuget.config` no repositório consumidor

Na raiz da solution (ex.: FiscalWR):

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

Referência: [`samples/nuget.config`](../../samples/nuget.config).

**Não** commitar PAT no `nuget.config`.

## Passo de autenticação (YAML)

Antes de `dotnet restore`, `dotnet build` ou `docker build` da API:

```yaml
- bash: |
    set -euo pipefail
    if [ -z "${GITHUB_PACKAGES_PAT:-}" ] || [ -z "${GITHUB_PACKAGES_USERNAME:-}" ]; then
      echo "##vso[task.logissue type=error]Defina GITHUB_PACKAGES_USERNAME e GITHUB_PACKAGES_PAT (secreto)."
      exit 1
    fi
    dotnet nuget add source "https://nuget.pkg.github.com/jpolvora/index.json" \
      --name github \
      --username "${GITHUB_PACKAGES_USERNAME}" \
      --password "${GITHUB_PACKAGES_PAT}" \
      --store-password-in-clear-text
  displayName: Authenticate GitHub Packages (NuGet)
  env:
    GITHUB_PACKAGES_USERNAME: $(GITHUB_PACKAGES_USERNAME)
    GITHUB_PACKAGES_PAT: $(GITHUB_PACKAGES_PAT)

- script: dotnet restore fiscalwr.sln
  displayName: Restore
```

## Docker build (API .NET)

Passe credenciais como build-args e autentique **dentro** do estágio `build` do Dockerfile (antes do `dotnet restore`):

```yaml
- bash: |
    set -euo pipefail
    docker build \
      --build-arg GITHUB_PACKAGES_USERNAME="$(GITHUB_PACKAGES_USERNAME)" \
      --build-arg GITHUB_PACKAGES_TOKEN="$(GITHUB_PACKAGES_PAT)" \
      -t "$(ghcrRegistry)/$(ghcrImageOwner)/$(apiImageName):$(uniqueTag)" \
      -f src/fiscalwr.HttpApi.Host/Dockerfile \
      .
  displayName: Build imagem API (.NET)
  env:
    GITHUB_PACKAGES_USERNAME: $(GITHUB_PACKAGES_USERNAME)
    GITHUB_PACKAGES_PAT: $(GITHUB_PACKAGES_PAT)
```

## Versão dos pacotes

| Cenário | `ErpFiscalPackageVersion` |
|---------|---------------------------|
| CI reprodutível | Pin fixo, ex.: `0.1.42` |
| Dev local | `0.1.*` (última versão de `main`) |
| Release estável | `1.0.0` (tag `v1.0.0` no repo ERP.Fiscal) |

Sample: [`samples/erp-fiscal-version.props`](../../samples/erp-fiscal-version.props).

## Referências

- [README — Consumir via GitHub Packages](../../README.md#consumir-via-github-packages)
- [GitHub: Working with the NuGet registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)
