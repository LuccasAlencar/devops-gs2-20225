# Dockerfile para aplicação .NET 8
# DEVOPS TOOLS & CLOUD COMPUTING - GS2 2025

# Use imagem oficial .NET 8 runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# Use imagem .NET 8 SDK para build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar arquivo de projeto e restaurar dependências
COPY ["dotnet-gs2-2025.csproj", "./"]
RUN dotnet restore "./dotnet-gs2-2025.csproj"

# Copiar todo o código fonte
COPY . .
WORKDIR "/src/."

# Build da aplicação
RUN dotnet build "dotnet-gs2-2025.csproj" -c Release -o /app/build

# Publish da aplicação
FROM build AS publish
RUN dotnet publish "dotnet-gs2-2025.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Imagem final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Criar usuário não-root para segurança
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Configurar variáveis de ambiente
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Iniciar aplicação
ENTRYPOINT ["dotnet", "dotnet-gs2-2025.dll"]
