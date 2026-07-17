#!/usr/bin/env bash
# Auditoria read-only de segredos e privacidade — histórico Git + working tree.
# Alinhado a .agents/skills/security-check/SKILL.md
#
# Uso:
#   bash scripts/audit-history-secrets.sh [opções]
#
# Opções:
#   --output DIR           Pasta de relatórios (default: .security-audit)
#   --skip-gitleaks        Não executar Gitleaks
#   --skip-filter-analyze  Não executar git-filter-repo --analyze
#   --skip-filter-dry-run  Não executar git-filter-repo --dry-run
#   --install-gitleaks     Baixar Gitleaks para .tools/ se ausente (Windows x64)
#   --help
#
# Exit: 0 = sem achados relevantes | 1 = achados | 2 = erro de ferramenta
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$ROOT"

OUTPUT_DIR=".security-audit"
SKIP_GITLEAKS=0
SKIP_FILTER_ANALYZE=0
SKIP_FILTER_DRY_RUN=0
INSTALL_GITLEAKS=0
FINDINGS=0

RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m'

usage() {
  sed -n '2,16p' "$0" | sed 's/^# \{0,1\}//'
  exit 0
}

log() { log_both "${CYAN}▸ $*${NC}"; }
ok()  { log_both "${GREEN}  ✓ $*${NC}"; }
warn() { log_both "${YELLOW}  ⚠ $*${NC}"; FINDINGS=1; }
hit() { log_both "${RED}  ✗ $*${NC}"; FINDINGS=1; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --output) OUTPUT_DIR="$2"; shift 2 ;;
    --skip-gitleaks) SKIP_GITLEAKS=1; shift ;;
    --skip-filter-analyze) SKIP_FILTER_ANALYZE=1; shift ;;
    --skip-filter-dry-run) SKIP_FILTER_DRY_RUN=1; shift ;;
    --install-gitleaks) INSTALL_GITLEAKS=1; shift ;;
    --help|-h) usage ;;
    *) echo "Opção desconhecida: $1" >&2; usage ;;
  esac
done

mkdir -p "$OUTPUT_DIR"
REPORT="$OUTPUT_DIR/report-$(date +%Y%m%d-%H%M%S).txt"

log_both() {
  echo -e "$1" | tee -a "$REPORT"
}

log_both "=== ERP.Fiscal — auditoria de segredos e privacidade ==="
log_both "Repo: $ROOT"
log_both "Relatório: $REPORT"
log_both ""

# --- Fase C: arquivos versionados (HEAD) ---
log "Fase 1/7 — arquivos versionados (HEAD)"
TRACKED_REPORT="$OUTPUT_DIR/tracked-hits.txt"
: > "$TRACKED_REPORT"

git ls-files -z | xargs -0 rg -n -i --no-heading \
  -e 'gh[pousr]_[A-Za-z0-9_]{20,}' \
  -e 'github_pat_[A-Za-z0-9_]{20,}' \
  -e 'AKIA[0-9A-Z]{16}' \
  -e '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----' \
  -e '(CURSOR_API_KEY|NUGET_API_KEY)=[^[:space:]${}]+' \
  -e '"ProductionApiKey"[[:space:]]*:[[:space:]]*"[^"]{8,}"' \
  -e '"SandboxApiKey"[[:space:]]*:[[:space:]]*"[^"]{8,}"' \
  2>/dev/null | grep -Ev -- '2da392a6|sua-chave|chave-prod|chave-producao|SEU_PAT|\$\{\{ secrets\.' >> "$TRACKED_REPORT" || true

if [[ -s "$TRACKED_REPORT" ]]; then
  hit "Padrões suspeitos em arquivos versionados:"
  head -20 "$TRACKED_REPORT" | sed 's/^/    /'
else
  ok "Nenhum padrão crítico em HEAD"
fi

# --- Fase B: histórico (git log -p) ---
log "Fase 2/7 — histórico Git (todas as branches)"
HISTORY_REPORT="$OUTPUT_DIR/history-hits.txt"
: > "$HISTORY_REPORT"

git log --all -p --no-color 2>/dev/null | rg -n -i \
  -e 'gh[pousr]_[A-Za-z0-9_]{20,}' \
  -e 'github_pat_[A-Za-z0-9_]{20,}' \
  -e 'AKIA[0-9A-Z]{16}' \
  -e '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----' \
  -e '(CURSOR_API_KEY|NUGET_API_KEY)=[^[:space:]${}]+' \
  2>/dev/null | grep -Ev -- '2da392a6|sua-chave|chave-prod|SEU_PAT|security-check|pre-commit-security|audit-history' >> "$HISTORY_REPORT" || true

