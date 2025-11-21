#!/bin/bash

# Script para deletar todos os recursos do Azure - DotNet GS2
# Boas práticas: Confirmação do usuário, sem sudo, limpeza segura

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${RED}╔══════════════════════════════════════════════════════════╗${NC}"
echo -e "${RED}║  Delete Azure Resources - DotNet GS2                     ║${NC}"
echo -e "${RED}╚══════════════════════════════════════════════════════════╝${NC}"
echo ""

# Carregar variáveis do .env
if [ -f ".env" ]; then
    export $(cat .env | grep -v '^#' | xargs)
    echo -e "${GREEN}✅ Variáveis carregadas do .env${NC}"
elif [ -f "../.env" ]; then
    export $(cat ../.env | grep -v '^#' | xargs)
    echo -e "${GREEN}✅ Variáveis carregadas${NC}"
else
    echo -e "${RED}❌ Arquivo .env não encontrado!${NC}"
    exit 1
fi

# Verificar se está logado no Azure
echo -e "${BLUE}🔐 Verificando login no Azure...${NC}"
if ! az account show &> /dev/null; then
    echo -e "${RED}❌ Você não está logado no Azure. Faça login:${NC}"
    az login
fi

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
echo -e "${GREEN}✅ Logado na subscription: $SUBSCRIPTION_ID${NC}"
echo ""

# Verificar se o Resource Group existe
if ! az group show --name $RESOURCE_GROUP &> /dev/null; then
    echo -e "${YELLOW}⚠️  Resource Group '$RESOURCE_GROUP' não encontrado.${NC}"
    echo -e "${GREEN}✅ Nada para deletar!${NC}"
    exit 0
fi

# Listar recursos
echo -e "${BLUE}📋 Recursos encontrados no Resource Group:${NC}"
az resource list --resource-group $RESOURCE_GROUP --query "[].{Name:name, Type:type}" -o table
echo ""

# Confirmar exclusão
echo -e "${RED}⚠️  ATENÇÃO: Todos os recursos acima serão DELETADOS!${NC}"
echo -e "${YELLOW}   - Azure Container Instance (Aplicação)${NC}"
echo -e "${YELLOW}   - Azure Container Registry${NC}"
echo -e "${YELLOW}   - Azure Database for MySQL${NC}"
echo -e "${YELLOW}   - Storage Account${NC}"
echo -e "${YELLOW}   - Resource Group${NC}"
echo ""
read -p "Deseja continuar? (digite 'sim' para confirmar): " confirmacao

if [ "$confirmacao" != "sim" ]; then
    echo -e "${BLUE}❌ Operação cancelada pelo usuário.${NC}"
    exit 0
fi

echo ""
echo -e "${RED}🗑️  Deletando Resource Group e todos os recursos...${NC}"
echo "   Isso pode levar alguns minutos..."
echo ""

# Deletar Resource Group
az group delete --name $RESOURCE_GROUP --yes --no-wait

echo ""
echo -e "${GREEN}✅ Comando de exclusão enviado!${NC}"
echo ""
echo -e "${BLUE}💡 INFORMAÇÕES:${NC}"
echo "   - A exclusão está em andamento em background"
echo "   - Pode levar de 5 a 10 minutos para completar"
echo "   - Verifique o status no portal do Azure"
echo ""
echo -e "${YELLOW}🔍 Para verificar o status da exclusão:${NC}"
echo "   az group show --name $RESOURCE_GROUP"
echo ""
echo -e "${BLUE}🧹 Limpando arquivos locais...${NC}"
rm -f scripts/.mysql-credentials
rm -f scripts/infrastructure-output.env
rm -f .mysql-credentials
echo -e "${GREEN}✅ Arquivos locais removidos${NC}"
echo ""
echo -e "${GREEN}✨ Script finalizado!${NC}"
