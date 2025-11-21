#!/bin/bash

# Script completo unificado de deploy e infraestrutura
# DEVOPS TOOLS & CLOUD COMPUTING - GS2 2025
# Uso: ./deploy.sh [full|infra|docker|app|db|cleanup]

set -e

# Configurações
PROJECT_NAME="devops-gs2-2025"
RESOURCE_GROUP="rg-devops-gs2-2025"
LOCATION="canadacentral"
ACR_NAME="acrdevopsgs22025"
ACI_NAME="aci-devops-gs2-2025"
MYSQL_SERVER_NAME="mysql-devops-gs2-2025"
MYSQL_DATABASE_NAME="devops_gs2_2025"
MYSQL_ADMIN_USER="devopsadmin"
MYSQL_ADMIN_PASSWORD="DevOps@2025!GS2"
STORAGE_ACCOUNT_NAME="stdevopsgs22025"

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Funções
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

check_prerequisites() {
    log_step "Verificando pré-requisitos..."
    
    # Verificar Azure CLI
    if ! command -v az &> /dev/null; then
        log_error "Azure CLI não está instalado. Instale em: https://docs.microsoft.com/pt-br/cli/azure/install-azure-cli"
        exit 1
    fi
    
    # Verificar Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker não está instalado. Instale Docker Desktop"
        exit 1
    fi
    
    # Verificar .NET 8
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET 8 SDK não está instalado. Instale em: https://dotnet.microsoft.com/download/dotnet/8.0"
        exit 1
    fi
    
    # Verificar login no Azure
    if ! az account show &> /dev/null; then
        log_warn "Você não está logado no Azure. Executando login..."
        az login
    fi
    
    log_info "Pré-requisitos verificados com sucesso"
}

setup_infrastructure() {
    log_step "Configurando infraestrutura Azure..."
    
    # Criar Resource Group
    log_info "Criando Resource Group: $RESOURCE_GROUP"
    az group create \
        --name $RESOURCE_GROUP \
        --location $LOCATION \
        --tags "project=devops-gs2-2025" "environment=production"
    
    # Criar ACR (se não existir)
    if az acr show --resource-group $RESOURCE_GROUP --name $ACR_NAME --output none 2>/dev/null; then
        log_info "Azure Container Registry $ACR_NAME já existe"
    else
        log_info "Criando Azure Container Registry: $ACR_NAME"
        az acr create \
            --resource-group $RESOURCE_GROUP \
            --name $ACR_NAME \
            --sku Basic \
            --admin-enabled true || log_warn "ACR já existe ou sem permissão"
    fi
    
    # Criar MySQL Container (se não existir)
    if az container show --resource-group $RESOURCE_GROUP --name aci-mysql-server --output none 2>/dev/null; then
        log_info "MySQL Container já existe"
    else
        log_info "Criando MySQL Container: aci-mysql-server"
        az container create \
            --resource-group $RESOURCE_GROUP \
            --name aci-mysql-server \
            --image mysql:8.0 \
            --os-type Linux \
            --cpu 1 \
            --memory 2 \
            --ports 3306 \
            --dns-name-label aci-mysql-$PROJECT_NAME \
            --restart-policy Always \
            --environment-variables \
                MYSQL_ROOT_PASSWORD=$MYSQL_ADMIN_PASSWORD \
                MYSQL_DATABASE=$MYSQL_DATABASE_NAME \
                MYSQL_USER=$MYSQL_ADMIN_USER \
                MYSQL_PASSWORD=$MYSQL_ADMIN_PASSWORD || log_warn "MySQL Container já existe ou sem permissão"
    fi
    
    # Criar Storage Account (se não existir)
    if az storage account show --resource-group $RESOURCE_GROUP --name $STORAGE_ACCOUNT_NAME --output none 2>/dev/null; then
        log_info "Storage Account $STORAGE_ACCOUNT_NAME já existe"
    else
        log_info "Criando Azure Storage Account: $STORAGE_ACCOUNT_NAME"
        az storage account create \
            --resource-group $RESOURCE_GROUP \
            --name $STORAGE_ACCOUNT_NAME \
            --location $LOCATION \
            --sku Standard_LRS \
            --kind StorageV2 || log_warn "Storage Account já existe ou sem permissão"
    fi
    
    log_info "Infraestrutura configurada com sucesso"
}

build_application() {
    log_step "Build da aplicação .NET..."
    
    # Voltar para diretório raiz
    cd ..
    
    # Restore
    log_info "Restaurando pacotes NuGet..."
    dotnet restore
    
    # Build
    log_info "Compilando aplicação..."
    dotnet build -c Release
    
    # Test
    log_info "Executando testes..."
    dotnet test -c Release --no-build --logger trx --results-directory TestResults
    
    # Publicar artefatos
    log_info "Publicando aplicação..."
    dotnet publish -c Release -o ./publish
    
    # Voltar para scripts
    cd scripts
    
    log_info "Build concluído com sucesso"
}

