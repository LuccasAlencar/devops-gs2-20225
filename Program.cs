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
using DotNetEnv;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

// Carregar variáveis de ambiente do arquivo .env
Env.Load();

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

    // Detectar qual banco de dados usar
    // Prioridade: FORCE_MYSQL (para migrations) > MYSQL_HOST (Azure) > ORACLE_DATA_SOURCE (Local) > appsettings.json
    var forceMySQL = Environment.GetEnvironmentVariable("FORCE_MYSQL");
    var mysqlHost = Environment.GetEnvironmentVariable("MYSQL_HOST");
    var oracleDataSource = Environment.GetEnvironmentVariable("ORACLE_DATA_SOURCE");
    var oracleUserId = Environment.GetEnvironmentVariable("ORACLE_USER_ID");
    var oraclePassword = Environment.GetEnvironmentVariable("ORACLE_PASSWORD");
    
    string connectionString = "";
    
    if (!string.IsNullOrEmpty(forceMySQL) || !string.IsNullOrEmpty(mysqlHost))
    {
        // Usar MySQL (Azure)
        Log.Information("Usando Azure Database for MySQL");
        var mysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
        var mysqlDatabase = Environment.GetEnvironmentVariable("MYSQL_DATABASE");
        var mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER");
        var mysqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
        
        connectionString = $"Server={mysqlHost};Port={mysqlPort};Database={mysqlDatabase};Uid={mysqlUser};Pwd={mysqlPassword};SslMode=Required;";
        
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
    }
    else if (!string.IsNullOrEmpty(oracleDataSource) && !string.IsNullOrEmpty(oracleUserId))
    {
        // Usar Oracle (Local)
        Log.Information("Usando Oracle Database (Local)");
        connectionString = $"User Id={oracleUserId};Password={oraclePassword};Data Source={oracleDataSource};";
        
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseOracle(connectionString));
    }
    else
    {
        // Fallback para appsettings.json
        Log.Information("Usando configuração padrão do appsettings.json");
        connectionString = builder.Configuration.GetConnectionString("MySqlConnection") 
            ?? builder.Configuration.GetConnectionString("OracleConnection") ?? "";
        
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
    }

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
            Description = "API RESTful buscadora de vagas com Adzuna, desenvolvida em .NET 8 com Oracle Database. ",
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
            Description = "API RESTful buscadora de vagas com Adzuna, desenvolvida em .NET 8 com Oracle Database. - Versão 2 (Melhorada)",
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
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "❌ Erro ao aplicar migrations");
    }

    // Middleware para logging de requisições
    app.UseSerilogRequestLogging();

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
