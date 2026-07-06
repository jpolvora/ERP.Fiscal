#!/usr/bin/env bash
# Publica pacotes ERP.Fiscal no GitHub Packages e NuGet.org via workflow Deploy Main.
# Uso:
#   release.sh status              → versão pendente, publicada e estado do CI
#   release.sh publish [opções]    → fluxo completo (commit trigger, CI, tag, release)
#   release.sh verify [versão]     → confere feeds (default: versão em nuget.props - 1 ou informada)
#
# Opções (publish):
#   --dry-run          Mostra o plano sem alterar remoto
#   --merge-develop    Faz merge de origin/develop em main antes do release
#   --force            Publica mesmo se a versão já existir no GitHub Packages
#   --message "texto"  Mensagem do commit trigger (default: chore: trigger release vX.Y.Z)
#   --no-tag           Não cria/atualiza tag nem GitHub Release
#   --no-watch         Não aguarda o workflow (apenas dispara)

set -eu
if (set -o | grep -q pipefail) 2>/dev/null; then
    set -o pipefail
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../../" && pwd)"
cd "$REPO_ROOT"

WORKFLOW_FILE="deploy-main.yml"
WORKFLOW_NAME="Deploy Main"
GITHUB_OWNER="jpolvora"
PACKAGES=("ERP.Fiscal.Abstractions" "ERP.Fiscal.PlugNotas")
MAIN_BRANCH="main"
DEVELOP_BRANCH="develop"

DRY_RUN=false
MERGE_DEVELOP=false
FORCE=false
NO_TAG=false
NO_WATCH=false
CUSTOM_MESSAGE=""

log() { printf '%s\n' "$*"; }
err() { printf 'ERRO: %s\n' "$*" >&2; }

usage() {
  sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'
  exit "${1:-0}"
}

require_cmd() {
  local cmd="$1"
  command -v "$cmd" >/dev/null 2>&1 || {
    err "Comando obrigatório não encontrado: $cmd"
    exit 1
  }
}

resolve_version() {
  bash "$REPO_ROOT/scripts/ci-package-version.sh" resolve
}

read_prefix() {
  sed -n 's|^[[:space:]]*<VersionPrefix>\(.*\)</VersionPrefix>|\1|p' nuget.props | head -n1
}

gh_authenticated() {
  gh auth status >/dev/null 2>&1
}

git_clean() {
  [[ -z "$(git status --porcelain)" ]]
}

fetch_all() {
  git fetch origin --tags --prune
}

on_main_synced() {
  git rev-parse --abbrev-ref HEAD | grep -qx "$MAIN_BRANCH"
}

main_behind_remote() {
  local local_sha remote_sha
  local_sha="$(git rev-parse "$MAIN_BRANCH")"
  remote_sha="$(git rev-parse "origin/$MAIN_BRANCH")"
  [[ "$local_sha" != "$remote_sha" ]]
}

package_exists_github() {
  local package="$1" version="$2"
  gh api "users/${GITHUB_OWNER}/packages/nuget/${package}/versions" \
    --jq ".[] | select(.name == \"${version}\") | .name" 2>/dev/null | grep -qx "$version"
}

package_exists_nuget_org() {
  local package="$1" version="$2"
  local id
  id="$(printf '%s' "$package" | tr '[:upper:]' '[:lower:]')"
  curl -fsS "https://api.nuget.org/v3-flatcontainer/${id}/${version}/${id}.nuspec" >/dev/null 2>&1
}

latest_published_github() {
  local package="$1"
  gh api "users/${GITHUB_OWNER}/packages/nuget/${package}/versions" \
    --jq 'map(.name) | sort | reverse | .[0]' 2>/dev/null || echo "?"
}

