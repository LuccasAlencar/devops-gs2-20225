#!/bin/bash

# Script Completo: Infraestrutura + Deploy
# Provisiona tudo e faz deploy em um único comando
# Boas práticas: Sem hardcode de senhas, sem sudo, variáveis de ambiente

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${BLUE}╔══════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Deploy Completo: Infraestrutura + Aplicação             ║${NC}"
echo -e "${BLUE}║  DotNet GS2 - Azure                                      ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════╝${NC}"
echo ""

# Carregar variáveis do .env
if [ -f ".env" ]; then
    export $(cat .env | grep -v '^#' | xargs)
    echo -e "${GREEN}✅ Variáveis carregadas do .env${NC}"
elif [ -f "../.env" ]; then
    export $(cat ../.env | grep -v '^#' | xargs)
    echo -e "${GREEN}✅ Variáveis carregadas do .env (raiz)${NC}"
else
    echo -e "${RED}❌ Arquivo .env não encontrado!${NC}"
    echo -e "${YELLOW}Execute este script da raiz do projeto ou da pasta scripts/${NC}"
    exit 1
fi

echo -e "${YELLOW}📋 Configurações:${NC}"
echo "  RM: $RM"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  ACR: $ACR_NAME"
echo "  MySQL: $MYSQL_SERVER"
echo ""

# ===========================================
# PARTE 1: INFRAESTRUTURA
# ===========================================

echo -e "${BLUE}╔══════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  FASE 1: PROVISIONAMENTO DE INFRAESTRUTURA               ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════╝${NC}"
echo ""

# Verificar se está logado no Azure
echo -e "${BLUE}🔐 Verificando login no Azure...${NC}"
if ! az account show &> /dev/null; then
    echo -e "${YELLOW}⚠️  Você não está logado. Faça login:${NC}"
    az login
fi

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
echo -e "${GREEN}✅ Logado na subscription: $SUBSCRIPTION_ID${NC}"
echo ""

# 1. Criar Resource Group
echo -e "${BLUE}📦 [1/8] Criando Resource Group...${NC}"
if az group show --name $RESOURCE_GROUP &> /dev/null; then
    echo -e "${YELLOW}⚠️  Resource Group já existe${NC}"
else
    az group create \
        --name $RESOURCE_GROUP \
        --location "$LOCATION" \
        --output none
    echo -e "${GREEN}✅ Resource Group criado${NC}"
fi
echo ""

# 2. Criar Container Registry (ACR)
echo -e "${BLUE}🐳 [2/8] Criando Azure Container Registry...${NC}"
if az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP &> /dev/null; then
    echo -e "${YELLOW}⚠️  ACR já existe${NC}"
else
    az acr create \
        --resource-group $RESOURCE_GROUP \
        --name $ACR_NAME \
        --sku Basic \
        --admin-enabled true \
        --output none
    echo -e "${GREEN}✅ ACR criado${NC}"
fi

# Aguarda o provisionamento do ACR
echo -e "${BLUE}⏳ Aguardando ACR estar pronto...${NC}"
MAX_RETRIES=12
RETRY=0
while true; do
    EXISTS=$(az acr show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "provisioningState" -o tsv 2>/dev/null)
    if [ "$EXISTS" == "Succeeded" ]; then
        break
    fi
    RETRY=$((RETRY+1))
    if [ $RETRY -ge $MAX_RETRIES ]; then
        echo -e "${RED}❌ Timeout esperando o ACR ser provisionado.${NC}"
        exit 1
    fi
    echo "⏳ Tentativa $RETRY de $MAX_RETRIES..."
    sleep 10
done
echo -e "${GREEN}✅ ACR pronto${NC}"
echo ""

# 3. Criar Storage Account para artefatos
echo -e "${BLUE}💾 [3/8] Criando Storage Account...${NC}"
if az storage account show --name $STORAGE_ACCOUNT --resource-group $RESOURCE_GROUP &> /dev/null; then
    echo -e "${YELLOW}⚠️  Storage Account já existe${NC}"
