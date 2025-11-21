# Scripts de Infraestrutura - DotNet GS2

Conjunto de scripts bash para provisionamento e deploy automático na Azure, seguindo boas práticas de segurança e DevOps.

## 📋 Pré-requisitos

- Azure CLI instalado e configurado
- Docker instalado e rodando
- Bash shell (Linux, macOS ou WSL no Windows)
- Permissões para criar recursos na Azure

## 🔐 Boas Práticas de Segurança

### ✅ Implementadas

- **Sem hardcode de senhas**: Senhas são geradas dinamicamente ou carregadas de variáveis de ambiente
- **Sem sudo**: Todos os scripts rodam sem privilégios elevados
- **Proteção de credenciais**: Arquivo `.mysql-credentials` criado com permissões 600 (apenas leitura do proprietário)
- **Variáveis de ambiente**: Configurações customizáveis via variáveis de ambiente
- **Confirmação de exclusão**: Script de delete pede confirmação antes de deletar recursos
- **Separação de responsabilidades**: Cada script tem um propósito específico

## 📁 Estrutura dos Scripts

```
scripts/
├── deploy-complete.sh       # ⭐ Infraestrutura + Deploy (tudo em um comando)
├── script-delete.sh         # Deleta todos os recursos
└── README.md               # Este arquivo
```

## 🚀 Como Usar

### ⭐ RECOMENDADO: Deploy Completo (Tudo em Um Comando)

```bash
cd scripts
./deploy-complete.sh
```

**O que faz em uma única execução:**
1. Provisiona toda a infraestrutura (Resource Group, ACR, Storage, MySQL)
2. Faz build da imagem Docker
3. Push para ACR
4. Deploy no Azure Container Instance
5. Exibe URL da aplicação

**Pré-requisito:**
- Arquivo `.env` na raiz do projeto (com variáveis configuradas)
- Dockerfile na raiz do projeto

**Saída:**
- Infraestrutura completa provisionada
- Aplicação rodando
- URL pública disponível

---

### Deletar Todos os Recursos

```bash
cd scripts
./script-delete.sh
```

**O que faz:**
- Lista recursos a serem deletados
- Pede confirmação do usuário
- Deleta Resource Group (e todos os recursos dentro)
- Remove arquivos locais de credenciais

**Segurança:**
- Requer confirmação digitando "sim"
- Não deleta sem confirmação

## 📊 Fluxo de Execução

```
deploy-complete.sh
├─ Criar Resource Group
├─ Criar ACR
├─ Criar Storage Account
├─ Criar MySQL Database
├─ Build Docker
├─ Push para ACR
└─ Deploy em ACI
    ↓
Aplicação rodando em http://<FQDN>:8080
```

## 🔑 Gerenciamento de Credenciais

### Arquivo `.mysql-credentials`

Criado automaticamente por `deploy-complete.sh`:

```bash
# Credenciais MySQL - Proteger este arquivo!
DB_HOST=mysql-dotnet-gs2-rm556152.mysql.database.azure.com
DB_PORT=3306
DB_NAME=dotnet_gs2
DB_USER=adminuser
DB_PASSWORD=DotNet@GS2RM556152...
```

**Proteção:**
- Permissões: `600` (apenas proprietário pode ler)
- Adicionado ao `.gitignore`
- Nunca fazer commit deste arquivo

### Arquivo `infrastructure-output.env`

Contém variáveis de infraestrutura (sem senhas):

```bash
RESOURCE_GROUP=rg-dotnet-gs2-rm556152
ACR_NAME=acrdotnetgs2rm556152
REGISTRY_URL=acrdotnetgs2rm556152.azurecr.io
MYSQL_SERVER=mysql-dotnet-gs2-rm556152
...
```

**Proteção:**
- Adicionado ao `.gitignore`
- Pode ser recriado executando `deploy-complete.sh`

## 🛠️ Variáveis de Ambiente

### Customização Global

```bash
# Mudar RM
export RM=123456

# Mudar localização
export LOCATION=westus2

# Mudar banco de dados
export MYSQL_DATABASE=meu_banco

# Mudar usuário admin
export MYSQL_ADMIN_USER=meuuser

# Executar script
./deploy-complete.sh
```

## 📝 Logs e Monitoramento

### Ver logs do container

```bash
az container logs -g rg-dotnet-gs2-rm556152 -n aci-dotnet-gs2-rm556152
```

### Ver status do container

```bash
az container show -g rg-dotnet-gs2-rm556152 -n aci-dotnet-gs2-rm556152
```

### Ver recursos do Resource Group

```bash
az resource list -g rg-dotnet-gs2-rm556152 -o table
```

## ⚠️ Troubleshooting

### Erro: "Arquivo .env não encontrado"

**Solução:** Certifique-se de estar na pasta correta

```bash
# ✅ Correto
cd /path/to/dotnet-gs2-2025/scripts
./deploy-complete.sh

# ❌ Errado
cd /path/to/dotnet-gs2-2025
./deploy-complete.sh
```

### Erro: "Você não está logado no Azure"

**Solução:** Faça login no Azure

```bash
az login
```

### Erro: "Docker daemon is not running"

**Solução:** Inicie o Docker

```bash
# No Windows
docker run hello-world

# No Linux/macOS
sudo systemctl start docker
```

### Erro: "Permission denied" ao executar scripts

**Solução:** Adicione permissão de execução

```bash
chmod +x scripts/*.sh
```

## 🔄 Redeploy

Para fazer um novo deploy (atualizar código):

```bash
cd scripts
./deploy-complete.sh
```

O script automaticamente:
- Deleta o container anterior
- Faz novo build
- Faz novo push
- Cria novo container

## 📚 Referências

- [Azure CLI Documentation](https://docs.microsoft.com/cli/azure/)
- [Azure Container Instances](https://docs.microsoft.com/azure/container-instances/)
- [Azure Container Registry](https://docs.microsoft.com/azure/container-registry/)
- [Azure Database for MySQL](https://docs.microsoft.com/azure/mysql/)

## 📄 Licença

Parte do projeto DotNet GS2 - FIAP
