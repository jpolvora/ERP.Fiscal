# Wrapper em PowerShell para o script de release NuGet.
# Permite a execução nativa no Windows (PowerShell) resolvendo dinamicamente o Git Bash correto.

$gitPath = Get-Command git -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
$bashPath = $null

if ($gitPath) {
    # Tenta achar o bash.exe no mesmo diretório do Git (tipicamente C:\Program Files\Git\bin\bash.exe ou similar)
    $gitDir = Split-Path (Split-Path $gitPath -Parent) -Parent
    $possibleBash = Join-Path $gitDir "bin\bash.exe"
    if (Test-Path $possibleBash) {
        $bashPath = $possibleBash
    } else {
        $possibleBash = Join-Path $gitDir "usr\bin\bash.exe"
        if (Test-Path $possibleBash) {
            $bashPath = $possibleBash
        }
    }
}

if (-not $bashPath) {
    # Fallback para o bash no PATH (pode ser o WSL se estiver ativo)
    $bashPath = "bash"
}

$scriptPath = Join-Path $PSScriptRoot "..\.agents\skills\release-nuget-package\scripts\release.sh"

if (-not (Test-Path $scriptPath)) {
    Write-Error "Script de release não encontrado em: $scriptPath"
    exit 1
}

# Executa o script passando todos os argumentos
& $bashPath $scriptPath $args
exit $LASTEXITCODE
