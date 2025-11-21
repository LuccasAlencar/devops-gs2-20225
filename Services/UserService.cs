using dotnet_gs2_2025.Models;
using dotnet_gs2_2025.Models.DTOs;
using dotnet_gs2_2025.Repositories;

namespace dotnet_gs2_2025.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PagedResponse<UserResponseDto>> GetAllUsersAsync(int page, int pageSize, string baseUrl)
    {
        _logger.LogInformation("Serviço: Obtendo lista de usuários - Página: {Page}", page);

        var (users, totalCount) = await _repository.GetAllAsync(page, pageSize);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var userDtos = users.Select(u => MapToResponseDto(u, baseUrl)).ToList();

        var response = new PagedResponse<UserResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = totalPages,
            Data = userDtos,
            Links = GeneratePaginationLinks(page, pageSize, totalPages, baseUrl)
        };

        return response;
    }

    public async Task<UserResponseDto?> GetUserByIdAsync(int id, string baseUrl)
    {
        _logger.LogInformation("Serviço: Obtendo usuário por ID: {UserId}", id);

        var user = await _repository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("Serviço: Usuário não encontrado - ID: {UserId}", id);
            return null;
        }

        return MapToResponseDto(user, baseUrl);
    }

    public async Task<UserResponseDto> CreateUserAsync(UserCreateDto userDto, string baseUrl)
    {
        _logger.LogInformation("Serviço: Criando novo usuário - Email: {Email}", userDto.Email);

        // Verificar se email já existe
        var existingUser = await _repository.GetByEmailAsync(userDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Serviço: Email já cadastrado - Email: {Email}", userDto.Email);
            throw new InvalidOperationException("Email já cadastrado no sistema");
        }

        // Hash da senha (em produção, use BCrypt ou similar)
        var hashedPassword = HashPassword(userDto.Password);

        var user = new User
        {
            Name = userDto.Name,
            Email = userDto.Email,
            Password = hashedPassword,
            Phone = userDto.Phone
        };

        var createdUser = await _repository.CreateAsync(user);
        return MapToResponseDto(createdUser, baseUrl);
    }

    public async Task<UserResponseDto?> UpdateUserAsync(int id, UserUpdateDto userDto, string baseUrl)
    {
        _logger.LogInformation("Serviço: Atualizando usuário - ID: {UserId}", id);

        var existingUser = await _repository.GetByIdAsync(id);
        if (existingUser == null)
        {
            _logger.LogWarning("Serviço: Usuário não encontrado para atualização - ID: {UserId}", id);
            return null;
        }

        // Verificar se o novo email já existe (se fornecido)
        if (!string.IsNullOrEmpty(userDto.Email) && userDto.Email != existingUser.Email)
        {
            var emailExists = await _repository.GetByEmailAsync(userDto.Email);
            if (emailExists != null)
            {
                _logger.LogWarning("Serviço: Email já cadastrado - Email: {Email}", userDto.Email);
                throw new InvalidOperationException("Email já cadastrado no sistema");
            }
        }

        // Atualizar apenas os campos fornecidos
        if (!string.IsNullOrEmpty(userDto.Name))
            existingUser.Name = userDto.Name;

        if (!string.IsNullOrEmpty(userDto.Email))
            existingUser.Email = userDto.Email;

        if (!string.IsNullOrEmpty(userDto.Password))
            existingUser.Password = HashPassword(userDto.Password);

        if (userDto.Phone != null)
            existingUser.Phone = userDto.Phone;

        var updatedUser = await _repository.UpdateAsync(id, existingUser);
        return updatedUser != null ? MapToResponseDto(updatedUser, baseUrl) : null;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        _logger.LogInformation("Serviço: Deletando usuário - ID: {UserId}", id);
        return await _repository.DeleteAsync(id);
    }

    private UserResponseDto MapToResponseDto(User user, string baseUrl)
    {
        var dto = new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Links = GenerateUserLinks(user.Id, baseUrl)
        };

        return dto;
    }

    private List<Link> GenerateUserLinks(int userId, string baseUrl)
    {
        return new List<Link>
        {
            new Link
            {
                Href = $"{baseUrl}/{userId}",
                Rel = "self",
                Method = "GET"
            },
            new Link
            {
                Href = $"{baseUrl}/{userId}",
                Rel = "update",
                Method = "PUT"
            },
            new Link
            {
                Href = $"{baseUrl}/{userId}",
                Rel = "delete",
                Method = "DELETE"
            },
            new Link
            {
                Href = baseUrl,
                Rel = "all-users",
                Method = "GET"
            }
        };
    }

    private List<Link> GeneratePaginationLinks(int currentPage, int pageSize, int totalPages, string baseUrl)
    {
        var links = new List<Link>
        {
            new Link
            {
                Href = $"{baseUrl}?page={currentPage}&pageSize={pageSize}",
                Rel = "self",
                Method = "GET"
            }
        };

        if (currentPage > 1)
        {
            links.Add(new Link
            {
                Href = $"{baseUrl}?page={currentPage - 1}&pageSize={pageSize}",
                Rel = "previous",
                Method = "GET"
            });

            links.Add(new Link
            {
                Href = $"{baseUrl}?page=1&pageSize={pageSize}",
                Rel = "first",
                Method = "GET"
            });
        }

        if (currentPage < totalPages)
        {
            links.Add(new Link
            {
                Href = $"{baseUrl}?page={currentPage + 1}&pageSize={pageSize}",
                Rel = "next",
                Method = "GET"
            });

            links.Add(new Link
            {
                Href = $"{baseUrl}?page={totalPages}&pageSize={pageSize}",
                Rel = "last",
                Method = "GET"
            });
        }

        return links;
    }

    private string HashPassword(string password)
    {
        // Usando BCrypt para hash seguro de senhas
        // O BCrypt gera automaticamente um salt único para cada senha
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }
}
