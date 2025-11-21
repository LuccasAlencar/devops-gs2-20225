-- Script de Banco de Dados para Azure Database for MySQL
-- Cria tabelas, constraints e dados iniciais para a aplicação

-- Criar banco de dados
CREATE DATABASE IF NOT EXISTS dotnet_gs2;
USE dotnet_gs2;

-- Tabela de Usuários
CREATE TABLE IF NOT EXISTS users (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    full_name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    phone VARCHAR(20),
    location VARCHAR(255),
    date_of_birth DATE,
    bio TEXT,
    profile_picture_url VARCHAR(500),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_email (email),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de Currículos
CREATE TABLE IF NOT EXISTS resumes (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id BIGINT NOT NULL,
    file_url VARCHAR(500),
    full_text LONGTEXT,
    skills JSON,
    experience JSON,
    education JSON,
    extracted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de Buscas de Vagas
CREATE TABLE IF NOT EXISTS job_searches (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id BIGINT NOT NULL,
    keywords VARCHAR(255),
    location VARCHAR(255),
    job_type VARCHAR(50),
    searched_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_searched_at (searched_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de Vagas (cache)
CREATE TABLE IF NOT EXISTS jobs (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    external_id VARCHAR(255) UNIQUE,
    title VARCHAR(255) NOT NULL,
    company VARCHAR(255) NOT NULL,
    description LONGTEXT,
    location VARCHAR(255),
    job_type VARCHAR(50),
    salary_min DECIMAL(10, 2),
    salary_max DECIMAL(10, 2),
    currency VARCHAR(10),
    url VARCHAR(500),
    source VARCHAR(100),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_external_id (external_id),
    INDEX idx_company (company),
    INDEX idx_location (location)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de Favoritos/Saved Jobs
CREATE TABLE IF NOT EXISTS saved_jobs (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id BIGINT NOT NULL,
    job_id BIGINT NOT NULL,
    saved_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (job_id) REFERENCES jobs(id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_job (user_id, job_id),
    INDEX idx_user_id (user_id),
    INDEX idx_saved_at (saved_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabela de Audit/Logging
CREATE TABLE IF NOT EXISTS audit_logs (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    user_id BIGINT,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100),
    entity_id BIGINT,
    details JSON,
    ip_address VARCHAR(45),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
    INDEX idx_user_id (user_id),
    INDEX idx_action (action),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Dados de exemplo
INSERT INTO users (full_name, email, password_hash, phone, location, bio, is_active)
VALUES 
    ('Admin User', 'admin@example.com', '$2a$11$5L1sOqPCHQGvE4sJa2p7r.NeWAWBXXDKlLHDWvGa7ZqPEqYj3tqcS', '11999999999', 'São Paulo, SP', 'Administrator', TRUE),
    ('Test User', 'test@example.com', '$2a$11$5L1sOqPCHQGvE4sJa2p7r.NeWAWBXXDKlLHDWvGa7ZqPEqYj3tqcS', '11988888888', 'Rio de Janeiro, RJ', 'Test User', TRUE);

-- Criar índices de performance
CREATE INDEX idx_users_is_active ON users(is_active);
CREATE INDEX idx_jobs_source ON jobs(source);

-- Criar view para estatísticas
CREATE VIEW user_statistics AS
SELECT 
    u.id,
    u.full_name,
    u.email,
    COUNT(DISTINCT js.id) as total_searches,
    COUNT(DISTINCT sj.id) as total_saved_jobs,
    COUNT(DISTINCT r.id) as total_resumes,
    u.created_at
FROM users u
LEFT JOIN job_searches js ON u.id = js.user_id
LEFT JOIN saved_jobs sj ON u.id = sj.user_id
LEFT JOIN resumes r ON u.id = r.user_id
GROUP BY u.id, u.full_name, u.email, u.created_at;

-- Criar procedure para limpar dados antigos
DELIMITER $$
CREATE PROCEDURE cleanup_old_data()
BEGIN
    DELETE FROM audit_logs WHERE created_at < DATE_SUB(NOW(), INTERVAL 90 DAY);
    DELETE FROM job_searches WHERE searched_at < DATE_SUB(NOW(), INTERVAL 30 DAY);
END$$
DELIMITER ;

-- Confirmação
SELECT 'Database setup completed successfully!' as status;
