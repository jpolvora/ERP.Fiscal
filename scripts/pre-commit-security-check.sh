#!/usr/bin/env bash
# Pre-commit security scan — staged files only.
# Alinhado a .agents/skills/security-check/SKILL.md
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"

RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

fail() {
  echo -e "${RED}pre-commit security-check: BLOQUEADO — $1${NC}" >&2
  exit 1
}

warn() {
  echo -e "${YELLOW}pre-commit security-check: aviso — $1${NC}" >&2
}

# --- 1) Arquivos proibidos por nome/extensão (staged) ---
FORBIDDEN_PATH_REGEX='\.(pfx|p12|pem|key|der)$|(^|/)(secrets\.json|\.env|\.env\.local|appsettings\.local\.json|appsettings\.[^/]+\.local\.json|nuget\.config\.local|\.tmp-postman-cache\.json)$|/secrets/'

while IFS= read -r -d '' path; do
  if [[ "$path" =~ $FORBIDDEN_PATH_REGEX ]]; then
    if [[ "$path" =~ \.env\.(example|template)$ ]]; then
      continue
    fi
    fail "arquivo sensível ou proibido no stage: $path"
  fi
done < <(git diff --cached --name-only -z --diff-filter=ACMR)

# --- 2) Conteúdo adicionado no stage ---
STAGED_DIFF="$(git diff --cached -U0 --no-color || true)"
if [[ -z "$STAGED_DIFF" ]]; then
  echo "pre-commit security-check: nenhuma alteração staged — OK"
  exit 0
fi

# Linhas adicionadas (ignora metadados do diff)
ADDED_LINES="$(printf '%s\n' "$STAGED_DIFF" | grep -E -- '^\+[^+]' || true)"

if [[ -z "$ADDED_LINES" ]]; then
  echo "pre-commit security-check: OK (sem linhas adicionadas)"
  exit 0
fi

# --- 3) Allowlist — remover linhas seguras antes da varredura ---
FILTERED="$ADDED_LINES"

# CI / placeholders / docs
FILTERED="$(printf '%s\n' "$FILTERED" | grep -Ev -- \
  '\$\{\{[[:space:]]*secrets\.|SEU_PAT|SEU_PERSONAL|sua-chave|chave-prod|placeholder|<senha|<password|Password=<|\-\-store-password-in-clear-text' \
  || true)"

# Chaves vazias em JSON/config
FILTERED="$(printf '%s\n' "$FILTERED" | grep -Ev -- \
  '"(SandboxApiKey|ProductionApiKey|ApiKey)"[[:space:]]*:[[:space:]]*""' \
  || true)"

# Sandbox público documentado pela PlugNotas (constante no código)
FILTERED="$(printf '%s\n' "$FILTERED" | grep -Ev -- \
  '2da392a6-79d2-4304-a8b7-959572c7e44d|PublicSandboxApiKey' \
  || true)"

# Apenas menção a nomes de propriedade (sem valor atribuído)
FILTERED="$(printf '%s\n' "$FILTERED" | grep -Ev -- \
  '`(SandboxApiKey|ProductionApiKey|ApiKey)`|PlugNotasOptions\.(Sandbox|Production)ApiKey|nameof\((Sandbox|Production)ApiKey\)' \
  || true)"

# Definições de padrão dentro do próprio script de checagem
FILTERED="$(printf '%s\n' "$FILTERED" | grep -Ev -- 'check_pattern |FORBIDDEN_PATH_REGEX' || true)"

if [[ -z "$FILTERED" ]]; then
  echo "pre-commit security-check: OK"
  exit 0
fi

# --- 4) Padrões de segredo (linhas adicionadas) ---
FOUND=0
check_pattern() {
  local name="$1"
  local regex="$2"
  local hits
  hits="$(printf '%s\n' "$FILTERED" | grep -Ei -- "$regex" || true)"
  if [[ -n "$hits" ]]; then
    FOUND=1
    echo -e "${RED}▸ $name${NC}" >&2
    printf '%s\n' "$hits" | head -5 | sed 's/^/    /' >&2
    local count
    count="$(printf '%s\n' "$hits" | wc -l | tr -d ' ')"
    if [[ "$count" -gt 5 ]]; then
      echo "    ... (+$((count - 5)) ocorrências)" >&2
    fi
  fi
}

check_pattern "chave privada PEM/SSH" '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----'
check_pattern "AWS Access Key" 'AKIA[0-9A-Z]{16}'
check_pattern "GitHub PAT" 'gh[pousr]_[A-Za-z0-9_]{20,}|github_pat_[A-Za-z0-9_]{20,}'
check_pattern "Bearer token" 'Bearer [A-Za-z0-9._\-]{24,}'
check_pattern "ProductionApiKey com valor" '"ProductionApiKey"[[:space:]]*:[[:space:]]*"[^"]{8,}"'
check_pattern "SandboxApiKey com valor" '"SandboxApiKey"[[:space:]]*:[[:space:]]*"[^"]{8,}"'
check_pattern "ApiKey atribuída (C#/JSON)" '(SandboxApiKey|ProductionApiKey|ApiKey)[[:space:]]*=[[:space:]]*"[^"]{12,}"'
check_pattern "x-api-key com valor" '["'\'']?x-api-key["'\'']?[[:space:]]*:[[:space:]]*["'\''][^"'\'']{12,}'
check_pattern "senha em connection string" '(Password|Pwd)=[^;"'\''[:space:]]{4,}'
check_pattern "token/secret de CI" '(CURSOR_API_KEY|NUGET_API_KEY|GITHUB_TOKEN)[[:space:]]*=[[:space:]]*[^[:space:]${}]+'
check_pattern "Authorization header com credencial" 'Authorization:[[:space:]]+(Bearer[[:space:]]+)?[A-Za-z0-9._\-]{20,}'

if [[ "$FOUND" -eq 1 ]]; then
  echo "" >&2
  echo -e "${RED}Commit abortado. Remova ou mova os segredos para config local ignorada (.gitignore).${NC}" >&2
  echo "Referência: .agents/skills/security-check/SKILL.md" >&2
  exit 1
fi

echo "pre-commit security-check: OK"
