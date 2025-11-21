using System.ComponentModel.DataAnnotations;

namespace dotnet_gs2_2025.Models.DTOs;

public class SkillsJobSearchRequestDto
{
    /// <summary>
    /// Lista de habilidades para buscar vagas
    /// </summary>
    [Required(ErrorMessage = "Ao menos uma habilidade é necessária")]
    public List<string> Habilidades { get; set; } = new();

    /// <summary>
    /// Localização da vaga (ex: "são paulo", "brasil", "rio de janeiro")
    /// </summary>
    public string? Localizacao { get; set; } = "brasil";

    /// <summary>
    /// Categoria da vaga (ex: "it-jobs", "engineering-jobs")
    /// </summary>
    public string? Categoria { get; set; }

    /// <summary>
    /// Página dos resultados (padrão: 1)
    /// </summary>
    public int Pagina { get; set; } = 1;

    /// <summary>
    /// Quantidade de resultados por página (padrão: 20, máx: 50)
    /// </summary>
    public int ResultadosPorPagina { get; set; } = 20;
}
