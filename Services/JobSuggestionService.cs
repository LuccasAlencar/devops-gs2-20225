using dotnet_gs2_2025.Models.DTOs;

namespace dotnet_gs2_2025.Services;

public class JobSuggestionService : IJobSuggestionService
{
    private readonly IHuggingFaceService _huggingFaceService;
    private readonly IAdzunaService _adzunaService;
    private readonly ILogger<JobSuggestionService> _logger;

    public JobSuggestionService(
        IHuggingFaceService huggingFaceService,
        IAdzunaService adzunaService,
        ILogger<JobSuggestionService> logger)
    {
        _huggingFaceService = huggingFaceService ?? throw new ArgumentNullException(nameof(huggingFaceService));
        _adzunaService = adzunaService ?? throw new ArgumentNullException(nameof(adzunaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<List<string>> SugerirCargos(List<string> skills, CancellationToken cancellationToken = default)
    {
        // Se não há habilidades, retorna vazio
        if (skills == null || !skills.Any())
        {
            _logger.LogWarning("Tentativa de sugerir cargos sem fornecer habilidades");
            return new List<string>();
        }

        try
        {
            // Identificação de possíveis cargos baseados em combinações de habilidades comuns
            // Alguns mapeamentos diretos para casos mais comuns
            var skillsLower = skills.Select(s => s.ToLowerInvariant()).ToList();
            
            // Verifica se há combinações conhecidas de skills
            // IMPORTANTE: Verificar áreas específicas ANTES de TI para evitar falsos positivos
            
            // Gastronomia e Culinária (verificar primeiro para não confundir com TI)
            if (skillsLower.Any(s => s.Contains("culinária") || s.Contains("gastronomia") || s.Contains("chef") || 
                                   s.Contains("cozinha") || s.Contains("haccp") || s.Contains("kitchen brigade") ||
                                   s.Contains("sous-vide") || s.Contains("massas") || s.Contains("risotos") ||
                                   s.Contains("confeitaria") || s.Contains("panificação")))
            {
                if (skillsLower.Any(s => s.Contains("executivo") || s.Contains("chef executivo")))
                {
                    return new List<string> { "Chef Executivo" };
                }
                if (skillsLower.Any(s => s.Contains("sous chef") || s.Contains("subchef")))
                {
                    return new List<string> { "Sous Chef" };
                }
                return new List<string> { "Chef de Cozinha" };
            }
            
            // Área de TI
            if (ContainsAll(skillsLower, new[] { "java", "spring" }) || 
                ContainsAll(skillsLower, new[] { "java", "spring boot" }))
            {
                return new List<string> { "Desenvolvedor Java Backend" };
            }
            
            if (ContainsAll(skillsLower, new[] { "python", "django" }) ||
                ContainsAll(skillsLower, new[] { "python", "flask" }))
            {
                return new List<string> { "Desenvolvedor Python Backend" };
            }
            
            if (ContainsAll(skillsLower, new[] { "javascript", "react" }) ||
                ContainsAll(skillsLower, new[] { "typescript", "react" }))
            {
                return new List<string> { "Desenvolvedor Frontend React" };
            }
            
            if (ContainsAll(skillsLower, new[] { "angular" }) ||
                ContainsAll(skillsLower, new[] { "typescript", "angular" }))
            {
                return new List<string> { "Desenvolvedor Frontend Angular" };
            }
            
            if (ContainsAll(skillsLower, new[] { "node", "express" }) ||
                ContainsAll(skillsLower, new[] { "node.js" }))
            {
                return new List<string> { "Desenvolvedor Node.js" };
            }
            
            if (ContainsAll(skillsLower, new[] { "aws", "cloud" }) ||
                ContainsAll(skillsLower, new[] { "devops", "kubernetes" }) ||
                ContainsAll(skillsLower, new[] { "docker", "ci/cd" }))
            {
                return new List<string> { "Engenheiro DevOps" };
            }
            
            // Finanças e Contabilidade
            if (ContainsAll(skillsLower, new[] { "contabilidade", "fiscal" }) ||
                ContainsAll(skillsLower, new[] { "contabilidade", "balanço" }))
            {
                return new List<string> { "Contador" };
            }
            
            if (ContainsAll(skillsLower, new[] { "finanças", "investimentos" }) ||
                ContainsAll(skillsLower, new[] { "financeiro", "controle" }))
            {
                return new List<string> { "Analista Financeiro" };
            }
            
            // Recursos Humanos
            if (ContainsAll(skillsLower, new[] { "recrutamento", "seleção" }) ||
                ContainsAll(skillsLower, new[] { "recursos humanos", "gestão de pessoas" }))
            {
                return new List<string> { "Analista de Recursos Humanos" };
            }
            
            // Marketing e Vendas
            if (ContainsAll(skillsLower, new[] { "marketing", "redes sociais" }) ||
                ContainsAll(skillsLower, new[] { "marketing digital", "campanhas" }))
            {
                return new List<string> { "Analista de Marketing Digital" };
            }
            
            if (ContainsAll(skillsLower, new[] { "vendas", "negociação" }) ||
                ContainsAll(skillsLower, new[] { "comercial", "prospecção" }))
            {
                return new List<string> { "Executivo de Vendas" };
            }
            
            // Saúde
            if (ContainsAll(skillsLower, new[] { "enfermagem", "hospitalar" }))
            {
                return new List<string> { "Enfermeiro" };
            }
            
            if (ContainsAll(skillsLower, new[] { "nutrição", "dietética" }))
            {
                return new List<string> { "Nutricionista" };
            }
            
            // Administrativo
            if (ContainsAll(skillsLower, new[] { "administrativo", "secretariado" }) ||
                ContainsAll(skillsLower, new[] { "atendimento", "arquivo" }))
            {
                return new List<string> { "Assistente Administrativo" };
            }
            
            // Educação
            if (ContainsAll(skillsLower, new[] { "professor", "ensino" }) ||
                ContainsAll(skillsLower, new[] { "educação", "aula" }))
            {
                return new List<string> { "Professor" };
            }
            
            // Limite a quantidade de habilidades para não exceder token limit
            var habilidadesTexto = string.Join(", ", skills.Take(10));
            
            // Criar um prompt mais específico para o modelo de IA sugerir cargos
            var prompt = $@"
Com base nestas habilidades profissionais: {habilidadesTexto}

Sugira APENAS UM cargo específico e real do mercado de trabalho brasileiro. 
Considere TODAS as áreas: gastronomia/culinária, TI, saúde, finanças, marketing, recursos humanos, vendas, jurídico, educação, engenharia, administração, etc.

IMPORTANTE: 
- Retorne APENAS o nome do cargo, sem explicações, sem pontos, sem vírgulas extras
- Use nomes de cargos comuns no Brasil
- NÃO retorne habilidades, retorne o CARGO/OCUPAÇÃO
- Analise o CONTEXTO das habilidades para determinar a área correta

REGRAS ESPECÍFICAS:
- Se houver habilidades de culinária/gastronomia (ex: Culinária Italiana, HACCP, Kitchen Brigade, Sous-vide): sugira cargo de gastronomia (Chef, Cozinheiro, etc)
- Se houver linguagens de programação (Java, Python, JavaScript): sugira cargo de TI
- Se houver habilidades de dados/analytics SEM linguagens de programação: NÃO sugira ""Cientista de Dados""
- Se houver apenas habilidades genéricas (Liderança, Orçamento, Idiomas): analise o contexto geral

Exemplos de resposta correta:
- Para habilidades de culinária: Chef de Cozinha, Chef Executivo, Sous Chef
- Para TI: Desenvolvedor Java, Analista de Sistemas, Engenheiro de Software
- Para finanças: Contador, Analista Financeiro
- Para saúde: Enfermeiro, Nutricionista
- Para marketing: Analista de Marketing, Gerente de Marketing Digital

Habilidades fornecidas: {habilidadesTexto}
Cargo sugerido:
";

            _logger.LogInformation("Solicitando sugestão de cargo para habilidades: {Habilidades}", habilidadesTexto);
            
            // Tenta usar o modelo de AI para sugerir cargos
            var cargoSugerido = await _huggingFaceService.GenerateText(prompt, cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(cargoSugerido))
            {
                // Limpar o texto gerado - remover explicações, pontos, vírgulas extras
                cargoSugerido = LimparCargoSugerido(cargoSugerido);
                
                if (cargoSugerido.Length > 3 && cargoSugerido.Length < 100) // Validar tamanho razoável
                {
                    _logger.LogInformation("Cargo sugerido pela AI: {Cargo}", cargoSugerido);
                    return new List<string> { cargoSugerido };
                }
                else
                {
                    _logger.LogWarning("Cargo sugerido pela AI parece inválido (tamanho: {Tamanho}): {Cargo}", 
                        cargoSugerido.Length, cargoSugerido);
                }
            }
            
            // Se o modelo não conseguiu sugerir, use heurística baseada em palavras-chave
            _logger.LogWarning("AI não conseguiu sugerir cargo, usando heurística");
            
            // Tenta determinar a área principal
            var area = DeterminarAreaPrincipal(skillsLower);
            if (!string.IsNullOrEmpty(area))
            {
                return new List<string> { area };
            }
            
            // Último caso: usa a primeira habilidade mais o termo "profissional"
            var primeiraHabilidade = skills.FirstOrDefault() ?? "";
            if (!string.IsNullOrWhiteSpace(primeiraHabilidade))
            {
                return new List<string> { primeiraHabilidade };
            }
            
            // Caso extremo: retorna vazio para não buscar com termo genérico
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao sugerir cargos com base em habilidades");
            
            // Fallback para a primeira habilidade em caso de erro
            if (skills.Any())
            {
                return new List<string> { skills.First() };
            }
            
            return new List<string>();
        }
    }
    
    private bool ContainsAll(List<string> skills, string[] keywords)
    {
        return keywords.All(keyword => skills.Any(skill => skill.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }
    
    private string DeterminarAreaPrincipal(List<string> skills)
    {
        // Define mapeamentos de habilidades para áreas
        // IMPORTANTE: Ordem importa - áreas específicas primeiro para evitar falsos positivos
        var areaMappings = new Dictionary<string, string[]>
        {
            // Gastronomia (verificar ANTES de TI para evitar confusão)
            { "Chef de Cozinha", new[] { "culinária", "gastronomia", "chef", "cozinha", "haccp", "kitchen brigade", "sous-vide", "massas", "risotos", "confeitaria", "panificação", "culinária italiana", "culinária francesa", "desenvolvimento de cardápios", "gestão de estoque", "seleção de ingredientes" } },
            
            // Área de TI (verificar apenas se NÃO há indicadores de outras áreas)
            { "Desenvolvedor Frontend", new[] { "javascript", "html", "css", "react", "vue", "angular", "frontend", "ui", "ux" } },
            { "Desenvolvedor Backend", new[] { "java", "c#", ".net", "python", "nodejs", "php", "backend", "api", "rest" } },
            { "Cientista de Dados", new[] { "python", "r", "machine learning", "ml", "ai", "data science", "analytics", "estatística", "big data", "pandas", "numpy", "tensorflow", "pytorch" } },
            { "DBA", new[] { "sql", "oracle", "mysql", "postgresql", "banco de dados", "database", "dba" } },
            { "DevOps", new[] { "aws", "azure", "gcp", "docker", "kubernetes", "ci/cd", "jenkins", "devops", "terraform" } },
            { "QA", new[] { "teste", "testing", "automation", "selenium", "qa", "qualidade" } },
            
            // Área de Negócios
            { "Analista Financeiro", new[] { "finanças", "contabilidade", "fluxo de caixa", "investimentos", "orçamento", "excel", "fiscal", "tributário", "controle financeiro" } },
            { "Profissional de Marketing", new[] { "marketing", "publicidade", "redes sociais", "seo", "google ads", "facebook ads", "e-commerce", "campanhas", "branding", "estratégia de marca" } },
            { "Analista de Recursos Humanos", new[] { "recursos humanos", "recrutamento", "seleção", "dp", "departamento pessoal", "gestão de pessoas", "treinamento", "folha de pagamento", "benefícios" } },
            { "Profissional de Vendas", new[] { "vendas", "comercial", "negociação", "prospecção", "crm", "atendimento ao cliente", "contas", "relacionamento", "vendedor" } },
            { "Gerente de Projetos", new[] { "gestão de projetos", "pmbok", "pmp", "scrum", "kanban", "agile", "projetos", "cronograma", "planejamento estratégico" } },
            
            // Área de Saúde
            { "Profissional de Saúde", new[] { "saúde", "medicina", "enfermagem", "farmacêutico", "fisioterapia", "nutricionista", "odontologia", "psicologia", "terapêutico" } },
            
            // Área Jurídica
            { "Profissional Jurídico", new[] { "direito", "jurídico", "advogado", "legislação", "contrato", "assessoria jurídica", "compliance", "normas regulatórias" } },
            
            // Área de Educação
            { "Profissional de Educação", new[] { "educação", "ensino", "professor", "treinamento", "instrução", "aula", "pedagogia", "licenciatura" } },
            
            // Área Industrial/Engenharia
            { "Profissional de Engenharia", new[] { "engenharia", "produção", "manutenção", "logística", "industrial", "manufatura", "automação", "produção" } },
            
            // Atendimento e Suporte
            { "Profissional de Atendimento", new[] { "atendimento", "suporte", "call center", "help desk", "customer service", "sac", "atendimento ao cliente" } }
        };
        
        // Conta a correspondência para cada área
        var matches = new Dictionary<string, int>();
        
        // Primeiro, verifica áreas específicas que devem ter prioridade
        var areasEspecificas = new[] { "Chef de Cozinha" };
        foreach (var areaEspecifica in areasEspecificas)
        {
            if (areaMappings.TryGetValue(areaEspecifica, out var keywordsEspecificos))
            {
                var count = keywordsEspecificos.Count(keyword => 
                    skills.Any(skill => skill.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
                
                // Se encontrou pelo menos 2 correspondências em área específica, retorna imediatamente
                if (count >= 2)
                {
                    return areaEspecifica;
                }
            }
        }
        
        // Se não encontrou área específica com alta confiança, verifica todas as áreas
        foreach (var mapping in areaMappings)
        {
            var area = mapping.Key;
            var keywords = mapping.Value;
            
            var count = keywords.Count(keyword => 
                skills.Any(skill => skill.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
            
            if (count > 0)
            {
                matches[area] = count;
            }
        }
        
        // Retorna a área com mais correspondências, mas só se tiver pelo menos 2 matches
        if (matches.Any())
        {
            var melhorMatch = matches.OrderByDescending(m => m.Value).First();
            // Só retorna se tiver pelo menos 2 correspondências ou se for área específica
            if (melhorMatch.Value >= 2 || areasEspecificas.Contains(melhorMatch.Key))
            {
                return melhorMatch.Key;
            }
        }
        
        return string.Empty;
    }

    /// <inheritdoc />
    public async Task<JobSearchResponseDto> BuscarVagasComSugestaoDeCargo(
        List<string> skills, 
        string? location = "brasil",
        string? category = null,
        int page = 1,
        int resultsPerPage = 20,
        CancellationToken cancellationToken = default)
    {
        // Se não há habilidades, retorna vazio
        if (skills == null || !skills.Any())
        {
            _logger.LogWarning("Tentativa de buscar vagas sem fornecer habilidades");
            return new JobSearchResponseDto { Results = new List<JobDto>() };
        }

        try
        {
            // 1. Sugerir cargo com IA
            var cargos = await SugerirCargos(skills, cancellationToken);
            var cargoSugerido = cargos.FirstOrDefault();
            
            // Se não conseguiu sugerir cargo, usa a primeira habilidade
            if (string.IsNullOrWhiteSpace(cargoSugerido))
            {
                cargoSugerido = skills.First();
                _logger.LogInformation("Nenhum cargo sugerido, usando primeira habilidade: {Skill}", cargoSugerido);
            }
            
            // 2. Determinar categoria apropriada se não foi fornecida
            if (string.IsNullOrWhiteSpace(category))
            {
                category = DeterminarCategoriaAdzuna(cargoSugerido, skills);
                _logger.LogInformation("Categoria determinada automaticamente: {Categoria} para cargo: {Cargo}", category, cargoSugerido);
            }
            
            // 3. Buscar no Adzuna com o cargo sugerido
            var request = new JobSearchRequestDto
            {
                Cargo = cargoSugerido,
                Localizacao = location,
                Categoria = category,
                Pagina = page,
                ResultadosPorPagina = resultsPerPage
            };
            
            _logger.LogInformation(
                "Buscando vagas com cargo sugerido: {Cargo}, Categoria: {Categoria}, Localização: {Local}", 
                cargoSugerido, 
                category ?? "nenhuma",
                location);
                
            var vagas = await _adzunaService.BuscarVagasAsync(request);
            
            // 4. Se não encontrou vagas e há categoria, tenta sem categoria (busca geral)
            if (vagas.Results.Count == 0 && !string.IsNullOrWhiteSpace(category))
            {
                _logger.LogInformation("Sem resultados com categoria {Categoria}, tentando busca sem categoria", category);
                request.Categoria = null;
                vagas = await _adzunaService.BuscarVagasAsync(request);
            }
            
            // 5. Se ainda não encontrou, tenta uma busca com as duas primeiras habilidades
            if (vagas.Results.Count == 0 && skills.Count >= 2)
            {
                var queryFallback = string.Join(" ", skills.Take(2));
                _logger.LogInformation("Sem resultados, tentando fallback com: {Query}", queryFallback);
                
                request.Cargo = queryFallback;
                request.Categoria = null; // Remove categoria para busca mais ampla
                vagas = await _adzunaService.BuscarVagasAsync(request);
            }
            
            // 6. Se ainda não encontrou, tenta com a primeira habilidade
            if (vagas.Results.Count == 0 && skills.Count >= 1)
            {
                _logger.LogInformation("Ainda sem resultados, tentando com a primeira habilidade: {Skill}", skills.First());
                
                request.Cargo = skills.First();
                request.Categoria = null;
                vagas = await _adzunaService.BuscarVagasAsync(request);
            }
            
            return vagas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar vagas com sugestão de cargo");
            throw;
        }
    }
    
    /// <summary>
    /// Determina a categoria do Adzuna com base no cargo sugerido e nas habilidades
    /// </summary>
    private string? DeterminarCategoriaAdzuna(string cargo, List<string> skills)
    {
        if (string.IsNullOrWhiteSpace(cargo))
        {
            return null;
        }
        
        var cargoLower = cargo.ToLowerInvariant();
        var skillsLower = skills.Select(s => s.ToLowerInvariant()).ToList();
        
        // Mapeamento de palavras-chave para categorias do Adzuna
        // Baseado na documentação do Adzuna para Brasil
        
        // TI e Tecnologia
        if (cargoLower.Contains("desenvolvedor") || cargoLower.Contains("programador") || 
            cargoLower.Contains("engenheiro de software") || cargoLower.Contains("analista de sistemas") ||
            cargoLower.Contains("devops") || cargoLower.Contains("dba") || cargoLower.Contains("qa") ||
            cargoLower.Contains("cientista de dados") || cargoLower.Contains("arquiteto de software") ||
            skillsLower.Any(s => s.Contains("programação") || s.Contains("java") || s.Contains("python") || 
                               s.Contains("javascript") || s.Contains("react") || s.Contains("node")))
        {
            return "it-jobs";
        }
        
        // Engenharia
        if ((cargoLower.Contains("engenheiro") && !cargoLower.Contains("software")) ||
            cargoLower.Contains("engenharia") || cargoLower.Contains("projetista"))
        {
            return "engineering-jobs";
        }
        
        // Saúde
        if (cargoLower.Contains("médico") || cargoLower.Contains("enfermeiro") || cargoLower.Contains("enfermagem") ||
            cargoLower.Contains("fisioterapeuta") || cargoLower.Contains("nutricionista") || 
            cargoLower.Contains("farmacêutico") || cargoLower.Contains("psicólogo") ||
            cargoLower.Contains("dentista") || cargoLower.Contains("odontologia") ||
            skillsLower.Any(s => s.Contains("saúde") || s.Contains("medicina") || s.Contains("enfermagem")))
        {
            return "healthcare-nursing-jobs";
        }
        
        // Finanças e Contabilidade
        if (cargoLower.Contains("contador") || cargoLower.Contains("contabilidade") ||
            cargoLower.Contains("analista financeiro") || cargoLower.Contains("financeiro") ||
            cargoLower.Contains("auditor") || cargoLower.Contains("tesoureiro") ||
            skillsLower.Any(s => s.Contains("contabilidade") || s.Contains("finanças") || s.Contains("fiscal")))
        {
            return "accounting-finance-jobs";
        }
        
        // Marketing e Vendas
        if (cargoLower.Contains("marketing") || cargoLower.Contains("vendedor") || cargoLower.Contains("vendas") ||
            cargoLower.Contains("comercial") || cargoLower.Contains("publicidade") ||
            skillsLower.Any(s => s.Contains("marketing") || s.Contains("vendas") || s.Contains("seo")))
        {
            return "sales-jobs";
        }
        
        // Recursos Humanos
        if (cargoLower.Contains("recursos humanos") || cargoLower.Contains("rh") ||
            cargoLower.Contains("recrutador") || cargoLower.Contains("dp") ||
            skillsLower.Any(s => s.Contains("recursos humanos") || s.Contains("recrutamento")))
        {
            return "hr-recruitment-jobs";
        }
        
        // Educação
        if (cargoLower.Contains("professor") || cargoLower.Contains("educação") ||
            cargoLower.Contains("pedagogo") || cargoLower.Contains("instrutor") ||
            skillsLower.Any(s => s.Contains("ensino") || s.Contains("educação") || s.Contains("aula")))
        {
            return "teaching-jobs";
        }
        
        // Jurídico
        if (cargoLower.Contains("advogado") || cargoLower.Contains("jurídico") ||
            cargoLower.Contains("direito") || skillsLower.Any(s => s.Contains("jurídico") || s.Contains("direito")))
        {
            return "legal-jobs";
        }
        
        // Administração e Secretariado
        if (cargoLower.Contains("administrativo") || cargoLower.Contains("secretário") ||
            cargoLower.Contains("assistente administrativo") ||
            skillsLower.Any(s => s.Contains("administrativo") || s.Contains("secretariado")))
        {
            return "admin-secretarial-jobs";
        }
        
        // Atendimento ao Cliente
        if (cargoLower.Contains("atendimento") || cargoLower.Contains("call center") ||
            cargoLower.Contains("suporte") || cargoLower.Contains("sac") ||
            skillsLower.Any(s => s.Contains("atendimento") || s.Contains("call center")))
        {
            return "customer-service-jobs";
        }
        
        // Gastronomia e Hospitalidade
        if (cargoLower.Contains("chef") || cargoLower.Contains("cozinha") || cargoLower.Contains("cozinheiro") ||
            cargoLower.Contains("gastronomia") || cargoLower.Contains("culinária") ||
            cargoLower.Contains("confeiteiro") || cargoLower.Contains("padeiro") ||
            skillsLower.Any(s => s.Contains("culinária") || s.Contains("gastronomia") || s.Contains("chef") || 
                               s.Contains("haccp") || s.Contains("kitchen brigade") || s.Contains("sous-vide")))
        {
            return "hospitality-catering-jobs";
        }
        
        // Se não encontrou categoria específica, retorna null para busca geral
        return null;
    }
    
    /// <summary>
    /// Limpa e normaliza o cargo sugerido pela IA, removendo explicações e formatação extra
    /// </summary>
    private string LimparCargoSugerido(string cargo)
    {
        if (string.IsNullOrWhiteSpace(cargo))
        {
            return string.Empty;
        }
        
        // Remove o prompt echo se houver
        var linhas = cargo.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cargoLimpo = linhas.LastOrDefault() ?? cargo;
        
        // Remove prefixos comuns
        cargoLimpo = cargoLimpo.Trim();
        cargoLimpo = System.Text.RegularExpressions.Regex.Replace(cargoLimpo, @"^(Cargo sugerido|Cargo|O cargo é|É|Sugestão):\s*", "", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        // Remove pontos finais, vírgulas extras, parênteses com explicações
        cargoLimpo = System.Text.RegularExpressions.Regex.Replace(cargoLimpo, @"\s*\([^)]*\)\s*", " "); // Remove (explicações)
        cargoLimpo = cargoLimpo.TrimEnd('.', ',', ';', ':', '-', ' ');
        
        // Pega apenas a primeira linha se houver múltiplas
        cargoLimpo = cargoLimpo.Split('\n').FirstOrDefault()?.Trim() ?? cargoLimpo;
        
        // Remove espaços múltiplos
        cargoLimpo = System.Text.RegularExpressions.Regex.Replace(cargoLimpo, @"\s+", " ");
        
        return cargoLimpo.Trim();
    }
}
