using System.Text.Json;
using dotnet_gs2_2025.Models.DTOs;

namespace dotnet_gs2_2025.Services;

public class AdzunaService : IAdzunaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdzunaService> _logger;
    private readonly string _appId;
    private readonly string _appKey;

    public AdzunaService(
        HttpClient httpClient, 
        IConfiguration configuration, 
        ILogger<AdzunaService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // Buscar credenciais (prioridade: .env > appsettings.json)
        _appId = Environment.GetEnvironmentVariable("ADZUNA_APP_ID") 
                 ?? _configuration["Adzuna:AppId"] 
                 ?? throw new InvalidOperationException("ADZUNA_APP_ID não configurado no .env ou appsettings.json");
        
        _appKey = Environment.GetEnvironmentVariable("ADZUNA_APP_KEY") 
                  ?? _configuration["Adzuna:AppKey"] 
                  ?? throw new InvalidOperationException("ADZUNA_APP_KEY não configurado no .env ou appsettings.json");

        _httpClient.BaseAddress = new Uri("https://api.adzuna.com/v1/api/jobs/br/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<JobSearchResponseDto> BuscarVagasAsync(JobSearchRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Buscando vagas - Cargo: {Cargo}, Localização: {Localizacao}, Página: {Pagina}", 
                request.Cargo, 
                request.Localizacao, 
                request.Pagina);

            // Construir URL com parâmetros
            var queryParams = new Dictionary<string, string>
            {
                { "app_id", _appId },
                { "app_key", _appKey },
                { "what", request.Cargo },
                { "results_per_page", request.ResultadosPorPagina.ToString() }
            };

            if (!string.IsNullOrEmpty(request.Localizacao))
            {
                queryParams.Add("where", request.Localizacao);
            }

            if (!string.IsNullOrEmpty(request.Categoria))
            {
                queryParams.Add("category", request.Categoria);
            }

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var url = $"search/{request.Pagina}?{queryString}";

            _logger.LogInformation("URL completa: {BaseUrl}{Url}", _httpClient.BaseAddress, url);
            _logger.LogDebug("APP_ID: {AppId}", _appId);

            // Fazer requisição
            var response = await _httpClient.GetAsync(url);
            
            _logger.LogInformation("Status Code: {StatusCode}", response.StatusCode);
            
            response.EnsureSuccessStatusCode();
            
            // Ler bytes diretamente para evitar problema de charset 'utf8' vs 'utf-8'
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var content = System.Text.Encoding.UTF8.GetString(bytes);
            
            _logger.LogInformation("Resposta JSON completa (primeiros 1000 chars): {Content}", content.Length > 1000 ? content.Substring(0, 1000) : content);

            // Deserializar resposta
            var result = JsonSerializer.Deserialize<JobSearchResponseDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                _logger.LogWarning("Resposta da API Adzuna retornou null");
                return new JobSearchResponseDto();
            }

            _logger.LogInformation(
                "Busca concluída com sucesso - {Count} vagas encontradas", 
                result.Results.Count);

            // Log dos primeiros resultados para debug
            if (result.Results.Count > 0)
            {
                var firstJob = result.Results[0];
                _logger.LogInformation(
                    "Primeira vaga: Title={Title}, Company={Company}, Location={Location}, RedirectUrl={Url}",
                    firstJob.Title,
                    firstJob.Company?.DisplayName,
                    firstJob.Location?.DisplayName,
                    firstJob.RedirectUrl
                );
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao fazer requisição para API Adzuna");
            throw new InvalidOperationException("Erro ao buscar vagas. Verifique as credenciais da API.", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Erro ao deserializar resposta da API Adzuna");
            throw new InvalidOperationException("Erro ao processar resposta da API de vagas.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar vagas");
            throw;
        }
    }
}
