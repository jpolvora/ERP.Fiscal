#!/usr/bin/env bash
# Resolve ou incrementa a versão patch dos pacotes NuGet (nuget.props).
# Uso:
#   ci-package-version.sh resolve [nuget.props]   → imprime 0.1.N
#   ci-package-version.sh bump [nuget.props]      → incrementa N e imprime o novo N

set -euo pipefail

ACTION="${1:?action required: resolve|bump}"
PROPS_FILE="${2:-nuget.props}"

if [[ ! -f "$PROPS_FILE" ]]; then
  echo "Arquivo não encontrado: $PROPS_FILE" >&2
  exit 1
fi

read_value() {
  local tag="$1"
  sed -n "s|^[[:space:]]*<${tag}>\(.*\)</${tag}>|\1|p" "$PROPS_FILE" | head -n1
}

read_patch_number() {
  read_value "PackagePatchNumber"
}

resolve_version() {
  local prefix patch
  prefix="$(read_value VersionPrefix)"
  patch="$(read_patch_number)"
  if [[ -z "$prefix" || -z "$patch" ]]; then
    echo "VersionPrefix ou PackagePatchNumber ausente em ${PROPS_FILE}" >&2
    exit 1
  fi
  echo "${prefix}.${patch}"
}

case "$ACTION" in
  resolve)
    resolve_version
    ;;
  bump)
    current="$(read_patch_number)"
    if [[ -z "$current" || ! "$current" =~ ^[0-9]+$ ]]; then
      echo "PackagePatchNumber inválido em ${PROPS_FILE}" >&2
      exit 1
    fi
    next=$((current + 1))
    sed -i "s|<PackagePatchNumber>${current}</PackagePatchNumber>|<PackagePatchNumber>${next}</PackagePatchNumber>|" "$PROPS_FILE"
    echo "$next"
    ;;
  *)
    echo "Ação desconhecida: $ACTION (use resolve ou bump)" >&2
    exit 1
    ;;
esac
