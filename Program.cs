using Asp.Versioning;
using dotnet_gs2_2025.Configuration;
using dotnet_gs2_2025.Data;
using dotnet_gs2_2025.Repositories;
using dotnet_gs2_2025.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

// Carregar variáveis de ambiente do arquivo .env (se existir localmente)
try
{
    using (var env = new System.IO.FileStream(".env", System.IO.FileMode.Open, System.IO.FileAccess.Read))
    {
        DotNetEnv.Env.Load();
    }
}
catch
{
    // .env não existe em produção, variáveis virão do ambiente
}

// Configuração do Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Iniciando aplicação...");

    var builder = WebApplication.CreateBuilder(args);

    // Configurar Serilog
    builder.Host.UseSerilog();

    // Detectar se está em modo design-time (migrations)
    var isDesignTime = args.Contains("--design-time");
    
    // Configuração do MySQL (Azure ou Local)
    Log.Information("Configurando Azure Database for MySQL");
    
    // Forçar uso de variáveis de ambiente individuais (para Azure Container Instance)
    var mysqlHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
    var mysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
    var mysqlDatabase = Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "dotnet_gs2";
    var mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
    var mysqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? "password";
    
    Log.Information("Variáveis de conexão MySQL carregadas:");
    Log.Information("  Host: {Host}", mysqlHost);
    Log.Information("  Port: {Port}", mysqlPort);
    Log.Information("  Database: {Database}", mysqlDatabase);
    Log.Information("  User: {User}", mysqlUser);
    
    // Usar SslMode=Required apenas para Azure, usar None para localhost
    var sslMode = mysqlHost.Contains("azure") ? "Required" : "None";
    string connectionString = $"Server={mysqlHost};Port={mysqlPort};Database={mysqlDatabase};Uid={mysqlUser};Pwd={mysqlPassword};SslMode={sslMode};";
    
    Log.Information("Connection string construída a partir de variáveis de ambiente");
    
    Log.Information("Connection string configurada: {ConnectionString}", 
        System.Text.RegularExpressions.Regex.Replace(connectionString, @"Pwd=[^;]+", "Pwd=***"));
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

    // Dependency Injection
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    
    builder.Services.Configure<HuggingFaceOptions>(builder.Configuration.GetSection(HuggingFaceOptions.SectionName));

    builder.Services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();
    builder.Services.AddScoped<IResumeService, ResumeService>();

    // Configurar HttpClient e serviços externos
    builder.Services.AddHttpClient<IAdzunaService, AdzunaService>();
    builder.Services.AddHttpClient<IHuggingFaceService, HuggingFaceService>(client =>
    {
        client.BaseAddress = new Uri("https://router.huggingface.co/hf-inference/");
        client.Timeout = TimeSpan.FromSeconds(60);
    });
    
    // Registrar o serviço de sugestão de cargos
    builder.Services.AddScoped<IJobSuggestionService, JobSuggestionService>();

    // Configuração de CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Configuração de API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-API-Version"),
            new QueryStringApiVersionReader("api-version"));
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddMySql(
            connectionString!,
            name: "mysql-database",
            timeout: TimeSpan.FromSeconds(3),
            tags: new[] { "db", "mysql", "database" });

    // OpenTelemetry (Tracing)
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService("UserAPI"))
                .AddAspNetCoreInstrumentation()
                .AddConsoleExporter();
        });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Configuração do Swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Users API - V1",
            Version = "v1",
            Description = "API RESTful buscadora de vagas com Adzuna, desenvolvida em .NET 8 com MySQL Database.",
            Contact = new OpenApiContact
            {
                Name = "Suporte API",
                Email = "suporte@exemplo.com"
            }
        });

        c.SwaggerDoc("v2", new OpenApiInfo
        {
            Title = "Users API - V2",
            Version = "v2",
            Description = "API RESTful buscadora de vagas com Adzuna, desenvolvida em .NET 8 com MySQL Database - Versão 2 (Melhorada)",
            Contact = new OpenApiContact
            {
                Name = "Suporte API",
                Email = "suporte@exemplo.com"
            }
        });

        // Incluir comentários XML se existir
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    var app = builder.Build();

    // Aplicar migrations automaticamente na primeira execução
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Log.Information("Aplicando migrations do banco de dados...");
            dbContext.Database.Migrate();
            Log.Information("✅ Migrations aplicadas com sucesso");
            
            // Verificar se a tabela users existe, se não criar
            try
            {
                var userCount = dbContext.Users.CountAsync().Result;
                Log.Information("✅ Tabela users verificada - {Count} usuários existentes", userCount);
            }
            catch (Exception tableEx)
            {
                Log.Warning("⚠️  Erro ao verificar tabela users: {Message}", tableEx.Message);
                
                // Tentar criar tabela com SQL bruto se migration falhou
                Log.Information("Tentando criar tabela users com SQL direto...");
                var createTableSql = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id INT PRIMARY KEY AUTO_INCREMENT,
                        name VARCHAR(100) NOT NULL,
                        email VARCHAR(150) NOT NULL UNIQUE,
                        password VARCHAR(255) NOT NULL,
                        phone VARCHAR(20),
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                        INDEX idx_email (email),
                        INDEX idx_created_at (created_at)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                ";
                
                dbContext.Database.ExecuteSqlRaw(createTableSql);
                Log.Information("✅ Tabela users criada com sucesso");
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Erro ao aplicar migrations: {Message}", ex.Message);
        // Não falhar a aplicação se as migrations falharem
        // O banco pode já estar migrado
    }

    // Middleware para logging de requisições
    app.UseSerilogRequestLogging();

    // Middleware global de tratamento de exceções
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            Log.Error(exception, "Erro não tratado na aplicação");

            var response = new
            {
                message = "Erro interno do servidor",
                detail = exception?.Message,
                path = exceptionHandlerPathFeature?.Path
            };

            await context.Response.WriteAsJsonAsync(response);
        });
    });

    // Configure the HTTP request pipeline
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Users API V1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Users API V2");
        c.RoutePrefix = string.Empty; // Swagger na raiz
    });

    // Health Check Endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    app.UseHttpsRedirection();

    // Habilitar CORS
    app.UseCors("AllowFrontend");

    app.UseAuthorization();
    app.MapControllers();

    Log.Information("Aplicação iniciada com sucesso");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
