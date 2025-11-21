using Microsoft.EntityFrameworkCore;
using dotnet_gs2_2025.Data;
using dotnet_gs2_2025.Models;

namespace dotnet_gs2_2025.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<User> Users, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        _logger.LogInformation("Buscando usuários - Página: {Page}, Tamanho: {PageSize}", page, pageSize);
        
        var totalCount = await _context.Users.CountAsync();
        var users = await _context.Users
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (users, totalCount);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Buscando usuário por ID: {UserId}", id);
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        _logger.LogInformation("Buscando usuário por email: {Email}", email);
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> CreateAsync(User user)
    {
        _logger.LogInformation("Criando novo usuário: {Email}", user.Email);
        
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Usuário criado com sucesso - ID: {UserId}", user.Id);
        return user;
    }

    public async Task<User?> UpdateAsync(int id, User user)
    {
        _logger.LogInformation("Atualizando usuário - ID: {UserId}", id);
        
        var existingUser = await _context.Users.FindAsync(id);
        if (existingUser == null)
        {
            _logger.LogWarning("Usuário não encontrado para atualização - ID: {UserId}", id);
            return null;
        }

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;
        existingUser.Password = user.Password;
        existingUser.Phone = user.Phone;
        existingUser.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Usuário atualizado com sucesso - ID: {UserId}", id);
        return existingUser;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("Deletando usuário - ID: {UserId}", id);
        
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            _logger.LogWarning("Usuário não encontrado para exclusão - ID: {UserId}", id);
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Usuário deletado com sucesso - ID: {UserId}", id);
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }
}
