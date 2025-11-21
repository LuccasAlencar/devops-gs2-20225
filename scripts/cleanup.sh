#!/bin/bash

# Script de limpeza completa da infraestrutura
# DEVOPS TOOLS & CLOUD COMPUTING - GS2 2025
# Uso: ./cleanup.sh [all|docker|infra|images]

set -e

# Configurações
PROJECT_NAME="devops-gs2-2025"
RESOURCE_GROUP="rg-devops-gs2-2025"
ACR_NAME="acrdevopsgs22025"
ACI_NAME="aci-devops-gs2-2025"
CONTAINER_REGISTRY="$ACR_NAME.azurecr.io"
LOCATION="canadacentral"

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

confirm_action() {
    echo ""
    log_warn "ATENÇÃO: Esta ação é IRREVERSÍVEL!"
    read -p "Tem certeza que deseja continuar? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        log_info "Operação cancelada"
        exit 0
    fi
}

check_azure_cli() {
    if ! command -v az &> /dev/null; then
        log_error "Azure CLI não está instalado"
        exit 1
    fi
    
    if ! az account show &> /dev/null; then
        log_warn "Você não está logado no Azure. Executando login..."
        az login
    fi
}

cleanup_docker_images() {
    log_step "Limpando imagens Docker locais..."
    
    # Remover imagens do projeto
    log_info "Removendo imagens Docker locais..."
    
    # Remover imagem local se existir
    if docker images -q $CONTAINER_REGISTRY/$PROJECT_NAME:latest | grep -q .; then
        docker rmi $CONTAINER_REGISTRY/$PROJECT_NAME:latest 2>/dev/null || true
        log_info "Imagem local removida"
    fi
    
    # Limpar dangling images
    log_info "Limpando imagens pendentes..."
    docker image prune -f 2>/dev/null || true
    
    log_info "Limpeza Docker concluída"
}

cleanup_azure_container_instance() {
    log_step "Removendo Azure Container Instance..."
    
    if az container show --resource-group $RESOURCE_GROUP --name $ACI_NAME --output none 2>/dev/null; then
        log_info "Removendo ACI: $ACI_NAME"
        az container delete \
            --resource-group $RESOURCE_GROUP \
            --name $ACI_NAME \
            --yes
        log_info "ACI removido com sucesso"
    else
        log_warn "ACI não encontrado"
    fi
}

cleanup_mysql_flexible_server() {
    log_step "Removendo MySQL Flexible Server..."
    
    if az mysql flexible-server show --resource-group $RESOURCE_GROUP --name $MYSQL_SERVER_NAME --output none 2>/dev/null; then
        log_info "Removendo MySQL Flexible Server: $MYSQL_SERVER_NAME"
        az mysql flexible-server delete \
            --resource-group $RESOURCE_GROUP \
            --name $MYSQL_SERVER_NAME \
            --yes
        log_info "MySQL Flexible Server removido com sucesso"
    else
        log_warn "MySQL Flexible Server não encontrado"
    fi
}

cleanup_acr_images() {
    log_step "Limpando imagens do Azure Container Registry..."
    
    if az acr show --resource-group $RESOURCE_GROUP --name $ACR_NAME --output none 2>/dev/null; then
        log_info "Listando imagens no ACR..."
        
        # Listar repositórios
        REPOS=$(az acr repository list --resource-group $RESOURCE_GROUP --name $ACR_NAME --output tsv 2>/dev/null || echo "")
        
        if [ ! -z "$REPOS" ]; then
            for repo in $REPOS; do
                log_info "Removendo repositório: $repo"
                az acr repository delete \
                    --resource-group $RESOURCE_GROUP \
                    --name $ACR_NAME \
                    --repository $repo \
                    --yes 2>/dev/null || true
            done
            log_info "Imagens do ACR removidas"
        else
            log_warn "Nenhuma imagem encontrada no ACR"
        fi
    else
        log_warn "ACR não encontrado"
    fi
}

cleanup_infrastructure() {
    log_step "Removendo toda a infraestrutura Azure..."
    
    if az group show --name $RESOURCE_GROUP --output none 2>/dev/null; then
        log_info "Removendo Resource Group: $RESOURCE_GROUP"
        az group delete \
            --name $RESOURCE_GROUP \
            --yes \
            --no-wait
        log_info "Resource Group marcado para deleção"
    else
        log_warn "Resource Group não encontrado"
    fi
}

cleanup_azure_devops() {
    log_step "Limpando recursos do Azure DevOps..."
    
    # Limpar build artifacts locais
    log_info "Limpando arquivos temporários..."
    
    # Remover diretórios de build
    if [ -d "bin" ]; then
        rm -rf bin
        log_info "Diretório bin removido"
    fi
    
    if [ -d "obj" ]; then
        rm -rf obj
        log_info "Diretório obj removido"
    fi
    
    if [ -d "publish" ]; then
        rm -rf publish
        log_info "Diretório publish removido"
    fi
    
    if [ -d "TestResults" ]; then
        rm -rf TestResults
        log_info "Diretório TestResults removido"
    fi
    
    # Limpar logs
    if [ -d "logs" ]; then
        rm -rf logs/*
        log_info "Logs removidos"
    fi
    
    log_info "Limpeza local concluída"
}

full_cleanup() {
    log_info "Iniciando limpeza completa..."
    
    check_azure_cli
    confirm_action
    
    cleanup_docker_images
    cleanup_azure_container_instance
    cleanup_mysql_flexible_server
    cleanup_acr_images
    cleanup_infrastructure
    cleanup_azure_devops
    
    log_info "Limpeza completa realizada com sucesso!"
    log_warn "A deleção do Resource Group pode levar alguns minutos para ser processada pelo Azure"
}

# Menu principal
case "${1:-all}" in
    "all")
        full_cleanup
        ;;
    "docker")
        cleanup_docker_images
        ;;
    "infra")
        check_azure_cli
        confirm_action
        cleanup_azure_container_instance
        cleanup_acr_images
        cleanup_infrastructure
        ;;
    "images")
        check_azure_cli
        cleanup_azure_container_instance
        cleanup_acr_images
        ;;
    "local")
        cleanup_docker_images
        cleanup_azure_devops
        ;;
    *)
        echo "Uso: $0 [all|docker|infra|images|local]"
        echo ""
        echo "Comandos:"
        echo "  all     - Limpeza completa (Docker + Azure + Local)"
        echo "  docker  - Apenas imagens Docker locais"
        echo "  infra   - Apenas infraestrutura Azure (ACI + ACR + Resource Group)"
        echo "  images  - Apenas imagens (ACI + ACR)"
        echo "  local   - Apenas recursos locais (Docker + arquivos temporários)"
        echo ""
        echo "AVISO: Use com cuidado! Esta ação é irreversível."
        exit 1
        ;;
esac