if [[ -s "$HISTORY_REPORT" ]]; then
  hit "Padrões críticos no histórico:"
  head -20 "$HISTORY_REPORT" | sed 's/^/    /'
else
  ok "Nenhum PAT/AWS/PEM crítico no histórico"
fi

# --- Privacidade: consumidores em commits ---
log "Fase 3/7 — privacidade (mensagens de commit / nomes de consumidor)"
PRIVACY_REPORT="$OUTPUT_DIR/privacy-hits.txt"
: > "$PRIVACY_REPORT"

git log --all --format='%h %s' 2>/dev/null | rg -i \
  'FiscalWR|MarchanteERP|FlorestalERP|implementation_plan|walkthrough\.md|task\.md' \
  >> "$PRIVACY_REPORT" || true

if [[ -s "$PRIVACY_REPORT" ]]; then
  warn "Menções a consumidores/planos em mensagens de commit (revisar se devem permanecer públicas):"
  cat "$PRIVACY_REPORT" | sed 's/^/    /'
else
  ok "Nenhuma menção sensível em mensagens de commit"
fi

# --- Fase D: temporários ---
log "Fase 4/7 — arquivos temporários (mesmo ignorados)"
TEMP_REPORT="$OUTPUT_DIR/temp-hits.txt"
: > "$TEMP_REPORT"

for candidate in .tmp-postman-cache.json .tmp-test-leak.json; do
  if [[ -f "$candidate" ]]; then
    echo "PRESENT: $candidate (gitignored — não commitar)" >> "$TEMP_REPORT"
    # Evita dump do JSON Postman público; só alerta presença local
    if [[ "$candidate" == ".tmp-postman-cache.json" ]]; then
      if rg -q 'gh[pousr]_[A-Za-z0-9_]{20,}|AKIA[0-9A-Z]{16}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----' "$candidate" 2>/dev/null; then
        echo "CRITICAL_PATTERN_IN: $candidate" >> "$TEMP_REPORT"
      fi
    else
      rg -n -i 'gh[pousr]_[A-Za-z0-9_]{20,}|AKIA[0-9A-Z]{16}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----' "$candidate" >> "$TEMP_REPORT" 2>/dev/null || true
    fi
  fi
done

if [[ -s "$TEMP_REPORT" ]]; then
  if grep -q '^CRITICAL_PATTERN_IN:' "$TEMP_REPORT" 2>/dev/null; then
    hit "Padrão crítico em temporário local:"
    grep '^CRITICAL_PATTERN_IN:' "$TEMP_REPORT" | sed 's/^/    /'
  else
    warn "Arquivos temporários locais presentes (não versionados — OK se não forem commitados):"
    grep '^PRESENT:' "$TEMP_REPORT" | sed 's/^/    /' || true
  fi
  if [[ -d .cursor/plans ]] && rg -l -i 'password|ApiKey|secret|token' .cursor/plans/ >/dev/null 2>&1; then
    hit "Planos locais com possível segredo em .cursor/plans/"
    rg -l -i 'password|ApiKey|secret|token' .cursor/plans/ | head -10 | sed 's/^/    /'
  fi
else
  ok "Temporários/planos locais limpos ou ausentes"
fi

# --- Gitleaks ---
resolve_gitleaks() {
  if command -v gitleaks >/dev/null 2>&1; then
    command -v gitleaks
    return 0
  fi
  local tool="$ROOT/.tools/gitleaks/gitleaks.exe"
  if [[ -x "$tool" ]] || [[ -f "$tool" ]]; then
    echo "$tool"
    return 0
  fi
  tool="$ROOT/.tools/gitleaks/gitleaks"
  if [[ -x "$tool" ]]; then
    echo "$tool"
    return 0
  fi
  return 1
}

install_gitleaks_local() {
  local version="8.30.1"
  local dir="$ROOT/.tools/gitleaks"
  mkdir -p "$dir"
  local zip="/tmp/gitleaks_${version}_windows_x64.zip"
  log "Baixando Gitleaks v${version}..."
  curl -fsSL "https://github.com/gitleaks/gitleaks/releases/download/v${version}/gitleaks_${version}_windows_x64.zip" -o "$zip"
  unzip -o -q "$zip" -d "$dir"
  rm -f "$zip"
  chmod +x "$dir/gitleaks.exe" 2>/dev/null || true
}

log "Fase 5/7 — Gitleaks (histórico completo)"
GITLEAKS_REPORT="$OUTPUT_DIR/gitleaks.json"
if [[ "$SKIP_GITLEAKS" -eq 1 ]]; then
  warn "Gitleaks ignorado (--skip-gitleaks)"
