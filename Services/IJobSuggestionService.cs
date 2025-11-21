using dotnet_gs2_2025.Models.DTOs;

namespace dotnet_gs2_2025.Services;

public interface IJobSuggestionService
{
    /// <summary>
    /// Suggests job titles based on a list of skills
    /// </summary>
    /// <param name="skills">List of skills extracted from a resume</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A list of suggested job titles, sorted by relevance</returns>
    Task<List<string>> SugerirCargos(List<string> skills, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for jobs using AI-suggested job titles based on skills
    /// </summary>
    /// <param name="skills">List of skills to use for job suggestions</param>
    /// <param name="location">Optional location for job search</param>
    /// <param name="category">Optional job category</param>
    /// <param name="page">Page number for results</param>
    /// <param name="resultsPerPage">Number of results per page</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Job search results with fallback strategy if no results found</returns>
    Task<JobSearchResponseDto> BuscarVagasComSugestaoDeCargo(
        List<string> skills, 
        string? location = "brasil",
        string? category = null,
        int page = 1,
        int resultsPerPage = 20,
        CancellationToken cancellationToken = default);
}
