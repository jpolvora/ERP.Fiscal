```markdown
# ERP.Fiscal Development Patterns

> Auto-generated skill from repository analysis

## Overview

This skill teaches the core development patterns, coding conventions, and collaborative workflows used in the `ERP.Fiscal` repository. The codebase is primarily Python, with no major frameworks detected, and is structured for modularity, clarity, and maintainability. It includes agent skill management, security tooling, provider feature development, and documentation routing. This guide will help you contribute effectively by following established conventions and workflows.

## Coding Conventions

### File Naming

- Use **PascalCase** for file names.
  - Example: `PlugNotasProvider.py`, `FiscalModule.py`

### Import Style

- Use **relative imports** within modules.
  - Example:
    ```python
    from .Contracts import NfeContract
    from .Providers import PlugNotasProvider
    ```

### Export Style

- Use **named exports** (explicitly define what is exported).
  - Example:
    ```python
    __all__ = ["PlugNotasProvider", "NfeContract"]
    ```

### Commit Messages

- Follow **conventional commit** types:
  - Prefixes: `chore`, `docs`, `refactor`, `feat`, `fix`, `delete`
  - Example:
    ```
    feat: add PlugNotas auxiliary provider for NFe events
    fix: correct import path in PlugNotasProvider
    ```

## Workflows

### Agent Skill Package Update

**Trigger:** When you want to add, update, or remove agent skill packages and synchronize agent documentation and routing.  
**Command:** `/update-skills`

1. Update or add files under `.agents/skills/` (e.g., `SKILL.md`, scripts, references).
2. Update `.agents/AGENTS.md` and/or `AGENTS.md` at the root to reflect skill or routing changes.
3. Update related documentation (e.g., `shared/AGENTS.md`, `gates.md`, `setup.md`).
4. Optionally update rules under `.cursor/rules/` or related `.mdc` files.
5. Optionally update scripts or `CHANGELOG.md`.

**Example:**
```bash
# Add a new skill
cp -r .agents/skills/template .agents/skills/new-skill
nano .agents/skills/new-skill/SKILL.md
/update-skills
```

---

### Security Audit Tooling Update

**Trigger:** When you want to improve or add security checks, secret scanning, or compliance automation.  
**Command:** `/add-security-tooling`

1. Add or update scripts for security checks (e.g., `pre-commit-security-check.sh`).
2. Add or update configuration files for security tools (e.g., `.gitleaks.toml`).
3. Update `package.json` and `package-lock.json` to add dependencies if needed.
4. Update documentation (`README.md`, `AGENTS.md`, `SKILL.md` for security-check).
5. Update `.gitignore` as required.

**Example:**
```bash
# Add a pre-commit hook for security
cp scripts/pre-commit-security-check.sh .husky/pre-commit
nano .gitleaks.toml
/add-security-tooling
```

---

### Feature Development: PlugNotas Provider

**Trigger:** When you want to add a new PlugNotas feature, endpoint, or auxiliary provider.  
**Command:** `/add-plugnotas-feature`

1. Implement or extend provider interfaces and contracts (e.g., `INfeAuxiliaresProvider`, `PlugNotasAuxiliaresContracts`).
2. Update or add new classes in `src/ERP.Fiscal.PlugNotas/` (Providers, Contracts, Payload, Extensions).
3. Update or add documentation in `docs/plugnotas/`.
4. Update or add unit tests in `test/ERP.Fiscal.PlugNotas.Tests/`.
5. Update `AGENTS.md` or related mapping documentation as needed.

**Example:**
```python
# src/ERP.Fiscal.PlugNotas/Providers/PlugNotasProvider.py
from .Contracts import NfeContract

class PlugNotasProvider:
    def send_nfe(self, payload: NfeContract):
        # Implementation
        pass
```
```bash
/add-plugnotas-feature
```

---

### Agent Documentation & Routing Update

**Trigger:** When you want to update agent documentation to match new workflow protocols or agent behavior.  
**Command:** `/update-agent-docs`

1. Update `AGENTS.md` and/or `.agents/AGENTS.md` to clarify workflow/session handling.
2. Update shared documentation (e.g., `gates.md`, `setup.md`, `tools.md`).
3. Update or add protocol documentation (e.g., `artifact-cleanup.md`, `delivery-result.md`).
4. Update FAQ or README files to align with new workflow behavior.

**Example:**
```bash
nano AGENTS.md
nano .agents/skills/shared/gates.md
/update-agent-docs
```

## Testing Patterns

- **Framework:** Unknown (no explicit framework detected).
- **Test File Pattern:** Files end with `*Tests.cs`.
- **Location:** `test/ERP.Fiscal.PlugNotas.Tests/`
- **Style:** Tests are written in C# (despite main codebase being Python), likely using a .NET test framework.
- **Example:**
  ```csharp
  // PlugNotasProviderTests.cs
  [Fact]
  public void Should_Send_Nfe_Successfully()
  {
      // Arrange
      // Act
      // Assert
  }
  ```

## Commands

| Command                | Purpose                                                      |
|------------------------|--------------------------------------------------------------|
| /update-skills         | Synchronize agent skill packages and documentation           |
| /add-security-tooling  | Add or update security audit tools and documentation         |
| /add-plugnotas-feature | Develop new PlugNotas provider features and related tests    |
| /update-agent-docs     | Update agent documentation and workflow routing              |
```