else
  if ! GITLEAKS_BIN="$(resolve_gitleaks)"; then
    if [[ "$INSTALL_GITLEAKS" -eq 1 ]]; then
      install_gitleaks_local
      GITLEAKS_BIN="$(resolve_gitleaks)" || true
    fi
  fi
  if [[ -n "${GITLEAKS_BIN:-}" ]]; then
    CONFIG_ARG=()
    [[ -f "$ROOT/.gitleaks.toml" ]] && CONFIG_ARG=(--config "$ROOT/.gitleaks.toml")
    if "$GITLEAKS_BIN" detect \
      --source "$ROOT" \
      --log-opts="--all" \
      --report-format json \
      --report-path "$GITLEAKS_REPORT" \
      --redact \
      "${CONFIG_ARG[@]}" \
      > "$OUTPUT_DIR/gitleaks.log" 2>&1; then
      ok "Gitleaks: nenhum vazamento detectado"
    else
      leaks="$(rg -c '"Description"' "$GITLEAKS_REPORT" 2>/dev/null || echo 0)"
      hit "Gitleaks: ${leaks} achado(s) — ver $GITLEAKS_REPORT"
    fi
  else
    warn "Gitleaks não instalado (use --install-gitleaks ou instale: https://github.com/gitleaks/gitleaks)"
  fi
fi

# --- git-filter-repo --analyze ---
log "Fase 6/7 — git-filter-repo --analyze"
if [[ "$SKIP_FILTER_ANALYZE" -eq 1 ]]; then
  warn "filter-repo --analyze ignorado"
else
  if python3 -m git_filter_repo --analyze 2>&1 | tee "$OUTPUT_DIR/filter-repo-analyze.log"; then
    if [[ -d .git/filter-repo/analysis ]]; then
      cp -r .git/filter-repo/analysis "$OUTPUT_DIR/filter-repo-analysis"
      DELETED="$OUTPUT_DIR/filter-repo-analysis/path-deleted-sizes.txt"
      if [[ -f "$DELETED" ]] && [[ "$(wc -l < "$DELETED")" -gt 3 ]]; then
        warn "Paths deletados ainda presentes no histórico (ver path-deleted-sizes.txt)"
        tail -n +4 "$DELETED" | head -10 | sed 's/^/    /'
      else
        ok "Nenhum path deletado relevante no histórico"
      fi
    fi
  else
    echo "Erro: python3 -m git_filter_repo não disponível (pip install git-filter-repo)" >&2
    exit 2
  fi
fi

# --- git-filter-repo --dry-run ---
log "Fase 7/7 — git-filter-repo --dry-run (preview de remediação)"
if [[ "$SKIP_FILTER_DRY_RUN" -eq 1 ]]; then
  warn "filter-repo --dry-run ignorado"
else
  EXPRESSIONS="$ROOT/scripts/audit-filter-expressions.txt"
  DRY_DIR="$(mktemp -d 2>/dev/null || mktemp -d -t erp-fiscal-dryrun)"
  trap 'rm -rf "$DRY_DIR"' EXIT

  if git clone --quiet --no-local "$ROOT" "$DRY_DIR/repo" 2>/dev/null; then
    if (cd "$DRY_DIR/repo" && python3 -m git_filter_repo \
      --dry-run \
      --replace-text "$EXPRESSIONS" \
      2>&1 | tee "$OUTPUT_DIR/filter-repo-dry-run.log"); then
      ORIG="$DRY_DIR/repo/.git/filter-repo/fast-export.original"
      FILT="$DRY_DIR/repo/.git/filter-repo/fast-export.filtered"
      DIFF_OUT="$OUTPUT_DIR/filter-repo-dry-run.diff"
      if [[ -f "$ORIG" && -f "$FILT" ]]; then
        diff -u "$ORIG" "$FILT" > "$DIFF_OUT" 2>/dev/null || true
        CONTENT_CHANGES="$(diff "$ORIG" "$FILT" | rg -v '^(<|>)original-oid ' | rg '^[<>]' || true)"
        if [[ -n "$CONTENT_CHANGES" ]]; then
          hit "Dry-run alteraria conteúdo no histórico — ver $DIFF_OUT"
          echo "$CONTENT_CHANGES" | head -15 | sed 's/^/    /'
        else
          ok "Dry-run: expressões de remediação não alterariam conteúdo (só metadados)"
        fi
      fi
    fi
  else
    warn "Não foi possível clonar para dry-run"
  fi
fi

echo ""
log_both "=== Resumo ==="
if [[ "$FINDINGS" -eq 0 ]]; then
  log_both "${GREEN}Nenhum achado crítico. Relatórios em: $OUTPUT_DIR/${NC}"
  exit 0
else
  log_both "${YELLOW}Achados para revisão. Relatórios em: $OUTPUT_DIR/${NC}"
  log_both "Remediação (se necessário): git-filter-repo --sensitive-data-removal em clone fresco."
  exit 1
fi