wait_for_main_deploy() {
  local timeout="${1:-600}" elapsed=0 run_id=""
  log "Aguardando workflow ${WORKFLOW_NAME} na ${MAIN_BRANCH}..."
  while (( elapsed < timeout )); do
    run_id="$(gh run list --workflow "$WORKFLOW_FILE" --branch "$MAIN_BRANCH" --limit 1 \
      --json databaseId,status --jq '.[0] | select(.status != "completed") | .databaseId' 2>/dev/null || true)"
    if [[ -n "$run_id" && "$run_id" != "null" ]]; then
      gh run watch "$run_id" --exit-status
      return 0
    fi
    local latest_status latest_conclusion
    latest_status="$(gh run list --workflow "$WORKFLOW_FILE" --branch "$MAIN_BRANCH" --limit 1 --json status --jq '.[0].status')"
    latest_conclusion="$(gh run list --workflow "$WORKFLOW_FILE" --branch "$MAIN_BRANCH" --limit 1 --json conclusion --jq '.[0].conclusion')"
    if [[ "$latest_status" == "completed" && "$latest_conclusion" == "success" ]]; then
      return 0
    fi
    sleep 5
    elapsed=$((elapsed + 5))
  done
  err "Timeout aguardando workflow (${timeout}s)."
  exit 1
}

cmd_status() {
  require_cmd git
  require_cmd gh
  fetch_all

  local pending published_gh published_nuget
  pending="$(resolve_version)"
  published_gh="$(latest_published_github "${PACKAGES[0]}")"

  log "=== ERP.Fiscal — status NuGet ==="
  log "Repositório:     $(git remote get-url origin 2>/dev/null || echo '?')"
  log "Branch atual:    $(git rev-parse --abbrev-ref HEAD)"
  log "Versão pendente: ${pending} (nuget.props → PackagePatchNumber)"
  log "Última no GH:    ${published_gh} (${PACKAGES[0]})"

  if package_exists_nuget_org "${PACKAGES[0]}" "$pending"; then
    log "NuGet.org:       ${pending} já indexada"
  else
    log "NuGet.org:       ${pending} ainda não indexada (ou não publicada)"
  fi

  log ""
  log "Workflow recente:"
  gh run list --workflow "$WORKFLOW_FILE" --limit 3 2>/dev/null || true

  if git show-ref --verify --quiet "refs/tags/v${pending}"; then
    log ""
    log "Tag local/remota v${pending} existe."
  fi
}

cmd_verify() {
  require_cmd curl
  local version="${1:-}"
  if [[ -z "$version" ]]; then
    version="$(resolve_version)"
    log "Verificando versão informada: ${version}"
  fi

  local ok=true
  for pkg in "${PACKAGES[@]}"; do
    if package_exists_github "$pkg" "$version"; then
      log "OK  GitHub Packages  ${pkg} ${version}"
    else
      log "FALHA GitHub Packages  ${pkg} ${version}"
      ok=false
    fi
    if package_exists_nuget_org "$pkg" "$version"; then
      log "OK  NuGet.org         ${pkg} ${version}"
    else
      log "PEND NuGet.org         ${pkg} ${version} (push pode ter sucesso antes da indexação)"
    fi
  done

  $ok || exit 1
}

