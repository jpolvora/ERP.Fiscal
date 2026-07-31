#!/usr/bin/env python3
"""
Diff rotas PlugNotas: coleção Postman oficial vs docs/plugnotas/ vs código ERP.Fiscal.

Uso (na raiz do repositório):
  python .agents/skills/sync-plugnotas-docs/scripts/diff-routes.py
  python .agents/skills/sync-plugnotas-docs/scripts/diff-routes.py --scope nfe
  python .agents/skills/sync-plugnotas-docs/scripts/diff-routes.py --cache .tmp-postman.json

Requer: Python 3.9+ (stdlib apenas).
"""

from __future__ import annotations

import argparse
import json
import re
import sys
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

POSTMAN_URL = (
    "https://documenter.gw.postman.com/api/collections/"
    "3720339/2sB3WpSh1R?segregateAuth=true&versionTag=latest"
)

# Segmentos de path dinâmicos → forma canônica para comparação fuzzy.
_PARAM_CANON = {
    "idnota": "id",
    "idnotaorchave": "id",
    "idnotaorprotocol": "id",
    "idnotaorchaveorprotocol": "id",
    "idcertificado": "id",
    "idcertificadorcpfcnpj": "id",
    "idintegracao": "idintegracao",
    "cpfcnpj": "cnpj",
    "cnpj": "cnpj",
    "chaveorprotocol": "protocol",
    "protocol": "protocol",
    "documento": "documento",
    "serie": "serie",
    "cep": "cep",
}

# Escopos ERP.Fiscal: primeiro segmento do path (após /).
SCOPES = frozenset({"certificado", "empresa", "nfe", "cnpj", "cep"})

# Rotas documentadas na lib mas ausentes na coleção Postman (confirmadas no Swagger/código).
KNOWN_POSTMAN_GAPS: frozenset[tuple[str, str]] = frozenset(
    {
        ("PATCH", "/empresa/{cnpj}/config"),
    }
)

# Rotas implementadas na lib — atualizar ao alterar PlugNotasHttpClient / AuxiliaresProvider.
CODE_ROUTES: frozenset[tuple[str, str]] = frozenset(
    {
        ("POST", "/nfe"),
        ("GET", "/nfe/{id}/resumo"),
        ("GET", "/nfe/{cnpj}/{idintegracao}/resumo"),
        ("GET", "/nfe/{id}/xml"),
        ("GET", "/nfe/{id}/pdf"),
        ("POST", "/nfe/{id}/cancelamento"),
        ("POST", "/certificado"),
        ("GET", "/certificado/{id}"),
        ("POST", "/empresa"),
        ("GET", "/empresa/{cnpj}"),
        ("PATCH", "/empresa/{cnpj}/config"),
        ("GET", "/cnpj/{cnpj}"),
        ("GET", "/cep/{cep}"),
        ("GET", "/nfse/cidades"),
        ("GET", "/nfse/cidades/{codigoibge}"),
        ("POST", "/nfse"),
        ("GET", "/nfse/consultar/{id}"),
        ("GET", "/nfse/{cnpj}/{idintegracao}/resumo"),
        ("GET", "/nfse/xml/{id}"),
        ("GET", "/nfse/pdf/{id}"),
        ("POST", "/nfse/{id}/cancelamento"),
    }
)

DOC_FILES_BY_SCOPE = {
    "certificado": ["02-certificado-digital.md"],
    "empresa": ["03-empresa-emissor.md"],
    "nfe": ["05-nfe-endpoints.md"],
    "nfse": ["09-nfse-endpoints.md", "08-auxiliares-cnpj-cep.md"],
    "cnpj": ["08-auxiliares-cnpj-cep.md"],
    "cep": ["08-auxiliares-cnpj-cep.md"],
}


@dataclass(frozen=True)
class Route:
    method: str
    path: str  # ex.: /nfe/{id}/resumo
    source: str
    implemented: bool | None = None  # None = não avaliado

    @property
    def key(self) -> tuple[str, str]:
        return (self.method.upper(), self.path)


def find_repo_root(start: Path) -> Path:
    for parent in [start, *start.parents]:
        if (parent / "AGENTS.md").is_file() and (parent / "docs" / "plugnotas").is_dir():
            return parent
    raise SystemExit("Não foi possível localizar a raiz do repositório (AGENTS.md + docs/plugnotas).")


def canon_param(name: str) -> str:
    key = name.strip("{}:").lower()
    return _PARAM_CANON.get(key, key)


