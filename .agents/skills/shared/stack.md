# Stack Definition — ERP.Fiscal

Human-readable companion to `config.json`. Agents read config.json for machine-readable values; this doc explains the structure and conventions.

> **Source of truth:** `.agents/skills/shared/config.json` — project identity, stack, verification commands, invariants. `tools.md` — canonical tool aliases.

## Project Stack

- **Backend:** .NET 10 / ABP Module (`Volo.Abp.Core` + `Microsoft.Extensions.Http`)
- **Frontend:** none (library is backend-only)
- **Database:** none (no EF Core, no DbContext, no migrations in this repo)
- **Domain:** neutral PlugNotas transmission; consumer ERPs own aggregates and payload builders
- **Solution:** `ERP.Fiscal.slnx`

## Code Paths

| Layer | Path | Role |
|-------|------|------|
| **Abstractions** | `src/ERP.Fiscal.Abstractions` | Interfaces + neutral result DTOs |
| **PlugNotas** | `src/ERP.Fiscal.PlugNotas` | HTTP, parsers, contracts, providers, module |
| **Tests** | `test/ERP.Fiscal.PlugNotas.Tests` | Unit tests (fake HTTP, parsers, retry) |
| **Docs** | `docs/plugnotas/`, `docs/security/` | Local PlugNotas compile + security index |
| **Scripts** | `scripts/` | Security pre-commit, audit-history, release helpers |

## Validation Commands

| Layer | Tool alias | Config key | Command |
|-------|-----------|------------|---------|
| **Backend** | `build-backend` | `verification.backendBuild` | `dotnet build ERP.Fiscal.slnx` |
| **Backend** | `test-backend` | `verification.backendTest` | `dotnet test ERP.Fiscal.slnx` |

Integration UI/API battery is typically skipped (`defaults.skipIntegration: true`) — this repo has no host app.

## Product skills (root hub)

See root [`AGENTS.md`](../../../AGENTS.md): `sync-plugnotas-docs`, `security-check`, `code-review` (ERP-specific), `release-nuget-package`, `consume-erp-fiscal`.

Workflow catalog: [`.agents/AGENTS.md`](../../AGENTS.md).

## Invariants (lib boundary)

- No EF Core / DbContext / migrations in this repo
- No consumer domain types (`Empresa`, `DocumentoFiscal`, …) in the lib
- Providers receive ready JSON payload + ambiente + correlation ids
- Hardcoded API keys forbidden — use `PlugNotasOptions` + resolvers

## Branches

| Item | Value |
|------|-------|
| **Git remote** | `origin` |
| **Working branch** | `develop` |
| **Base branch** | `main` |
