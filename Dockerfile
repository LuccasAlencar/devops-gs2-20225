# Dockerfile - Multi-stage build para aplicação .NET 8

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /app

# Limpar cache do NuGet
RUN rm -rf /root/.nuget/packages && rm -rf /root/.cache

# Copiar arquivo de projeto
COPY dotnet-gs2-2025.csproj .

# Restaurar dependências
RUN dotnet restore "dotnet-gs2-2025.csproj"

# Copiar código-fonte
COPY . .

# Build e Publish (sem usar cache)
RUN dotnet publish "dotnet-gs2-2025.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Criar usuário não-root para segurança
RUN groupadd -r appuser && useradd -r -g appuser appuser

WORKDIR /app

# Copiar artefatos do build
COPY --from=builder /app/publish .

# Expor porta padrão do ASP.NET Core
EXPOSE 8080
EXPOSE 8081

# Variáveis de ambiente - MYSQL (obrigatórias)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV MYSQL_HOST=localhost
ENV MYSQL_PORT=3306
ENV MYSQL_DATABASE=dotnet_gs2
ENV MYSQL_USER=root
ENV MYSQL_PASSWORD=password

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Trocar para usuário não-root
USER appuser

# Executar aplicação
ENTRYPOINT ["dotnet", "dotnet-gs2-2025.dll"]
