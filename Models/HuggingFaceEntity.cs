using System.Text.Json.Serialization;

namespace dotnet_gs2_2025.Models;

public class HuggingFaceEntity
{
    [JsonPropertyName("entity_group")]
    public string? EntityGroup { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }

    [JsonPropertyName("word")]
    public string? Word { get; set; }

    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }
}