else
    az storage account create \
        --name $STORAGE_ACCOUNT \
        --resource-group $RESOURCE_GROUP \
        --location "$LOCATION" \
        --sku Standard_LRS \
        --kind StorageV2 \
        --output none
    echo -e "${GREEN}✅ Storage Account criado${NC}"
fi

# Aguarda o provisionamento do Storage Account
sleep 10

# Criar container no storage
echo -e "${BLUE}📦 [4/8] Criando blob container para artefatos...${NC}"
STORAGE_ACCOUNT_KEY=$(az storage account keys list --resource-group $RESOURCE_GROUP --account-name $STORAGE_ACCOUNT --query [0].value -o tsv)

az storage container create \
    --name artifacts \
    --account-name $STORAGE_ACCOUNT \
    --account-key $STORAGE_ACCOUNT_KEY \
    --output none 2>/dev/null || true

echo -e "${GREEN}✅ Blob container pronto${NC}"
echo ""

# 4. Criar Azure Database for MySQL
echo -e "${BLUE}🗄️  [5/8] Criando Azure Database for MySQL...${NC}"
if az mysql flexible-server show --name $MYSQL_SERVER --resource-group $RESOURCE_GROUP &> /dev/null; then
    echo -e "${YELLOW}⚠️  MySQL Server já existe${NC}"
else
    # Tentar criar com retry em caso de erro transitório
    MAX_RETRIES=3
    RETRY=0
    while [ $RETRY -lt $MAX_RETRIES ]; do
        if az mysql flexible-server create \
            --name $MYSQL_SERVER \
            --resource-group $RESOURCE_GROUP \
            --location "$LOCATION" \
            --admin-user $MYSQL_USER \
            --admin-password "$MYSQL_PASSWORD" \
            --database-name $MYSQL_DATABASE \
            --sku-name Standard_B2s \
            --tier Burstable \
            --storage-size 32 \
            --high-availability Disabled \
            --public-access 0.0.0.0 \
            --output none 2>/dev/null; then
            echo -e "${GREEN}✅ MySQL Server criado${NC}"
            break
        else
            RETRY=$((RETRY+1))
            if [ $RETRY -lt $MAX_RETRIES ]; then
                echo -e "${YELLOW}⚠️  Tentativa $RETRY falhou, aguardando 30s...${NC}"
                sleep 30
            else
                echo -e "${RED}❌ Falha ao criar MySQL Server após $MAX_RETRIES tentativas${NC}"
                exit 1
            fi
        fi
    done
fi
echo ""

# 5. Configurar firewall do MySQL
echo -e "${BLUE}🔒 [6/8] Configurando firewall do MySQL...${NC}"
az mysql flexible-server firewall-rule create \
    --rule-name "AllowAzureServices" \
    --name $MYSQL_SERVER \
    --resource-group $RESOURCE_GROUP \
    --start-ip-address "0.0.0.0" \
    --end-ip-address "0.0.0.0" \
    --output none 2>/dev/null || true

echo -e "${GREEN}✅ Firewall configurado${NC}"
echo ""

# ============================================
# PARTE 2: DEPLOY DA APLICAÇÃO
# ============================================

echo -e "${BLUE}╔══════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  FASE 2: BUILD E DEPLOY DA APLICAÇÃO                     ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════╝${NC}"
echo ""

# 6. Fazer login no ACR e obter credenciais atualizadas
echo -e "${BLUE}🔐 [7/8] Fazendo login no ACR...${NC}"

# Obter credenciais atualizadas do ACR
echo -e "${BLUE}🔑 Obtendo credenciais do ACR...${NC}"
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --resource-group $RESOURCE_GROUP --query "passwords[0].value" -o tsv)

if [ -z "$ACR_USERNAME" ] || [ -z "$ACR_PASSWORD" ]; then
    echo -e "${RED}❌ Falha ao obter credenciais do ACR${NC}"
    exit 1
fi

