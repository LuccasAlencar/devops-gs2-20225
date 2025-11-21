using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using dotnet_gs2_2025.Models.DTOs;
using dotnet_gs2_2025.Services;

namespace dotnet_gs2_2025.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class JobsController : ControllerBase
{
    private readonly IAdzunaService _adzunaService;
    private readonly IJobSuggestionService _jobSuggestionService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IAdzunaService adzunaService, IJobSuggestionService jobSuggestionService, ILogger<JobsController> logger)
    {
        _adzunaService = adzunaService;
        _jobSuggestionService = jobSuggestionService;
        _logger = logger;
    }

    /// <summary>
    /// Busca vagas de emprego usando a API do Adzuna
    /// </summary>
    /// <param name="request">Parâmetros de busca (cargo, localização, categoria)</param>
    /// <returns>Lista de vagas encontradas</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(JobSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobSearchResponseDto>> SearchJobs([FromBody] JobSearchRequestDto request)
    {
        _logger.LogInformation("POST /api/v1/jobs/search - Cargo: {Cargo}", request.Cargo);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Requisição inválida para busca de vagas");
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _adzunaService.BuscarVagasAsync(request);

            if (result.Results.Count == 0)
            {
                _logger.LogInformation("Nenhuma vaga encontrada para os critérios: {Cargo}", request.Cargo);
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Erro ao buscar vagas");
            return StatusCode(500, new 
            { 
                message = ex.Message,
                error = "Erro ao buscar vagas. Verifique as configurações da API Adzuna."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar vagas");
            return StatusCode(500, new 
            { 
                message = "Erro interno do servidor",
                error = "Ocorreu um erro inesperado ao processar sua solicitação."
            });
        }
    }

    /// <summary>
    /// Busca vagas via GET com query parameters (alternativa mais simples)
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(JobSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JobSearchResponseDto>> SearchJobsGet(
        [FromQuery] string cargo,
        [FromQuery] string? localizacao = "brasil",
        [FromQuery] string? categoria = "it-jobs",
        [FromQuery] int pagina = 1,
        [FromQuery] int resultadosPorPagina = 20)
    {
        _logger.LogInformation("GET /api/v1/jobs/search - Cargo: {Cargo}", cargo);

        if (string.IsNullOrWhiteSpace(cargo))
        {
            return BadRequest(new { message = "Cargo é obrigatório" });
        }

        var request = new JobSearchRequestDto
        {
            Cargo = cargo,
            Localizacao = localizacao,
            Categoria = categoria,
            Pagina = pagina,
            ResultadosPorPagina = resultadosPorPagina
        };

        try
        {
            var result = await _adzunaService.BuscarVagasAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar vagas");
            return StatusCode(500, new { message = "Erro ao buscar vagas" });
        }
    }
    
    /// <summary>
    /// Busca vagas de emprego com base nas habilidades, usando IA para sugerir cargos relevantes
    /// </summary>
    /// <param name="request">Parâmetros de busca com habilidades</param>
    /// <returns>Lista de vagas encontradas, usando cargos sugeridos pela IA</returns>
    [HttpPost("search/skills")]
    [ProducesResponseType(typeof(JobSearchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JobSearchResponseDto>> SearchJobsWithSkills([FromBody] SkillsJobSearchRequestDto request)
    {
        _logger.LogInformation("POST /api/v1/jobs/search/skills - Habilidades: {Habilidades}", 
            request.Habilidades != null ? string.Join(", ", request.Habilidades) : "(nenhuma)");

        if (!ModelState.IsValid || request.Habilidades == null || !request.Habilidades.Any())
        {
            _logger.LogWarning("Requisição inválida para busca de vagas por habilidades");
            return BadRequest(new { message = "É necessário fornecer ao menos uma habilidade" });
        }

        try
        {
            var result = await _jobSuggestionService.BuscarVagasComSugestaoDeCargo(
                request.Habilidades,
                request.Localizacao,
                request.Categoria,
                request.Pagina,
                request.ResultadosPorPagina);

            if (result.Results.Count == 0)
            {
                _logger.LogInformation("Nenhuma vaga encontrada para as habilidades fornecidas");
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Erro ao buscar vagas por habilidades");
            return StatusCode(500, new 
            { 
                message = ex.Message,
                error = "Erro ao buscar vagas. Verifique as configurações da API Adzuna."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao buscar vagas por habilidades");
            return StatusCode(500, new 
            { 
                message = "Erro interno do servidor",
                error = "Ocorreu um erro inesperado ao processar sua solicitação."
            });
        }
    }
    
    /// <summary>
    /// Endpoint para testar sugestões de cargo com base em habilidades, sem buscar vagas
    /// </summary>
    /// <param name="habilidades">Lista de habilidades para gerar sugestões de cargo</param>
    /// <returns>Lista de cargos sugeridos com base nas habilidades</returns>
    [HttpPost("suggest-jobs")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<string>>> SuggestJobs([FromBody] List<string> habilidades)
    {
        if (habilidades == null || !habilidades.Any())
        {
            return BadRequest(new { message = "É necessário fornecer ao menos uma habilidade" });
        }

        try
        {
            var sugestoes = await _jobSuggestionService.SugerirCargos(habilidades);
            return Ok(sugestoes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sugerir cargos para habilidades");
            return StatusCode(500, new { message = "Erro ao gerar sugestões de cargo" });
        }
    }
}
