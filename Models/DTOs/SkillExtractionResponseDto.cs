namespace dotnet_gs2_2025.Models.DTOs;

public class SkillExtractionResponseDto
{
    public IReadOnlyCollection<string> Skills { get; init; } = Array.Empty<string>();

    public int TotalSkills { get; init; }

    public int TextLength { get; init; }

    public ResumeMetadata Metadata { get; init; } = new();

    public IEnumerable<Link> Links { get; init; } = Array.Empty<Link>();

    public IReadOnlyCollection<string> Locations { get; init; } = Array.Empty<string>();

    public string? SuggestedLocation { get; init; }
}

public class ResumeMetadata
{
    public string FileName { get; init; } = string.Empty;

    public long FileSizeBytes { get; init; }
}