# Fazer login com as credenciais obtidas
echo "$ACR_PASSWORD" | docker login -u "$ACR_USERNAME" --password-stdin $REGISTRY_URL

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Falha ao fazer login no ACR${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Login realizado${NC}"
echo ""

# 7. Build da imagem Docker
echo -e "${BLUE}🏗️  [8/8] Fazendo build da imagem Docker...${NC}"
IMAGE_TAG="${REGISTRY_URL}/${IMAGE_NAME}"

# Determinar caminho do Dockerfile
if [ -f "../Dockerfile" ]; then
    DOCKERFILE_PATH=".."
elif [ -f "Dockerfile" ]; then
    DOCKERFILE_PATH="."
else
    echo -e "${RED}❌ Dockerfile não encontrado!${NC}"
    exit 1
fi

docker build -t $IMAGE_TAG $DOCKERFILE_PATH
echo -e "${GREEN}✅ Build concluído: $IMAGE_TAG${NC}"
echo ""

# 8. Push para o ACR
echo -e "${BLUE}📤 Push da imagem para o ACR...${NC}"
# Aguardar um pouco para garantir que o ACR está pronto
sleep 5
docker push $IMAGE_TAG
echo -e "${GREEN}✅ Push concluído${NC}"
echo ""

# Aguardar o push ser processado
echo -e "${BLUE}⏳ Aguardando processamento da imagem no ACR...${NC}"
sleep 10
echo -e "${GREEN}✅ Imagem pronta${NC}"
echo ""

# 9. Deletar container existente se houver
echo -e "${BLUE}🗑️  Preparando deploy...${NC}"
az container delete \
    --resource-group $RESOURCE_GROUP \
    --name $ACI_NAME \
    --yes 2>/dev/null || true

# 10. Preparar variáveis do MySQL (Azure Database for MySQL)
# Já carregadas do .env, apenas confirmando

# 11. Criar Container Instance
echo -e "${BLUE}🚀 Criando Container Instance...${NC}"
az container create \
    --resource-group $RESOURCE_GROUP \
    --name $ACI_NAME \
    --image $IMAGE_TAG \
    --os-type Linux \
    --cpu 1 \
    --memory 1.5 \
    --registry-login-server $REGISTRY_URL \
    --registry-username $ACR_USERNAME \
    --registry-password $ACR_PASSWORD \
    --ports 8080 \
    --dns-name-label $ACI_NAME \
    --environment-variables \
        ASPNETCORE_ENVIRONMENT=Production \
        ASPNETCORE_URLS=http://+:8080 \
        MYSQL_HOST="$MYSQL_HOST" \
        MYSQL_PORT="$MYSQL_PORT" \
        MYSQL_DATABASE="$MYSQL_DATABASE" \
        MYSQL_USER="$MYSQL_USER" \
        MYSQL_PASSWORD="$MYSQL_PASSWORD" \
    --restart-policy Always \
    --output none

echo -e "${GREEN}✅ Container Instance criado${NC}"
echo ""

# 12. Obter a URL pública
echo -e "${BLUE}🌐 Obtendo URL pública...${NC}"
FQDN=$(az container show \
    --resource-group $RESOURCE_GROUP \
    --name $ACI_NAME \
    --query ipAddress.fqdn \
    --output tsv)

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║  ✅ DEPLOY COMPLETO FINALIZADO COM SUCESSO!             ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${BLUE}📋 INFORMAÇÕES DO DEPLOY:${NC}"
echo ""
echo -e "${YELLOW}🌐 URL da Aplicação:${NC}"
echo "   http://${FQDN}:8080"
echo ""
echo -e "${YELLOW}📚 Endpoints:${NC}"
echo "   Swagger UI: http://${FQDN}:8080/users"
echo "   Health Check: http://${FQDN}:8080/health"
echo ""
echo -e "${YELLOW}📊 Status do Container:${NC}"
az container show \
    --resource-group $RESOURCE_GROUP \
    --name $ACI_NAME \
    --query "{Status:instanceView.state, FQDN:ipAddress.fqdn, IP:ipAddress.ip}" \
    -o table
echo ""
echo -e "${BLUE}💡 COMANDOS ÚTEIS:${NC}"
echo "   Ver logs: az container logs -g $RESOURCE_GROUP -n $ACI_NAME"
echo "   Ver status: az container show -g $RESOURCE_GROUP -n $ACI_NAME"
echo "   Deletar tudo: ./script-delete.sh"
echo ""
echo -e "${GREEN}✨ Deploy finalizado!${NC}"
