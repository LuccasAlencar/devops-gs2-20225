# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Semantic Versioning](https://semver.org/lang/pt-BR/).

## [2.2.0] - 2024-11-11

### Adicionado
- Integração com Adzuna API para busca de vagas de emprego
- Suporte a variáveis de ambiente via arquivo .env (Oracle e Adzuna)
- Controller de Jobs (V1) com endpoints POST e GET para buscar vagas
- Service AdzunaService com HttpClient para consumir API externa
- DTOs para Jobs (JobDto, JobSearchRequestDto, JobSearchResponseDto)
- CORS habilitado para comunicação com frontend
- Frontend HTML/CSS/JavaScript na pasta frontend/
- Upload de currículo com extração simulada de palavras-chave
- Barra de busca com filtros (cargo, localização, categoria)
- Listagem de vagas encontradas com informações detalhadas

### Alterado
- Credenciais do Oracle agora podem ser configuradas via .env
- Descrição do Swagger atualizada para refletir funcionalidade de busca de vagas

## [2.1.0] - 2024-11-11

### Adicionado
- BCrypt para hash seguro de senhas (substituindo SHA256)
- Work factor de 12 para BCrypt
- Salt automático único para cada senha

### Removido
- Arquivos WeatherForecast (não necessários)
- Documentação de Migrations (uso direto do banco)
- MIGRATIONS_GUIDE.md

### Alterado
- Hash de senhas agora usa BCrypt.Net-Next

## [2.0.0] - 2024-11-11

### Adicionado
- Versão 2 da API com melhorias
- Headers customizados na resposta (X-API-Version, X-Total-Count, X-Total-Pages)
- Mensagens de erro mais detalhadas com timestamp e versão
- Page size padrão aumentado para 20 na V2

### Alterado
- Formato das mensagens de erro na V2
- Estrutura de resposta melhorada

## [1.0.0] - 2024-11-11

### Adicionado
- API RESTful completa para gerenciamento de usuários
- Integração com Oracle Database via Entity Framework Core
- Repository Pattern e Service Layer
- Paginação de resultados
- HATEOAS (Hypermedia as the Engine of Application State)
- Health Checks (completo, ready, live)
- Logging estruturado com Serilog (Console e Arquivo)
- Tracing com OpenTelemetry
- Versionamento de API (v1 e v2)
- Suporte para versionamento via URL, Header e Query String
- Documentação Swagger/OpenAPI
- Validação de dados com Data Annotations
- Status codes HTTP apropriados
- CRUD completo de usuários
- Tratamento de erros consistente
- README completo com documentação
- Arquivo .http com exemplos de requisições
- .gitignore configurado
- CONTRIBUTING.md com guia de contribuição