def expand_methods(method_field: str) -> list[str]:
    """Aceita 'GET' ou 'POST/GET/DELETE' em células de tabela."""
    return [m.strip().upper() for m in method_field.split("/") if m.strip()]


def is_example_literal_path(path: str) -> bool:
    """Ignora exemplos HTTP com valores fixos (ex.: /cnpj/18187168000160)."""
    for seg in path.strip("/").split("/"):
        if seg and not seg.startswith("{") and re.fullmatch(r"[\d\-]+", seg):
            return True
    return False


def normalize_path(raw: str) -> str:
    """Converte URL Postman ou path markdown em /segmentos/{param}."""
    path = raw.strip()
    path = re.sub(r"^https?://[^/]+", "", path)
    path = path.split("?", 1)[0]
    if not path.startswith("/"):
        path = "/" + path

    segments: list[str] = []
    for seg in path.split("/"):
        if not seg:
            continue
        if seg.startswith(":") or (seg.startswith("{") and seg.endswith("}")):
            segments.append("{" + canon_param(seg) + "}")
        else:
            segments.append(seg.lower())

    return "/" + "/".join(segments) if segments else "/"


def download_postman(cache: Path | None) -> dict:
    if cache and cache.is_file():
        return json.loads(cache.read_text(encoding="utf-8"))

    req = urllib.request.Request(
        POSTMAN_URL,
        headers={"User-Agent": "ERP.Fiscal-sync-plugnotas-docs/1.0"},
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except urllib.error.URLError as ex:
        raise SystemExit(f"Falha ao baixar coleção Postman: {ex}") from ex

    if cache:
        cache.parent.mkdir(parents=True, exist_ok=True)
        cache.write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")

    return data


def extract_postman_routes(collection: dict, scopes: frozenset[str]) -> list[Route]:
    routes: list[Route] = []

    def walk(items: Iterable[dict]) -> None:
        for item in items:
            req = item.get("request")
            if req:
                method = (req.get("method") or "").upper()
                url = req.get("url")
                path = ""
                if isinstance(url, dict):
                    path = "/" + "/".join(str(p) for p in url.get("path", []))
                elif isinstance(url, str):
                    path = url
                norm = normalize_path(path)
                scope = norm.strip("/").split("/")[0] if norm != "/" else ""
                if scope in scopes:
                    routes.append(Route(method, norm, source="postman"))
            if "item" in item:
                walk(item["item"])

    walk(collection.get("item", []))
    return routes


def parse_doc_routes(docs_dir: Path, scopes: frozenset[str]) -> list[Route]:
    routes: list[Route] = []
    files: set[str] = set()
    for scope in scopes:
        files.update(DOC_FILES_BY_SCOPE.get(scope, []))

    table_re = re.compile(
        r"\|\s*[^|]+\|\s*((?:GET|POST|PUT|PATCH|DELETE)(?:/(?:GET|POST|PUT|PATCH|DELETE))*)\s*\|\s*`([^`]+)`",
        re.IGNORECASE,
    )
    heading_re = re.compile(
        r"^###?\s*(GET|POST|PUT|PATCH|DELETE)\s*—?\s*(.+)$",
        re.IGNORECASE | re.MULTILINE,
    )
    inline_re = re.compile(
        r"^(GET|POST|PUT|PATCH|DELETE)\s+(/[\w/{}\-]+)",
        re.IGNORECASE | re.MULTILINE,
    )

    for filename in sorted(files):
        path = docs_dir / filename
        if not path.is_file():
            continue
        text = path.read_text(encoding="utf-8")

        for method_field, raw_path in table_re.findall(text):
            norm = normalize_path(raw_path)
            for method in expand_methods(method_field):
                impl = "✅" in _table_row_for_path(text, raw_path, method)
                routes.append(
                    Route(method.upper(), norm, source=f"doc:{filename}", implemented=impl)
                )

        for method, rest in heading_re.findall(text):
            m = re.search(r"`?(/[\w/{}\-:]+)`?", rest)
            if m:
                norm = normalize_path(m.group(1))
                routes.append(Route(method.upper(), norm, source=f"doc:{filename}"))

        for method, raw_path in inline_re.findall(text):
            norm = normalize_path(raw_path)
            if is_example_literal_path(norm):
                continue
            routes.append(Route(method.upper(), norm, source=f"doc:{filename}"))

    return routes


def _table_row_for_path(text: str, raw_path: str, method: str) -> str:
    for line in text.splitlines():
        if raw_path in line and method.upper() in line.upper():
            return line
    return ""


def code_routes() -> list[Route]:
    return [
        Route(m, p, source="code", implemented=True)
        for m, p in sorted(CODE_ROUTES)
    ]


def route_scope(path: str) -> str:
    return path.strip("/").split("/")[0] if path != "/" else ""


def filter_by_scope(keys: set[tuple[str, str]], scopes: frozenset[str]) -> set[tuple[str, str]]:
    return {k for k in keys if route_scope(k[1]) in scopes}


def dedupe(routes: Iterable[Route]) -> dict[tuple[str, str], Route]:
    out: dict[tuple[str, str], Route] = {}
    for r in routes:
        prev = out.get(r.key)
        if prev is None:
            out[r.key] = r
            continue
        impl = prev.implemented
        if r.implemented is True:
            impl = True
        elif impl is None:
            impl = r.implemented
        out[r.key] = Route(prev.method, prev.path, prev.source, impl)
    return out


def print_section(title: str, items: list[str]) -> None:
    print(f"\n{title}")
    if not items:
        print("  (nenhum)")
    else:
        for line in items:
            print(f"  - {line}")


def configure_stdout() -> None:
    """UTF-8 no Windows para caracteres como checkmark."""
    if hasattr(sys.stdout, "reconfigure"):
        try:
            sys.stdout.reconfigure(encoding="utf-8")
        except Exception:
            pass


def main() -> int:
    configure_stdout()
    parser = argparse.ArgumentParser(description="Diff rotas PlugNotas oficial vs docs vs código.")
    parser.add_argument(
        "--scope",
        action="append",
        choices=sorted(SCOPES),
        help="Limitar escopo (repita para vários). Padrão: todos os escopos ERP.Fiscal.",
    )
    parser.add_argument(
        "--cache",
        type=Path,
        help="Arquivo JSON da coleção Postman (baixa se ausente).",
    )
    parser.add_argument(
        "--strict",
        action="store_true",
        help="Exit code 1 se houver lacunas na documentação.",
    )
    args = parser.parse_args()

    scopes = frozenset(args.scope) if args.scope else SCOPES
    repo = find_repo_root(Path(__file__).resolve())
    docs_dir = repo / "docs" / "plugnotas"

    collection = download_postman(args.cache)
    official = dedupe(extract_postman_routes(collection, scopes))
    documented = dedupe(parse_doc_routes(docs_dir, scopes))
    implemented = dedupe(code_routes())

    official_keys = set(official)
    doc_keys = set(documented)
    code_keys = filter_by_scope(set(implemented), scopes)

    missing_in_docs = sorted(official_keys - doc_keys)
    extra_in_docs = sorted(
        k for k in (doc_keys - official_keys) if k not in KNOWN_POSTMAN_GAPS
    )
    doc_says_impl = {
        k for k, r in documented.items() if r.implemented is True
    }
    code_missing = sorted(doc_says_impl - code_keys)
    code_only = sorted(
        k for k in (code_keys - official_keys) if k not in KNOWN_POSTMAN_GAPS
    )
    doc_impl_gap = sorted(
        k
        for k in (code_keys & official_keys & doc_keys)
        if documented[k].implemented is not True
    )

    print("PlugNotas route diff — ERP.Fiscal")
    print(f"Repositório: {repo}")
    print(f"Escopos: {', '.join(sorted(scopes))}")
    print(f"Oficial (Postman): {len(official)} rotas | Docs: {len(documented)} | Código: {len(code_keys)}")

    print_section(
        "Lacuna — na API oficial, ausente na documentação local",
        [f"{m} {p}" for m, p in missing_in_docs],
    )
    print_section(
        "Extra — na documentação local, ausente na coleção Postman (revisar/obsoleto)",
        [f"{m} {p} ({documented[(m, p)].source})" for m, p in extra_in_docs],
    )
    print_section(
        "Inconsistencia — doc marca implementado mas codigo nao tem",
        [f"{m} {p}" for m, p in code_missing],
    )
    print_section(
        "Info — implementado na lib, doc local sem coluna ERP.Fiscal marcada",
        [f"{m} {p}" for m, p in doc_impl_gap],
    )
    print_section(
        "Código — implementado, não encontrado na coleção Postman (verificar Swagger)",
        [f"{m} {p}" for m, p in code_only],
    )

    if missing_in_docs or code_missing:
        print("\nAção sugerida: atualizar docs/plugnotas/ e/ou CODE_ROUTES em diff-routes.py")
        return 1 if args.strict else 0

    print("\nOK — nenhuma lacuna crítica detectada no escopo.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