cmd_publish() {
  require_cmd git
  require_cmd gh
  require_cmd curl

  if ! gh_authenticated; then
    err "GitHub CLI não autenticado. Execute: gh auth login"
    exit 1
  fi

  if ! git_clean; then
    err "Working tree sujo. Commit ou stash antes do release."
    exit 1
  fi

  fetch_all

  local previous_branch
  previous_branch="$(git rev-parse --abbrev-ref HEAD)"

  if [[ "$previous_branch" != "$MAIN_BRANCH" ]]; then
    log "Checkout ${MAIN_BRANCH}..."
    git checkout "$MAIN_BRANCH"
  fi

  git pull --ff-only origin "$MAIN_BRANCH"

  if $MERGE_DEVELOP; then
    log "Merge origin/${DEVELOP_BRANCH} → ${MAIN_BRANCH}..."
    if $DRY_RUN; then
      log "[dry-run] git merge origin/${DEVELOP_BRANCH} --no-edit"
    else
      git merge "origin/${DEVELOP_BRANCH}" --no-edit
      git push origin "$MAIN_BRANCH"
    fi
  fi

  local version prefix tag message release_sha
  version="$(resolve_version)"
  prefix="$(read_prefix)"
  tag="v${version}"
  message="${CUSTOM_MESSAGE:-chore: trigger release ${tag}}"

  log "Versão a publicar: ${version}"

  if package_exists_github "${PACKAGES[0]}" "$version" && ! $FORCE; then
    err "${PACKAGES[0]} ${version} já existe no GitHub Packages. Use --force para republicar."
    exit 1
  fi

  if $DRY_RUN; then
    log ""
    log "=== DRY RUN — plano ==="
    log "1. git commit --allow-empty -m \"${message}\""
    log "2. git push origin ${MAIN_BRANCH}"
    log "3. Aguardar ${WORKFLOW_NAME} (build, test, pack, push GH + NuGet.org, bump nuget.props)"
    log "4. git tag -a ${tag} <commit-trigger> -m \"Release ${tag}\""
    log "5. git push origin ${tag}"
    log "6. gh release create ${tag} (se não existir)"
    log "7. release.sh verify ${version}"
    exit 0
  fi

  git commit --allow-empty -m "$message"
  release_sha="$(git rev-parse HEAD)"
  log "Commit trigger: ${release_sha}"

  git push origin "$MAIN_BRANCH"

  if ! $NO_WATCH; then
    sleep 3
    wait_for_main_deploy 900
    git pull --ff-only origin "$MAIN_BRANCH"
  else
    log "Pulando watch do workflow (--no-watch). Verifique manualmente: gh run list --workflow ${WORKFLOW_FILE}"
    exit 0
  fi

  if ! $NO_TAG; then
    log "Tag ${tag} → ${release_sha}"
    git tag -fa "$tag" "$release_sha" -m "Release ${tag}"
    git push origin "$tag" --force
  fi

  if ! $NO_TAG; then
    if gh release view "$tag" >/dev/null 2>&1; then
      gh release edit "$tag" \
        --title "$tag" \
        --notes "## ERP.Fiscal ${version}

Publicado em GitHub Packages e NuGet.org via workflow **${WORKFLOW_NAME}**.

**Pacotes:** \`${PACKAGES[0]}\`, \`${PACKAGES[1]}\`"
    else
      gh release create "$tag" \
        --title "$tag" \
        --target "$release_sha" \
        --notes "## ERP.Fiscal ${version}

Publicado em GitHub Packages e NuGet.org via workflow **${WORKFLOW_NAME}**.

**Pacotes:** \`${PACKAGES[0]}\`, \`${PACKAGES[1]}\`"
    fi
  fi

  local next_version
  next_version="$(resolve_version)"
  log ""
  log "=== Release ${version} concluído ==="
  log "Próxima versão em nuget.props: ${next_version}"
  log "Verificando feeds..."
  cmd_verify "$version" || true
  log ""
  log "GitHub Release: https://github.com/${GITHUB_OWNER}/ERP.Fiscal/releases/tag/${tag}"
}

# --- parse args ---
ACTION="${1:-}"
shift || true

while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run) DRY_RUN=true ;;
    --merge-develop) MERGE_DEVELOP=true ;;
    --force) FORCE=true ;;
    --no-tag) NO_TAG=true ;;
    --no-watch) NO_WATCH=true ;;
    --message) shift; CUSTOM_MESSAGE="${1:?--message requer texto}" ;;
    -h|--help) usage 0 ;;
    *) err "Opção desconhecida: $1"; usage 1 ;;
  esac
  shift
done

case "${ACTION:-}" in
  status) cmd_status ;;
  publish|"") cmd_publish ;;
  verify) cmd_verify "${1:-}" ;;
  -h|--help|help) usage 0 ;;
  *)
    err "Ação desconhecida: ${ACTION}"
    usage 1
    ;;
esac
