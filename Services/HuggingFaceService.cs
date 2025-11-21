using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using dotnet_gs2_2025.Configuration;
using dotnet_gs2_2025.Models;
using Microsoft.Extensions.Options;

namespace dotnet_gs2_2025.Services;

public class HuggingFaceService : IHuggingFaceService
{
    private static readonly string[] AllowedShortSkills = { "C", "C#", "C++", "SQL", "BI", "UX", "UI", "Go", "AWS", ".NET", "PHP", "Java" };
    private static readonly RegexOptions RegexOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled;
    private static readonly string[] TechnicalPatterns =
    {
        // Linguagens de programação e tecnologias
        @"\b(Java|Python|JavaScript|TypeScript|C#|C\+\+|C\b|Go|Rust|Kotlin|Swift)\b",
        @"\b(React|Angular|Vue\.js?|Node\.js?|Next\.js?|Nuxt\.js?)\b",
        @"\b(Spring|Spring Boot|Django|Flask|Laravel|\.NET Core|ASP\.NET)\b",
        @"\b(MySQL|PostgreSQL|MongoDB|SQL Server|Oracle|Redis|Elasticsearch)\b",
        @"\b(AWS|Azure|Google Cloud|GCP|Docker|Kubernetes|Terraform|Ansible)\b",
        @"\b(Git|GitHub|GitLab|CI/CD|Jenkins|Agile|Scrum|Kanban)\b",
        @"\b(HTML|CSS|Sass|Less|REST|GraphQL|gRPC|SOAP|JSON|XML)\b",
        @"\b(Power BI|Tableau|Looker|Excel|ETL|Data Lake|Data Warehouse)\b",
        
        // Habilidades administrativas
        @"\b(Microsoft Office|Excel|Word|PowerPoint|Outlook)\b",
        @"\b(SAP|CRM|ERP|Gestão de Projetos)\b",
        
        // Habilidades de comunicação/interpessoais
        @"\b(Comunicação|Liderança|Negociação|Oratória|Trabalho em Equipe)\b",
        
        // Idiomas
        @"\b(Inglês|Espanhol|Francês|Alemão|Italiano|Português|Mandarim)\b",
        @"\b(Fluente|Avançado|Intermediário|Básico)\b",
        
        // Finanças e contabilidade
        @"\b(Contabilidade|Finanças|Orçamento|Controladoria|Tesouraria|Fiscal)\b",
        @"\b(Impostos|Tributos|Auditoria|Compliance|Folha de Pagamento)\b",
        
        // Vendas e marketing
        @"\b(Marketing Digital|SEO|Google Ads|Meta Ads|Redes Sociais|E-commerce)\b",
        @"\b(Vendas|Atendimento ao Cliente|Negociação|CRM|SAC)\b",
        
        // Área da saúde
        @"\b(Enfermagem|Medicina|Fisioterapia|Nutrição|Farmácia)\b",
        
        // Indústria e manufatura
        @"\b(Lean|Six Sigma|Gestão de Qualidade|ISO|5S|Kaizen|Manufatura)\b",
        
        // Gastronomia e Culinária
        @"\b(Gastronomia|Culinária|Chef|Cozinha|Cozinheiro|Confeitaria|Panificação)\b",
        @"\b(Culinária Italiana|Culinária Francesa|Culinária Brasileira|Cozinha Contemporânea)\b",
        @"\b(Massas|Risotos|Molhos|Sous-vide|Fermentação|Churrascaria)\b",
        @"\b(Kitchen Brigade|Mise en Place|HACCP|Segurança Alimentar)\b",
        @"\b(Desenvolvimento de Cardápios|Gestão de Estoque|Seleção de Ingredientes)\b",
        @"\b(Técnicas de Corte|Brunoise|Julienne|Chiffonade|Cozinha Vegetariana|Vegana)\b"
    };

    private readonly HttpClient _httpClient;
    private readonly HuggingFaceOptions _options;
    private readonly ILogger<HuggingFaceService> _logger;

