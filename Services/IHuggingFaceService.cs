using dotnet_gs2_2025.Models;

namespace dotnet_gs2_2025.Services;

public interface IHuggingFaceService
{
    Task<ResumeExtraction> ExtractResumeEntitiesAsync(string text, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates text using a Hugging Face model based on the provided prompt
    /// </summary>
    /// <param name="prompt">The prompt to generate text from</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>The generated text</returns>
    Task<string> GenerateText(string prompt, CancellationToken cancellationToken = default);
}

