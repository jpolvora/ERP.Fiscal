---
name: security-check
description: Diretrizes e checagens automáticas/manuais para evitar vazamento de credenciais, chaves, certificados e dados privados antes de commits ou publicação.
version: 1.0
---

# 🛡️ Security Check — Prevenção de Vazamento de Credenciais e Segredos

Esta skill descreve as regras e procedimentos de segurança obrigatórios para garantir que nenhuma credencial, chave de API (API key), certificado digital, senha ou dado sensível seja exposto publicamente no repositório GitHub.

---

## 🚫 1. O que NUNCA deve ser comitado/exposto

- **Chaves de API (API Keys):** Chaves de Sandbox ou Produção da PlugNotas (`SandboxApiKey`, `ProductionApiKey`), chaves de CI/CD (`CURSOR_API_KEY`, `NUGET_API_KEY`), ou qualquer outra credencial.
- **Certificados e Chaves Privadas:** Arquivos com extensões `.pfx`, `.p12`, `.pem`, `.key`, `.der`.
- **Configurações Locais com Segredos:** Arquivos como `appsettings.local.json`, `secrets.json`, `.env` ou `.env.local` que contenham dados de teste reais ou de produção.
- **Dados Pessoais ou de Clientes reais (PII):** CPF, CNPJ reais de produção, nomes, e-mails privados ou payloads com dados reais em fixtures de teste. Use dados mockados / geradores de dados para testes.

---

## 🔍 2. Procedimento de Checagem Pré-Commit / Fim de Sessão

Antes de declarar uma tarefa concluída ou propor um commit, o agente **deve** realizar os seguintes passos:

### Passo A: Validar Arquivos Modificados / Novos
Verifique a lista de arquivos alterados e garanta que nenhum arquivo temporário de segredos ou certificados esteja na lista de arquivos a serem adicionados (`git status`).

### Passo B: Buscar Padrões Suspeitos nas Mudanças
Inspecione o diff do código buscando palavras-chave sensíveis adicionadas. Execute comandos Git e ripgrep para validar:

#### Comando no PowerShell para buscar chaves/tokens no diff:
```powershell
git diff --cached | Select-String -Pattern "ApiKey", "password", "token", "secret", "private_key", "ProductionApiKey", "SandboxApiKey"
```

#### Comando ripgrep (caso use a ferramenta grep_search):
Buscar ativamente em arquivos modificados ou novos por termos que indicam atribuições diretas de chaves (ex: `ApiKey = "..."` com valores reais).

### Passo C: Validar o Alinhamento com o `.gitignore`
Assegure-se de que as regras do [.gitignore](file:///l:/SOURCE_AZURE/ERP.Fiscal.sync/.gitignore) estão sendo respeitadas. Se novos arquivos de configuração foram criados, confirme se a extensão ou padrão deles está devidamente ignorado.

Para testar se um arquivo está sendo ignorado pelo git:
```powershell
git check-ignore -v <caminho_do_arquivo>
```

---

## 🚨 3. O que Fazer se Encontrar um Vazamento

Se durante o desenvolvimento você identificar que algum segredo ou dado sensível foi exposto no código atual:

1. **Remova imediatamente** o segredo do arquivo de código e mova-o para a configuração apropriada do usuário (ex: `secrets.json`, variáveis de ambiente, ou `appsettings.local.json` devidamente ignorado).
2. **Se o segredo foi comitado localmente (mas não pushado):**
   - Use `git reset --soft HEAD~1` ou `git commit --amend` para remover a linha com o segredo do commit antes que ele vá para o branch remoto.
3. **Se o segredo já foi pushado para o GitHub:**
   - **Avise o usuário imediatamente no chat** com prioridade máxima.
   - Solicite a revogação imediata da credencial/chave exposta (ela deve ser considerada comprometida).
   - Recomende o uso de ferramentas como `git-filter-repo` ou BFG Repo-Cleaner para purgar o histórico do git se necessário.

---

## 📝 4. Checklist de Segurança do Agente

Antes de encerrar o turno ou propor um commit, responda mentalmente ou liste as seguintes checagens:
- [ ] Há chaves de API literais (hardcoded) no código? (Devem ser configuradas via `PlugNotasOptions` ou injeção/configuração).
- [ ] Adicionei algum arquivo `.pfx`, `.pem`, ou `.key` ao repositório git? (Devem estar listados no `.gitignore`).
- [ ] Usei CNPJ, CPF ou dados reais de produção para rodar testes? (Utilizar dados de teste/sandbox fictícios).
- [ ] O `git status` mostra apenas arquivos esperados e seguros?