build_and_push_docker() {
    log_step "Build e push da imagem Docker..."
    
    # Voltar para diretório raiz para build Docker
    cd ..
    
    # Build da imagem
    log_info "Construindo imagem Docker..."
    docker build -f dockerfiles/Dockerfile -t $ACR_NAME.azurecr.io/$PROJECT_NAME:latest .
    
    # Login no ACR
    log_info "Fazendo login no Azure Container Registry..."
    az acr login --name $ACR_NAME
    
    # Push da imagem
    log_info "Enviando imagem para o ACR..."
    docker push $ACR_NAME.azurecr.io/$PROJECT_NAME:latest
    
    # Voltar para scripts
    cd scripts
    
    log_info "Imagem Docker enviada com sucesso"
}

deploy_to_azure() {
    log_step "Deploy para Azure Container Instance..."
    
    # Obter connection string do MySQL Container
    MYSQL_HOST=$(az container show \
        --resource-group $RESOURCE_GROUP \
        --name aci-mysql-server \
        --query ipAddress.fqdn \
        --output tsv)
    
    MYSQL_CONNECTION_STRING="Server=$MYSQL_HOST;Port=3306;Database=$MYSQL_DATABASE_NAME;Uid=$MYSQL_ADMIN_USER;Pwd=$MYSQL_ADMIN_PASSWORD;"
    
    # Criar ou recriar ACI
    log_info "Criando/recriando Azure Container Instance..."
    
    if az container show --resource-group $RESOURCE_GROUP --name $ACI_NAME --output none 2>/dev/null; then
        log_info "Container existe, deletando para recriar..."
        az container delete --resource-group $RESOURCE_GROUP --name $ACI_NAME --yes 2>/dev/null || true
        sleep 10
    fi
    
    log_info "Criando novo container..."
    
    # Obter credenciais do ACR
    ACR_USERNAME=$ACR_NAME
    ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query "passwords[0].value" --output tsv)
    
    log_info "Usando credenciais ACR: $ACR_USERNAME"
    
    az container create \
        --resource-group $RESOURCE_GROUP \
        --name $ACI_NAME \
        --image $ACR_NAME.azurecr.io/$PROJECT_NAME:latest \
        --os-type Linux \
        --cpu 1 \
        --memory 2 \
        --ports 8080 \
        --dns-name-label $ACI_NAME \
        --restart-policy Always \
        --registry-username $ACR_USERNAME \
        --registry-password $ACR_PASSWORD \
        --environment-variables \
            ConnectionStrings__DefaultConnection="$MYSQL_CONNECTION_STRING" \
            ASPNETCORE_ENVIRONMENT=Production \
            ASPNETCORE_URLS=http://+:8080
    
    log_info "Deploy realizado com sucesso"
}

setup_database() {
    log_step "Configurando banco de dados MySQL Container..."
    
    # Esperar MySQL estar pronto
    log_info "Aguardando MySQL iniciar (90 segundos)..."
    sleep 90
    
    log_info "Executando script SQL no MySQL..."
    
    # Converter script SQL para base64 para evitar problemas com caracteres especiais
    SCRIPT_B64=$(base64 -w 0 < script-bd.sql)
    
    # Criar arquivo no container e executar
    az container exec \
        --resource-group $RESOURCE_GROUP \
        --name aci-mysql-server \
        --exec-command "bash -c 'echo $SCRIPT_B64 | base64 -d | mysql -u root -pDevOps@2025!GS2'" 2>&1 | grep -v "Warning" | tail -5 || true
    
    log_info "✅ Banco de dados MySQL configurado com sucesso"
}

get_application_url() {
    log_step "Obtendo URL da aplicação..."
    
    APP_URL=$(az container show \
        --resource-group $RESOURCE_GROUP \
        --name $ACI_NAME \
        --query ipAddress.fqdn \
        --output tsv)
    
    echo ""
    log_info "=== APLICAÇÃO DISPONÍVEL ==="
    echo "URL: http://$APP_URL:8080"
    echo "Health Check: http://$APP_URL:8080/health"
    echo "Swagger: http://$APP_URL:8080"
    echo ""
}

full_deploy() {
    log_info "Iniciando deploy completo..."
    
    check_prerequisites
    setup_infrastructure
    build_application
    build_and_push_docker
    deploy_to_azure
    setup_database
    get_application_url
    
    log_info "Deploy completo realizado com sucesso!"
}

# Menu principal
case "${1:-full}" in
    "full")
        full_deploy
        ;;
    "infra")
        check_prerequisites
        setup_infrastructure
        ;;
    "docker")
        check_prerequisites
        build_application
        build_and_push_docker
        ;;
    "app")
        check_prerequisites
        build_application
        build_and_push_docker
        deploy_to_azure
        ;;
    "db")
        check_prerequisites
        setup_database
        ;;
    "cleanup")
        log_info "Para limpeza completa, use: ./cleanup.sh"
        ;;
    *)
        echo "Uso: $0 [full|infra|docker|app|db|cleanup]"
        echo ""
        echo "Comandos:"
        echo "  full     - Deploy completo (infra + build + docker + deploy + db)"
        echo "  infra    - Apenas provisionar infraestrutura Azure"
        echo "  docker   - Apenas build e push da imagem Docker"
        echo "  app      - Apenas deploy da aplicação (build + docker + deploy)"
        echo "  db       - Apenas configuração do banco de dados"
        echo "  cleanup  - Mostra comando para limpeza"
        exit 1
        ;;
esac
