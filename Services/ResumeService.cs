using dotnet_gs2_2025.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace dotnet_gs2_2025.Services;

public class ResumeService : IResumeService
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IHuggingFaceService _huggingFaceService;
    private readonly ILogger<ResumeService> _logger;

    public ResumeService(IPdfTextExtractor pdfTextExtractor, IHuggingFaceService huggingFaceService, ILogger<ResumeService> logger)
    {
        _pdfTextExtractor = pdfTextExtractor ?? throw new ArgumentNullException(nameof(pdfTextExtractor));
        _huggingFaceService = huggingFaceService ?? throw new ArgumentNullException(nameof(huggingFaceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SkillExtractionResult> ExtractSkillsAsync(IFormFile resumeFile, CancellationToken cancellationToken = default)
    {
        if (resumeFile == null)
        {
            throw new ArgumentNullException(nameof(resumeFile));
        }

        if (resumeFile.Length == 0)
        {
            throw new InvalidOperationException("Arquivo de currículo está vazio.");
        }

        _logger.LogInformation("Iniciando extração de habilidades do currículo {FileName} ({FileSize} bytes)", resumeFile.FileName, resumeFile.Length);

        await using var memoryStream = new MemoryStream();
        await resumeFile.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var text = await _pdfTextExtractor.ExtractTextAsync(memoryStream, cancellationToken);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Não foi possível extrair texto do currículo {FileName}", resumeFile.FileName);
            return new SkillExtractionResult
            {
                FileName = resumeFile.FileName,
                FileSizeBytes = resumeFile.Length,
                TextLength = 0,
                Skills = Array.Empty<string>(),
                Locations = Array.Empty<string>(),
                SuggestedLocation = null
            };
        }

        var extraction = await _huggingFaceService.ExtractResumeEntitiesAsync(text, cancellationToken);

        var result = new SkillExtractionResult
        {
            FileName = resumeFile.FileName,
            FileSizeBytes = resumeFile.Length,
            TextLength = text.Length,
            Skills = extraction.Skills,
            Locations = extraction.Locations,
            SuggestedLocation = extraction.Locations.FirstOrDefault()
        };

        _logger.LogInformation("Extração concluída com {SkillCount} habilidades para o currículo {FileName}", result.TotalSkills, resumeFile.FileName);

        return result;
    }
}