    public HuggingFaceService(HttpClient httpClient, IOptions<HuggingFaceOptions> options, ILogger<HuggingFaceService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!string.IsNullOrWhiteSpace(_options.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        }

        if (_httpClient.DefaultRequestHeaders.Accept.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    public async Task<ResumeExtraction> ExtractResumeEntitiesAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ResumeExtraction();
        }

        var regexSkills = ExtractSkillsWithRegex(text);

        var locationTask = ExtractLocationsAsync(text, cancellationToken);
        var generativeTask = ExtractSkillsWithGenerativeModelAsync(text, cancellationToken);

        await Task.WhenAll(locationTask, generativeTask);

        var combinedSkills = regexSkills
            .Concat(generativeTask.Result)
            .Select(NormalizeSkill)
            .Where(IsRelevantSkill)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(skill => skill.Count(char.IsLetter))
            .ThenBy(skill => skill)
            .ToArray();

        var locations = locationTask.Result
            .Select(NormalizeLocation)
            .Where(location => !string.IsNullOrWhiteSpace(location))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(location => location.Length)
            .ToArray();

        _logger.LogInformation("Pipeline híbrido processou {SkillCount} habilidades e {LocationCount} localizações", combinedSkills.Length, locations.Length);

        return new ResumeExtraction
        {
            Skills = combinedSkills,
            Locations = locations
        };
    }

    private IReadOnlyCollection<string> ExtractSkillsWithRegex(string text)
    {
        var matches = new List<string>();

        foreach (var pattern in TechnicalPatterns)
        {
            foreach (Match match in Regex.Matches(text, pattern, RegexOptions))
            {
                if (match.Success)
                {
                    matches.Add(match.Value);
                }
            }
        }

        return matches;
    }

