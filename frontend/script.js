// Configuração da API
const API_BASE_URL = 'http://localhost:5000/api/v1';

// Elementos do DOM
const curriculoInput = document.getElementById('curriculoInput');
const fileName = document.getElementById('fileName');
const curriculoInfo = document.getElementById('curriculoInfo');
const keywordsDiv = document.getElementById('keywords');
const locationsDiv = document.getElementById('locations');
const locationsTitle = document.getElementById('locationsTitle');
const searchForm = document.getElementById('searchForm');
const container = document.querySelector('.container');
const loading = document.getElementById('loading');
const resultsSection = document.getElementById('resultsSection');
const resultsCount = document.getElementById('resultsCount');
const jobsList = document.getElementById('jobsList');

// Palavras-chave e localizações extraídas do currículo
let extractedKeywords = [];
let extractedLocations = [];

// Upload de Currículo
curriculoInput.addEventListener('change', async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    fileName.textContent = file.name;

    await extractKeywordsFromCurriculo(file);
});

async function extractKeywordsFromCurriculo(file) {
    try {
        curriculoInfo.classList.add('hidden');
        keywordsDiv.innerHTML = '';
        locationsDiv.innerHTML = '';
        locationsTitle.classList.add('hidden');

        const formData = new FormData();
        formData.append('file', file);

        const response = await fetch(`${API_BASE_URL}/resumes/skills`, {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            const error = await safeParseError(response);
            throw new Error(error?.message || 'Falha ao extrair palavras-chave do currículo');
        }

        const data = await response.json();
        extractedKeywords = data.skills || [];
        extractedLocations = data.locations || [];

        if (extractedKeywords.length === 0) {
            showKeywordsMessage('Nenhuma habilidade foi identificada automaticamente. Você ainda pode buscar vagas manualmente.');
        } else {
            renderKeywords(extractedKeywords);
        }

        if (extractedLocations.length > 0) {
            renderLocations(extractedLocations);
            locationsTitle.classList.remove('hidden');
        } else {
            locationsTitle.classList.add('hidden');
        }

        const suggestedLocation = data.suggestedLocation || extractedLocations[0] || '';
        if (suggestedLocation) {
            document.getElementById('localizacao').value = suggestedLocation;
        }

        // Não preenchemos o campo aqui, vamos esperar pelo retorno da sugestão de cargo
        curriculoInfo.classList.remove('hidden');

        // Buscar vagas usando as skills extraídas
        if (extractedKeywords.length > 0) {
            await buscarVagas(buildSearchQuery(extractedKeywords), document.getElementById('localizacao').value.trim());
        }
    } catch (error) {
        console.error('Erro ao processar currículo:', error);
        extractedKeywords = [];
        extractedLocations = [];
        showKeywordsMessage(error.message || 'Erro ao processar currículo. Tente novamente.');
        locationsDiv.innerHTML = '';
        locationsTitle.classList.add('hidden');
        curriculoInfo.classList.remove('hidden');
        displayTransientError('Não foi possível processar o currículo. Verifique se o arquivo está em PDF.');
    }
}

// Buscar Vagas
searchForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const cargo = document.getElementById('cargo').value.trim();
    const localizacao = document.getElementById('localizacao').value.trim();
    
    if (!cargo) {
        alert('Por favor, informe um cargo ou palavra-chave');
        return;
    }
    
    await buscarVagas(cargo, localizacao);
});

// Função para buscar vagas na API
async function buscarVagas(cargo, localizacao) {
    try {
        // Mostrar loading
        loading.classList.remove('hidden');
        resultsSection.classList.add('hidden');
        
        // Verificar se estamos usando habilidades extraídas do currículo
        const isUsingExtractedSkills = extractedKeywords.length > 0 && 
            cargo === buildSearchQuery(extractedKeywords);
        
        let response;
        
        if (isUsingExtractedSkills) {
            // Usar o endpoint AI que sugere cargos com base nas habilidades
            console.log('Usando endpoint AI com habilidades:', extractedKeywords);
            response = await fetch(`${API_BASE_URL}/jobs/search/skills`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    habilidades: extractedKeywords,
                    localizacao: localizacao || 'brasil',
                    pagina: 1,
                    resultadosPorPagina: 20
                })
            });
            
            // Solicitar o cargo sugerido para atualizar o campo de busca
            const suggestResponse = await fetch(`${API_BASE_URL}/jobs/suggest-jobs`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(extractedKeywords)
            });
            
            if (suggestResponse.ok) {
                const cargosSugeridos = await suggestResponse.json();
                if (cargosSugeridos && cargosSugeridos.length > 0) {
                    // Atualizar o campo de busca com o cargo sugerido
                    document.getElementById('cargo').value = cargosSugeridos[0];
                    console.log('Campo de busca atualizado com cargo sugerido:', cargosSugeridos[0]);
                }
            }
        } else {
            // Usar o endpoint padrão com texto de busca direto
            console.log('Usando endpoint padrão com texto:', cargo);
            response = await fetch(`${API_BASE_URL}/jobs/search`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    cargo: cargo,
                    localizacao: localizacao || 'brasil',
                    pagina: 1,
                    resultadosPorPagina: 20
                })
            });
        }
        
        if (!response.ok) {
            throw new Error('Erro ao buscar vagas');
        }
        
        const data = await response.json();
        
        // Ocultar loading
        loading.classList.add('hidden');
        
        // Exibir resultados
        exibirResultados(data);
        
    } catch (error) {
        console.error('Erro:', error);
        loading.classList.add('hidden');
        
        // Exibir erro
        const errorDiv = document.createElement('div');
        errorDiv.className = 'error';
        errorDiv.textContent = 'Erro ao buscar vagas. Verifique se a API está rodando e se as credenciais do Adzuna estão configuradas.';
        document.querySelector('.container').insertBefore(errorDiv, resultsSection);
        
        setTimeout(() => errorDiv.remove(), 5000);
    }
}

