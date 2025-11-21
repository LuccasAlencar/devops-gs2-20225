namespace dotnet_gs2_2025.Models.DTOs;

public class SkillExtractionResult
{
    public IReadOnlyCollection<string> Skills { get; init; } = Array.Empty<string>();

    public int TotalSkills => Skills.Count;

    public int TextLength { get; init; }

    public string FileName { get; init; } = string.Empty;

    public long FileSizeBytes { get; init; }

    public IReadOnlyCollection<string> Locations { get; init; } = Array.Empty<string>();

    public string? SuggestedLocation { get; init; }
}

