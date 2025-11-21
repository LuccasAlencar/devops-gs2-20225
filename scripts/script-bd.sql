-- Script de Banco de Dados para Azure Database for MySQL
-- Cria tabelas, constraints e dados iniciais para a aplicação
-- Schema compatível com o modelo User do .NET

-- Criar banco de dados
CREATE DATABASE IF NOT EXISTS dotnet_gs2;
USE dotnet_gs2;

-- Tabela de Usuários (compatível com User.cs)
CREATE TABLE IF NOT EXISTS users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL,
    phone VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_email (email),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Inserir usuários de exemplo
INSERT INTO users (name, email, password, phone)
VALUES 
    ('Admin User', 'admin@example.com', '$2a$12$5L1sOqPCHQGvE4sJa2p7r.NeWAWBXXDKlLHDWvGa7ZqPEqYj3tqcS', '11999999999'),
    ('Test User', 'test@example.com', '$2a$12$5L1sOqPCHQGvE4sJa2p7r.NeWAWBXXDKlLHDWvGa7ZqPEqYj3tqcS', '11988888888')
ON DUPLICATE KEY UPDATE email = email;

-- Confirmação
SELECT 'Database setup completed successfully!' as status;
