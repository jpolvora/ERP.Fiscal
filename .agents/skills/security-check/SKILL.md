---
name: security-check
description: Diretrizes e checagens automáticas/manuais para evitar vazamento de credenciais, chaves, certificados e dados privados — em mudanças locais, arquivos versionados e temporários.
version: 1.1
---

# 🛡️ Security Check — Prevenção de Vazamento de Credenciais e Segredos

Esta skill descreve as regras e procedimentos de segurança obrigatórios para garantir que nenhuma credencial, chave de API (API key), certificado digital, senha ou dado sensível seja exposto publicamente no repositório GitHub.

**Escopo obrigatório da varredura:** a checagem **não** se limita ao diff do commit atual. O agente deve cobrir **três superfícies** em toda execução:

| Superfície | O que inclui | Por quê |
|------------|--------------|---------|
| **Mudanças não commitadas** | Staged (`git diff --cached`), unstaged (`git diff`) e untracked (`git status`) | Risco imediato de commit acidental |
| **Arquivos já versionados** | Todo o working tree rastreado pelo Git (`git ls-files`) | Segredos podem ter entrado em commits anteriores e permanecer no branch |
| **Arquivos temporários** | Caches, dumps, planos locais, `.tmp*`, artefatos de skill/CI — **mesmo se estiverem no `.gitignore`** | Não versionados ≠ seguros; podem ser anexados, copiados ou commitados por engano |

---

## 🚫 1. O que NUNCA deve ser comitado/exposto

- **Chaves de API (API Keys):** Chaves de Sandbox ou Produção da PlugNotas (`SandboxApiKey`, `ProductionApiKey`), chaves de CI/CD (`CURSOR_API_KEY`, `NUGET_API_KEY`), ou qualquer outra credencial.
- **Certificados e Chaves Privadas:** Arquivos com extensões `.pfx`, `.p12`, `.pem`, `.key`, `.der`.
- **Configurações Locais com Segredos:** Arquivos como `appsettings.local.json`, `secrets.json`, `.env` ou `.env.local` que contenham dados de teste reais ou de produção.
- **Caches e temporários com payload sensível:** Ex.: `.tmp-postman-cache.json`, dumps HTTP, fixtures capturados de API real, exports Postman/Insomnia com headers de auth.
- **Dados Pessoais ou de Clientes reais (PII):** CPF, CNPJ reais de produção, nomes, e-mails privados ou payloads com dados reais em fixtures de teste. Use dados mockados / geradores de dados para testes.
- **Planos e Referências a Consumidores Privados:** Arquivos de planos de implementação (`implementation_plan.md`, `task.md`, `walkthrough.md`) ou mensagens de commit que citem explicitamente projetos privados ou ERPs consumidores específicos (ex.: `FiscalWR`). Planos que detalham a integração de um consumidor específico devem permanecer locais nos artifacts temporários ou ser limpos antes de qualquer commit. Apenas planos de alteração genéricos/neutros do próprio core do `ERP.Fiscal` podem ser versionados, se necessário.

---

## 🔍 2. Procedimento de Checagem (pré-commit / fim de sessão / `/security-check`)

Antes de declarar uma tarefa concluída, propor um commit ou responder a `/security-check`, o agente **deve** executar as fases abaixo **na ordem**. Registrar no resumo ao usuário: superfícies varridas, comandos usados e achados (ou “nenhum achado”).

### Fase A — Inventário do working tree

```bash
git status --short
git diff --stat
git diff --cached --stat
```

Garantir que nenhum arquivo temporário de segredos, certificado ou cache sensível apareça como **staged**, **modified** ou **untracked** pronto para `git add`.

Listar untracked explicitamente (inclui temporários ainda não ignorados):

```bash
git ls-files --others --exclude-standard
```

### Fase B — Mudanças não commitadas (staged + unstaged)

Inspecionar **ambos** os diffs — não apenas o staged:

```bash
# Bash — padrões suspeitos no working tree local
git diff --cached -U0 | rg -i 'ApiKey|password|token|secret|private_key|ProductionApiKey|SandboxApiKey|Bearer |Authorization:|-----BEGIN'
git diff -U0         | rg -i 'ApiKey|password|token|secret|private_key|ProductionApiKey|SandboxApiKey|Bearer |Authorization:|-----BEGIN'
```

```powershell
# PowerShell — equivalente
git diff --cached | Select-String -Pattern 'ApiKey','password','token','secret','private_key','ProductionApiKey','SandboxApiKey','Bearer ','Authorization:','-----BEGIN'
git diff          | Select-String -Pattern 'ApiKey','password','token','secret','private_key','ProductionApiKey','SandboxApiKey','Bearer ','Authorization:','-----BEGIN'
```

Para arquivos **novos** (untracked) ainda sem diff no Git, ler o conteúdo ou usar ripgrep diretamente nos paths listados em `git status`.

### Fase C — Arquivos já controlados no repositório

Varredura do conjunto **rastreado** — detecta segredos que já estão no branch, não só no que será commitado agora:

```bash
# Lista de arquivos versionados (respeita .gitignore para untracked; aqui só tracked)
git ls-files -z | xargs -0 rg -i --no-heading \
  'ApiKey\s*=\s*"[^"]{8,}"|password\s*=\s*"[^"]+"|ProductionApiKey|SandboxApiKey|Bearer [A-Za-z0-9._-]{20,}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----' \
  || true
```

Refinar por extensão quando o repo for grande:

```bash
git ls-files '*.json' '*.cs' '*.md' '*.yml' '*.yaml' '*.env*' '*.sh' '*.ps1' | \
  xargs rg -i --no-heading 'ApiKey|password|token|secret|private_key|SandboxApiKey|ProductionApiKey' || true
```

**Appsettings e configs versionados:** validar que valores sensíveis estão vazios (`""`) ou são placeholders — nunca chaves reais em `appsettings.json`, `appsettings.Development.json` versionados, workflows de CI (exceto `${{ secrets.* }}`), docs ou testes.

### Fase D — Arquivos temporários e caches (mesmo ignorados)

Arquivos ignorados pelo Git **ainda entram na checagem** — podem vazar via anexo, cópia ou `git add -f`.

Padrões comuns neste repositório (ver [.gitignore](.gitignore)):

- `.tmp-postman-cache.json`
- `.tmp-*`, `*.tmp`
- `artifacts/`, `packages/` (se existirem localmente)
- `nuget.config.local`, `appsettings.*.local.json`, `.env`, `secrets.json`
- Planos em `.cursor/plans/`, `implementation_plan.md`, `task.md`, `walkthrough.md`

```bash
# Temporários na raiz e subpastas (inclui ignorados)
rg -i --no-heading -g '!.git' \
  'ApiKey|password|token|secret|private_key|SandboxApiKey|ProductionApiKey|Bearer |Authorization:|-----BEGIN' \
  .tmp* **/.tmp* 2>/dev/null || true

# Ou varredura ampla em candidatos a cache (ajustar paths ao repo)
rg -i --no-heading \
  'ApiKey|password|token|secret|SandboxApiKey|ProductionApiKey' \
  .tmp-postman-cache.json .cursor/plans/ 2>/dev/null || true
```

Se um temporário contiver segredo real: **apagar ou sanitizar** o arquivo; confirmar que o padrão está no `.gitignore`; **não** commitar.

### Fase E — Alinhamento com `.gitignore`

Confirmar que novos artefatos sensíveis estão cobertos:

```bash
git check-ignore -v <caminho_do_arquivo>
```

Se um arquivo sensível **não** estiver ignorado, adicionar regra ao `.gitignore` **antes** de encerrar a tarefa (sem commitar o arquivo sensível).

### Fase F — Resumo ao usuário

Formato mínimo do relatório:

1. **Superfícies:** uncommitted / tracked / temporários — cada uma: ✅ limpa ou ⚠️ achados
2. **Achados:** path, linha ou trecho redigido (`***`), severidade, ação tomada ou recomendada
3. **Pronto para commit?** sim/não — se não, listar bloqueios

### Hook local (Husky)

Em cada `git commit`, o hook [`.husky/pre-commit`](../../.husky/pre-commit) executa [`scripts/pre-commit-security-check.sh`](../../scripts/pre-commit-security-check.sh) sobre **arquivos staged**. Setup único: `npm install` na raiz do repo (ativa Husky via `prepare`).

O hook complementa — **não substitui** — a varredura manual das fases A–E (tracked + temporários ignorados).

### Auditoria de histórico (read-only)

Script [`scripts/audit-history-secrets.sh`](../../scripts/audit-history-secrets.sh): varre HEAD, histórico Git, temporários, Gitleaks (opcional), `git-filter-repo --analyze` e `--dry-run` de remediação. Relatórios em `.security-audit/` (gitignored).

```bash
bash scripts/audit-history-secrets.sh --install-gitleaks
```

---

## 🚨 3. O que Fazer se Encontrar um Vazamento

Se durante o desenvolvimento você identificar que algum segredo ou dado sensível foi exposto no código atual:

1. **Remova imediatamente** o segredo do arquivo de código e mova-o para a configuração apropriada do usuário (ex: `secrets.json`, variáveis de ambiente, ou `appsettings.local.json` devidamente ignorado).
2. **Temporários:** delete o arquivo ou substitua valores por placeholders; não confiar só no `.gitignore`.
3. **Se o segredo foi comitado localmente (mas não pushado):**
   - Use `git reset --soft HEAD~1` ou `git commit --amend` para remover a linha com o segredo do commit antes que ele vá para o branch remoto.
4. **Se o segredo já foi pushado para o GitHub:**
   - **Avise o usuário imediatamente no chat** com prioridade máxima.
   - Solicite a revogação imediata da credencial/chave exposta (ela deve ser considerada comprometida).
   - Recomende o uso de ferramentas como `git-filter-repo` ou BFG Repo-Cleaner para purgar o histórico do git se necessário.

---

## 📝 4. Checklist de Segurança do Agente

Antes de encerrar o turno, responder `/security-check` ou propor um commit:

- [ ] **Uncommitted:** `git diff` + `git diff --cached` + untracked revisados — sem segredos novos?
- [ ] **Versionados:** `git ls-files` varrido — sem chaves hardcoded ou PII real em arquivos já no repo?
- [ ] **Temporários:** caches (ex.: `.tmp-postman-cache.json`), planos locais e dumps ignorados inspecionados — sanitizados ou ausentes?
- [ ] Há chaves de API literais no código? (Devem ir em `PlugNotasOptions`, secrets de CI ou config local ignorada.)
- [ ] Algum `.pfx`, `.pem` ou `.key` rastreado ou staged? (Devem estar só no `.gitignore`.)
- [ ] Testes usam CNPJ/CPF/dados fictícios — não produção?
- [ ] Planos/commits evitam nomes de consumidores privados quando o destino é o repo público do `ERP.Fiscal`?
- [ ] `git status` mostra apenas arquivos esperados e seguros para o próximo commit?
- [ ] Novos padrões sensíveis têm entrada correspondente no `.gitignore`?
