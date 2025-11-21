using System.ComponentModel.DataAnnotations;

namespace dotnet_gs2_2025.Models.DTOs;

public class JobSearchRequestDto
{
    /// <summary>
    /// Cargo ou palavra-chave para buscar (ex: "desenvolvedor", "java", "mobile")
    /// </summary>
    [Required(ErrorMessage = "Cargo é obrigatório")]
    public string Cargo { get; set; } = string.Empty;

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
