using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using dotnet_gs2_2025.Models.DTOs;
using dotnet_gs2_2025.Services;

namespace dotnet_gs2_2025.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Obtém lista paginada de usuários
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Tamanho da página (padrão: 10, máximo: 100)</param>
    /// <returns>Lista paginada de usuários com links HATEOAS</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<UserResponseDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("GET /api/v1/users - Página: {Page}, Tamanho: {PageSize}", page, pageSize);

        if (page < 1)
        {
            _logger.LogWarning("Número da página inválido: {Page}", page);
            return BadRequest(new { message = "Número da página deve ser maior que 0" });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            _logger.LogWarning("Tamanho da página inválido: {PageSize}", pageSize);
            return BadRequest(new { message = "Tamanho da página deve estar entre 1 e 100" });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        var result = await _userService.GetAllUsersAsync(page, pageSize, baseUrl);

        return Ok(result);
    }

    /// <summary>
    /// Obtém um usuário específico por ID
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <returns>Dados do usuário com links HATEOAS</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        _logger.LogInformation("GET /api/v1/users/{UserId}", id);

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v1/users";
        var user = await _userService.GetUserByIdAsync(id, baseUrl);

        if (user == null)
        {
            _logger.LogWarning("Usuário não encontrado - ID: {UserId}", id);
            return NotFound(new { message = $"Usuário com ID {id} não encontrado" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Cria um novo usuário
    /// </summary>
    /// <param name="userDto">Dados do usuário a ser criado</param>
    /// <returns>Usuário criado com links HATEOAS</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] UserCreateDto userDto)
    {
        _logger.LogInformation("POST /api/v1/users - Email: {Email}", userDto.Email);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Dados inválidos para criação de usuário");
            return BadRequest(ModelState);
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v1/users";
            var user = await _userService.CreateUserAsync(userDto, baseUrl);

            _logger.LogInformation("Usuário criado com sucesso - ID: {UserId}", user.Id);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Erro ao criar usuário: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Atualiza um usuário existente
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="userDto">Dados a serem atualizados</param>
    /// <returns>Usuário atualizado com links HATEOAS</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, [FromBody] UserUpdateDto userDto)
    {
        _logger.LogInformation("PUT /api/v1/users/{UserId}", id);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Dados inválidos para atualização de usuário");
            return BadRequest(ModelState);
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v1/users";
            var user = await _userService.UpdateUserAsync(id, userDto, baseUrl);

            if (user == null)
            {
                _logger.LogWarning("Usuário não encontrado para atualização - ID: {UserId}", id);
                return NotFound(new { message = $"Usuário com ID {id} não encontrado" });
            }

            _logger.LogInformation("Usuário atualizado com sucesso - ID: {UserId}", id);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Erro ao atualizar usuário: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove um usuário
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <returns>Status da operação</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        _logger.LogInformation("DELETE /api/v1/users/{UserId}", id);

        var result = await _userService.DeleteUserAsync(id);

        if (!result)
        {
            _logger.LogWarning("Usuário não encontrado para exclusão - ID: {UserId}", id);
            return NotFound(new { message = $"Usuário com ID {id} não encontrado" });
        }

        _logger.LogInformation("Usuário deletado com sucesso - ID: {UserId}", id);
        return NoContent();
    }
}
