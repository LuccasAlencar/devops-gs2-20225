-- Script de criação do banco de dados MySQL para Container
-- DEVOPS TOOLS & CLOUD COMPUTING - GS2 2025
-- Compatível com MySQL 8.0 em Docker Container

-- Criar banco de dados se não existir (já criado pelo container)
CREATE DATABASE IF NOT EXISTS devops_gs2_2025 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE devops_gs2_2025;

-- Tabela de Usuários (ajustada para MySQL Container)
CREATE TABLE IF NOT EXISTS users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(150) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    phone VARCHAR(20),
    location VARCHAR(100),
    date_of_birth DATE,
    bio TEXT,
    profile_picture_url VARCHAR(500),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_email (email),
    INDEX idx_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de Currículos (ajustada para MySQL Container)
CREATE TABLE IF NOT EXISTS resumes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    file_url VARCHAR(500),
    file_name VARCHAR(255),
    file_size BIGINT,
    full_text TEXT,
    skills JSON,
    experience JSON,
    education JSON,
    extracted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_extracted_at (extracted_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de Vagas (ajustada para MySQL Container)
CREATE TABLE IF NOT EXISTS jobs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    external_id VARCHAR(100) UNIQUE,
    title VARCHAR(255) NOT NULL,
    company VARCHAR(150),
    description TEXT,
    location VARCHAR(150),
    job_type VARCHAR(50),
    salary_min DECIMAL(10,2),
    salary_max DECIMAL(10,2),
    currency VARCHAR(3) DEFAULT 'BRL',
    url VARCHAR(500),
    source VARCHAR(50) DEFAULT 'Adzuna',
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_external_id (external_id),
    INDEX idx_company (company),
    INDEX idx_location (location),
    INDEX idx_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de Buscas de Vagas (Job Searches) - ajustada para MySQL Container
CREATE TABLE IF NOT EXISTS job_searches (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    resume_id INT,
    query_text VARCHAR(255),
    location VARCHAR(150),
    keywords JSON,
    results_count INT DEFAULT 0,
    search_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (resume_id) REFERENCES resumes(id) ON DELETE SET NULL,
    INDEX idx_user_id (user_id),
    INDEX idx_search_date (search_date)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de Aplicações (Applications) - ajustada para MySQL Container
CREATE TABLE IF NOT EXISTS applications (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    job_id INT NOT NULL,
    resume_id INT,
    status ENUM('applied', 'viewed', 'interview', 'rejected', 'offered', 'withdrawn') DEFAULT 'applied',
    application_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    notes TEXT,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (job_id) REFERENCES jobs(id) ON DELETE CASCADE,
    FOREIGN KEY (resume_id) REFERENCES resumes(id) ON DELETE SET NULL,
    INDEX idx_user_id (user_id),
    INDEX idx_job_id (job_id),
    INDEX idx_status (status),
    INDEX idx_application_date (application_date),
    UNIQUE KEY unique_user_job (user_id, job_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Inserir dados de exemplo (opcional)
INSERT IGNORE INTO users (id, name, email, password, phone, location, is_active) VALUES 
(1, 'Administrador', 'admin@devops.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj6QJw/2Ej7W', '(11) 99999-9999', 'São Paulo, SP', TRUE);

-- Inserir vagas de exemplo
INSERT IGNORE INTO jobs (external_id, title, company, description, location, job_type, salary_min, salary_max, url, source) VALUES 
('adzuna-001', 'Desenvolvedor .NET Pleno', 'Tech Company', 'Desenvolvimento de aplicações em .NET 8 com Azure', 'São Paulo, SP', 'CLT', 7000.00, 12000.00, 'https://www.adzuna.com.br/job/001', 'Adzuna'),
('adzuna-002', 'Analista DevOps Sênior', 'Cloud Solutions', 'Gerenciamento de infraestrutura em nuvem Azure', 'Rio de Janeiro, RJ', 'CLT', 9000.00, 15000.00, 'https://www.adzuna.com.br/job/002', 'Adzuna'),
('adzuna-003', 'Engenheiro de Software Cloud', 'Digital Innovation', 'Desenvolvimento de soluções cloud-native', 'Belo Horizonte, MG', 'PJ', 15000.00, 25000.00, 'https://www.adzuna.com.br/job/003', 'Adzuna');

SELECT 'Banco de dados MySQL Container configurado com sucesso!' as message;
