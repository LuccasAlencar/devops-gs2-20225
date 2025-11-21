using dotnet_gs2_2025.Models;

namespace dotnet_gs2_2025.Repositories;

public interface IUserRepository
{
    Task<(List<User> Users, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<User?> UpdateAsync(int id, User user);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
