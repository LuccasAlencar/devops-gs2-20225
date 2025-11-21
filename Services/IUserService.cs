using dotnet_gs2_2025.Models.DTOs;

namespace dotnet_gs2_2025.Services;

public interface IUserService
{
    Task<PagedResponse<UserResponseDto>> GetAllUsersAsync(int page, int pageSize, string baseUrl);
    Task<UserResponseDto?> GetUserByIdAsync(int id, string baseUrl);
    Task<UserResponseDto> CreateUserAsync(UserCreateDto userDto, string baseUrl);
    Task<UserResponseDto?> UpdateUserAsync(int id, UserUpdateDto userDto, string baseUrl);
    Task<bool> DeleteUserAsync(int id);
}
