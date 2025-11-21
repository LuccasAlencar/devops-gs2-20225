using Microsoft.AspNetCore.Http;
using dotnet_gs2_2025.Models.DTOs;

namespace dotnet_gs2_2025.Services;

public interface IResumeService
{
    Task<SkillExtractionResult> ExtractSkillsAsync(IFormFile resumeFile, CancellationToken cancellationToken = default);
}

