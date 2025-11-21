using Asp.Versioning;
using dotnet_gs2_2025.Models.DTOs;
using dotnet_gs2_2025.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace dotnet_gs2_2025.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class ResumesController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/x-pdf"
    };

    private readonly IResumeService _resumeService;
    private readonly ILogger<ResumesController> _logger;

    public ResumesController(IResumeService resumeService, ILogger<ResumesController> logger)
    {
        _resumeService = resumeService ?? throw new ArgumentNullException(nameof(resumeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Extrai habilidades de um currículo PDF utilizando IA (Hugging Face).
    /// </summary>
    /// <param name="request">Arquivo PDF contendo o currículo.</param>
    /// <returns>Lista de habilidades identificadas e metadados do arquivo.</returns>
    [HttpPost("skills")]
    [ProducesResponseType(typeof(SkillExtractionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
    public async Task<ActionResult<SkillExtractionResponseDto>> ExtractSkills([FromForm] ResumeUploadRequestDto request, CancellationToken cancellationToken)
    {
        if (request?.File == null)
        {
            _logger.LogWarning("Requisição inválida: arquivo de currículo não fornecido.");
            return BadRequest(new { message = "Arquivo de currículo é obrigatório." });
        }

        if (!IsPdf(request.File))
        {
            _logger.LogWarning("Tipo de conteúdo não suportado: {ContentType}", request.File.ContentType);
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, new { message = "Somente arquivos PDF são suportados." });
        }

        if (request.File.Length > 5 * 1024 * 1024)
        {
            _logger.LogWarning("Arquivo excede o limite permitido: {FileSize} bytes", request.File.Length);
            return BadRequest(new { message = "Arquivo de currículo excede o limite de 5MB." });
        }

        try
        {
            var extractionResult = await _resumeService.ExtractSkillsAsync(request.File, cancellationToken);

            var response = new SkillExtractionResponseDto
            {
                Skills = extractionResult.Skills,
                TotalSkills = extractionResult.TotalSkills,
                TextLength = extractionResult.TextLength,
                Locations = extractionResult.Locations,
                SuggestedLocation = extractionResult.SuggestedLocation,
                Metadata = new ResumeMetadata
                {
                    FileName = extractionResult.FileName,
                    FileSizeBytes = extractionResult.FileSizeBytes
                },
                Links = BuildLinks()
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Falha de validação ao processar currículo.");
            return BadRequest(new { message = ex.Message });
        }
    }

    private static bool IsPdf(IFormFile file)
    {
        if (file == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(file.ContentType) && AllowedContentTypes.Contains(file.ContentType))
        {
            return true;
        }

        return Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<Link> BuildLinks()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v1/resumes/skills";

        return new[]
        {
            new Link
            {
                Href = baseUrl,
                Rel = "self",
                Method = "POST"
            },
            new Link
            {
                Href = $"{Request.Scheme}://{Request.Host}/api/v1/jobs/search",
                Rel = "jobs-search",
                Method = "POST"
            }
        };
    }
}

