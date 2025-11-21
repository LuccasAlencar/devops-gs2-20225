namespace dotnet_gs2_2025.Models;

public class ResumeExtraction
{
    public IReadOnlyCollection<string> Skills { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Locations { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Organizations { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> People { get; init; } = Array.Empty<string>();
}