// Função para exibir resultados
function exibirResultados(data) {
    const jobs = data.results || [];
    
    if (jobs.length === 0) {
        resultsCount.textContent = 'Nenhuma vaga encontrada';
        jobsList.innerHTML = '<p style="text-align: center; color: #666;">Tente ajustar os filtros de busca.</p>';
    } else {
        resultsCount.textContent = `${jobs.length} vaga(s) encontrada(s)`;
        
        jobsList.innerHTML = '';
        
        jobs.forEach(job => {
            const jobCard = criarJobCard(job);
            jobsList.appendChild(jobCard);
        });
    }
    
    resultsSection.classList.remove('hidden');
    
    // Scroll suave até os resultados
    resultsSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// Função para criar card de vaga
function criarJobCard(job) {
    const card = document.createElement('div');
    card.className = 'job-card';
    
    // Debug: ver estrutura do job
    console.log('Job data:', job);
    
    const title = document.createElement('h3');
    title.className = 'job-title';
    title.textContent = job.title || 'Título não disponível';
    
    const company = document.createElement('p');
    company.className = 'job-company';
    const companyName = job.company?.display_name || 'Empresa não informada';
    company.textContent = `🏢 ${companyName}`;
    
    const location = document.createElement('p');
    location.className = 'job-location';
    const locationName = job.location?.display_name || 'Localização não informada';
    location.textContent = `📍 ${locationName}`;
    
    const salary = document.createElement('p');
    salary.className = 'job-salary';
    const salaryMin = job.salary_min;
    const salaryMax = job.salary_max;
    
    if (salaryMin && salaryMax) {
        salary.textContent = `💰 R$ ${Math.round(salaryMin).toLocaleString('pt-BR')} - R$ ${Math.round(salaryMax).toLocaleString('pt-BR')}`;
    } else if (salaryMin) {
        salary.textContent = `💰 A partir de R$ ${Math.round(salaryMin).toLocaleString('pt-BR')}`;
    } else {
        salary.textContent = '💰 Salário não informado';
    }
    
    const link = document.createElement('a');
    link.className = 'job-link';
    const jobUrl = job.redirect_url;
    
    if (jobUrl && jobUrl !== '') {
        link.href = jobUrl;
        link.target = '_blank';
        link.rel = 'noopener noreferrer';
        link.textContent = 'Ver vaga';
    } else {
        link.style.background = '#ccc';
        link.style.cursor = 'not-allowed';
        link.textContent = 'Link não disponível';
        link.onclick = (e) => e.preventDefault();
    }
    
    card.appendChild(title);
    card.appendChild(company);
    card.appendChild(location);
    card.appendChild(salary);
    card.appendChild(link);
    
    return card;
}

// Mensagem de boas-vindas
console.log('🚀 Frontend carregado com sucesso!');
console.log('📡 API: ' + API_BASE_URL);

function renderKeywords(keywords) {
    keywordsDiv.innerHTML = '';
    keywords.forEach(keyword => {
        const span = document.createElement('span');
        span.className = 'keyword';
        span.textContent = keyword;
        keywordsDiv.appendChild(span);
    });
}

function renderLocations(locations) {
    locationsDiv.innerHTML = '';
    locations.forEach(location => {
        const span = document.createElement('span');
        span.className = 'keyword';
        span.textContent = location;
        locationsDiv.appendChild(span);
    });
}

function showKeywordsMessage(message) {
    keywordsDiv.innerHTML = '';
    const paragraph = document.createElement('p');
    paragraph.style.color = '#555';
    paragraph.textContent = message;
    keywordsDiv.appendChild(paragraph);
}

async function safeParseError(response) {
    try {
        const data = await response.json();
        return data;
    } catch {
        return null;
    }
}

function displayTransientError(message) {
    const errorDiv = document.createElement('div');
    errorDiv.className = 'error';
    errorDiv.textContent = message;
    container.insertBefore(errorDiv, container.firstChild);
    setTimeout(() => errorDiv.remove(), 5000);
}

function buildSearchQuery(keywords) {
    if (!Array.isArray(keywords) || keywords.length === 0) {
        return '';
    }

    const topKeywords = keywords
        .filter(keyword => typeof keyword === 'string' && keyword.trim().length > 0)
        .slice(0, 5);

    return topKeywords.join(' ');
}