    private async Task<IReadOnlyCollection<string>> ExtractLocationsAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.LocationsModel))
        {
            return Array.Empty<string>();
        }

        try
        {
            var endpoint = $"models/{Uri.EscapeDataString(_options.LocationsModel)}";

            var payload = new
            {
                inputs = text,
                options = new { wait_for_model = true }
            };

            using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Hugging Face NER (locais) retornou {Status}: {Error}", response.StatusCode, error);
                return Array.Empty<string>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var tokens = await ParseEntitiesAsync(stream, cancellationToken);

            return MergeEntities(tokens, text)
                .Where(entity => entity != null && entity.Score >= _options.MinScore && IsLocation(entity))
                .Select(entity => entity.Text)
                .ToArray();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Falha ao extrair localizações com modelo {Model}", _options.LocationsModel);
            return Array.Empty<string>();
        }
    }

    private async Task<IReadOnlyCollection<string>> ExtractSkillsWithGenerativeModelAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SkillsModel))
        {
            return Array.Empty<string>();
        }

        try
        {
            var truncated = text.Length > 1500 ? text[..1500] : text;

            var prompt = $"""
            Analise o currículo abaixo e extraia todas as habilidades profissionais RELEVANTES, incluindo:
            - Habilidades técnicas específicas da área (ex: Culinária Italiana, Gestão de Kitchen Brigade, HACCP para gastronomia; Java, React para TI; Contabilidade, Fiscal para finanças)
            - Competências interpessoais importantes (comunicação, liderança, trabalho em equipe)
            - Conhecimentos específicos da área profissional (gastronomia, marketing, finanças, recursos humanos, vendas, saúde, TI, engenharia, etc)
            - Ferramentas e metodologias específicas da profissão
            
            IMPORTANTE:
            - NÃO extraia níveis de idioma isolados (ex: "Intermediário", "Avançado", "Fluente", "Básico") sem o idioma
            - Extraia idiomas como "Inglês Avançado", "Italiano Fluente", etc.
            - Priorize habilidades técnicas e específicas da área sobre habilidades genéricas
            - Para gastronomia: extraia técnicas culinárias, tipos de culinária, gestão de cozinha, etc.
            - Para TI: extraia linguagens, frameworks, ferramentas técnicas
            - Para outras áreas: extraia conhecimentos específicos da profissão
            
            Responda apenas com uma lista separada por vírgulas, sem explicações.

            Currículo:
            {truncated}

            Exemplos de resposta:
            - Para gastronomia: Culinária Italiana, Gestão de Kitchen Brigade, HACCP, Sous-vide, Desenvolvimento de Cardápios, Liderança de Equipes
            - Para TI: Java, Spring Boot, React, PostgreSQL, AWS, Git
            - Para finanças: Contabilidade, Fiscal, Excel Avançado, Orçamento, Controle Financeiro
            """;

            var payload = new
            {
                inputs = prompt,
                parameters = new { max_new_tokens = 120 },
                options = new { wait_for_model = true }
            };

            var endpoint = $"models/{Uri.EscapeDataString(_options.SkillsModel)}";
            using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Hugging Face generativo (skills) retornou {Status}: {Error}", response.StatusCode, error);
                return Array.Empty<string>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var skills = new List<string>();

            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("generated_text", out var generatedProp))
                    {
                        var generated = generatedProp.GetString();
                        skills.AddRange(ParseSkillList(generated));
                    }
                }
            }
            else if (document.RootElement.TryGetProperty("generated_text", out var generatedTextProp))
            {
                skills.AddRange(ParseSkillList(generatedTextProp.GetString()));
            }

            return skills;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Falha ao extrair habilidades com modelo generativo {Model}", _options.SkillsModel);
            return Array.Empty<string>();
        }
    }

    private static async Task<List<HuggingFaceEntity>> ParseEntitiesAsync(Stream responseStream, CancellationToken cancellationToken)
    {
        var entities = new List<HuggingFaceEntity>();

        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            return entities;
        }

        foreach (var element in root.EnumerateArray())
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var nested in element.EnumerateArray())
                {
                    if (TryReadEntity(nested, out var entity))
                    {
                        entities.Add(entity);
                    }
                }
            }
            else if (TryReadEntity(element, out var entity))
            {
                entities.Add(entity);
            }
        }

        return entities;
    }

    private static bool TryReadEntity(JsonElement element, out HuggingFaceEntity entity)
    {
        entity = null!;

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var result = new HuggingFaceEntity
        {
            EntityGroup = element.TryGetProperty("entity_group", out var groupProp)
                ? groupProp.GetString()
                : element.TryGetProperty("entity", out var entityProp)
                    ? entityProp.GetString()
                    : element.TryGetProperty("label", out var labelProp)
                        ? labelProp.GetString()
                        : null,
            Score = element.TryGetProperty("score", out var scoreProp) ? scoreProp.GetDouble() : 0d,
            Word = element.TryGetProperty("word", out var wordProp) ? wordProp.GetString() : null,
            Start = element.TryGetProperty("start", out var startProp) ? startProp.GetInt32() : 0,
            End = element.TryGetProperty("end", out var endProp) ? endProp.GetInt32() : 0
        };

        if (!string.IsNullOrWhiteSpace(result.EntityGroup))
        {
            result.EntityGroup = NormalizeGroupName(result.EntityGroup);
        }

        entity = result;
        return true;
    }

    private static IReadOnlyCollection<NormalizedEntity> MergeEntities(IEnumerable<HuggingFaceEntity> tokens, string originalText)
    {
        var sorted = tokens
            .Where(t => t != null && !string.IsNullOrWhiteSpace(t.EntityGroup))
            .OrderBy(t => t.Start)
            .ThenByDescending(t => t.End)
            .ToList();

        if (sorted.Count == 0)
        {
            return Array.Empty<NormalizedEntity>();
        }

        var result = new List<NormalizedEntity>();

        foreach (var token in sorted)
        {
            if (result.Count > 0 && CanMerge(result[^1], token))
            {
                var last = result[^1];
                last.Score = Math.Max(last.Score, token.Score);
                last.End = Math.Max(last.End, token.End);
                last.Text = SliceText(originalText, last.Start, last.End);
            }
            else
            {
                var text = SliceText(originalText, token.Start, token.End);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var entity = new NormalizedEntity
                    {
                        Group = token.EntityGroup ?? string.Empty,
                        Start = token.Start,
                        End = token.End,
                        Score = token.Score,
                        Text = text
                    };
                    result.Add(entity);
                }
            }
        }

        return result;
    }

    private static bool CanMerge(NormalizedEntity previous, HuggingFaceEntity current)
    {
        if (!string.Equals(previous.Group, current.EntityGroup, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return current.Start <= previous.End + 1;
    }

    private static string SliceText(string original, int start, int end)
    {
        try
        {
            if (start < 0 || end <= start || start >= original.Length)
            {
                return string.Empty;
            }

            var safeEnd = Math.Min(end, original.Length);
            var span = original[start..safeEnd];

            var normalized = Regex.Replace(span, @"\s+", " ").Trim();
            normalized = normalized.Replace("##", string.Empty, StringComparison.Ordinal);

            return normalized;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsRelevantSkill(string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
        {
            return false;
        }

        var trimmed = skill.Trim();

        if (trimmed.Length < 2)
        {
            return false;
        }

        // Remove níveis de idioma isolados (sem o nome do idioma)
        var niveisIdioma = new[] { "fluente", "avançado", "intermediário", "básico", "nativo", "intermediario" };
        if (niveisIdioma.Contains(trimmed.ToLowerInvariant()))
        {
            return false;
        }

        if (trimmed.Length <= 3 && !AllowedShortSkills.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            return HasVowel(trimmed) && trimmed.Length >= 3;
        }

        if (trimmed.StartsWith("Faculdade", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > 6)
        {
            return false;
        }

        return true;
    }

    private static bool HasVowel(string value) =>
        value.Any(ch => "aeiouáéíóúâêôãõ".Contains(char.ToLowerInvariant(ch)));

    private static string NormalizeGroupName(string rawGroup)
    {
        if (string.IsNullOrWhiteSpace(rawGroup))
        {
            return string.Empty;
        }

        if (rawGroup.Length > 2 && rawGroup[1] == '-')
        {
            rawGroup = rawGroup[2..];
        }

        return rawGroup.Trim().ToUpperInvariant();
    }

    private static IEnumerable<string> ParseSkillList(string? generated)
    {
        if (string.IsNullOrWhiteSpace(generated))
        {
            return Array.Empty<string>();
        }

        // Remove prompt echoes
        var cleaned = generated.Split('\n')
            .LastOrDefault(segment => segment.Contains(','))
            ?? generated;

        return cleaned
            .Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeSkill);
    }

    private class NormalizedEntity
    {
        public string Group { get; set; } = string.Empty;

        public int Start { get; set; }

        public int End { get; set; }

        public double Score { get; set; }

        public string Text { get; set; } = string.Empty;
    }

    private static string NormalizeSkill(string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
        {
            return string.Empty;
        }

        var trimmed = skill.Trim();
        trimmed = Regex.Replace(trimmed, @"^\W+|\W+$", string.Empty);

        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
    }

    private static string NormalizeLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return string.Empty;
        }

        var trimmed = location.Trim();
        trimmed = trimmed.Replace("##", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
    }

    private static bool IsLocation(NormalizedEntity? entity)
    {
        if (entity is null || string.IsNullOrWhiteSpace(entity.Group))
        {
            return false;
        }

        return entity.Group.Equals("LOC", StringComparison.OrdinalIgnoreCase)
            || entity.Group.Equals("GPE", StringComparison.OrdinalIgnoreCase)
            || entity.Group.Equals("LOCATION", StringComparison.OrdinalIgnoreCase);
    }
    
    public async Task<string> GenerateText(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return string.Empty;
        }
        
        try
        {
            // Use the same model as for skills extraction or a specific text generation model
            var modelName = _options.SkillsModel;
            
            if (string.IsNullOrWhiteSpace(modelName))
            {
                _logger.LogWarning("Não foi possível gerar texto: nenhum modelo configurado");
                return string.Empty;
            }
            
            var payload = new
            {
                inputs = prompt,
                parameters = new { max_new_tokens = 150, temperature = 0.7 },
                options = new { wait_for_model = true }
            };
            
            var endpoint = $"models/{Uri.EscapeDataString(modelName)}";
            using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Hugging Face text generation retornou {Status}: {Error}", response.StatusCode, error);
                return string.Empty;
            }
            
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("generated_text", out var generatedProp))
                    {
                        var text = generatedProp.GetString() ?? string.Empty;
                        return text.Trim();
                    }
                }
            }
            else if (document.RootElement.TryGetProperty("generated_text", out var generatedTextProp))
            {
                var text = generatedTextProp.GetString() ?? string.Empty;
                return text.Trim();
            }
            
            return string.Empty;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Falha ao gerar texto com modelo {Model}", _options.SkillsModel);
            return string.Empty;
        }
    }
}

