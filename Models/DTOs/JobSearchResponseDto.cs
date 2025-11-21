using System.Text.Json.Serialization;

namespace dotnet_gs2_2025.Models.DTOs;

public class JobSearchResponseDto
{
    [JsonPropertyName("__CLASS__")]
    public string? Class { get; set; }
    
    [JsonPropertyName("results")]
    public List<JobDto> Results { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("mean")]
    public decimal Mean { get; set; }
}
