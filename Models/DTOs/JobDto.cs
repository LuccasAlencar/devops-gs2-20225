using System.Text.Json.Serialization;

namespace dotnet_gs2_2025.Models.DTOs;

public class JobDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("company")]
    public CompanyDto? Company { get; set; }

    [JsonPropertyName("location")]
    public LocationDto? Location { get; set; }

    [JsonPropertyName("category")]
    public CategoryDto? Category { get; set; }

    [JsonPropertyName("redirect_url")]
    public string? RedirectUrl { get; set; }

    [JsonPropertyName("created")]
    public string? Created { get; set; }

    [JsonPropertyName("salary_min")]
    public double? SalaryMin { get; set; }

    [JsonPropertyName("salary_max")]
    public double? SalaryMax { get; set; }

    [JsonPropertyName("salary_is_predicted")]
    public string? SalaryIsPredicted { get; set; }

    [JsonPropertyName("contract_type")]
    public string? ContractType { get; set; }

    [JsonPropertyName("contract_time")]
    public string? ContractTime { get; set; }
}

public class CompanyDto
{
    [JsonPropertyName("__CLASS__")]
    public string? Class { get; set; }
    
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}

public class LocationDto
{
    [JsonPropertyName("__CLASS__")]
    public string? Class { get; set; }
    
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("area")]
    public List<string>? Area { get; set; }
}

public class CategoryDto
{
    [JsonPropertyName("__CLASS__")]
    public string? Class { get; set; }
    
    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }
}
