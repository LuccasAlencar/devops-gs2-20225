using dotnet_gs2_2025.Models.DTOs;

namespace dotnet_gs2_2025.Services;

public interface IAdzunaService
{
    Task<JobSearchResponseDto> BuscarVagasAsync(JobSearchRequestDto request);
}
