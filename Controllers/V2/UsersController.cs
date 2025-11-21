using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using dotnet_gs2_2025.Models.DTOs;
using dotnet_gs2_2025.Services;

namespace dotnet_gs2_2025.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
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
    /// [V2] Obtém lista paginada de usuários - Versão melhorada com mais opções
    /// </summary>
    /// <param name="page">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Tamanho da página (padrão: 20, máximo: 100)</param>
    /// <returns>Lista paginada de usuários com links HATEOAS e informações adicionais</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<UserResponseDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInformation("GET /api/v2/users - Página: {Page}, Tamanho: {PageSize}", page, pageSize);

        if (page < 1)
        {
            _logger.LogWarning("Número da página inválido: {Page}", page);
            return BadRequest(new 
            { 
                message = "Número da página deve ser maior que 0",
                version = "2.0",
                timestamp = DateTime.UtcNow 
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            _logger.LogWarning("Tamanho da página inválido: {PageSize}", pageSize);
            return BadRequest(new 
            { 
                message = "Tamanho da página deve estar entre 1 e 100",
                version = "2.0",
                timestamp = DateTime.UtcNow 
            });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
        var result = await _userService.GetAllUsersAsync(page, pageSize, baseUrl);

        // V2: Adicionar metadados extras
        Response.Headers.Append("X-API-Version", "2.0");
        Response.Headers.Append("X-Total-Count", result.TotalItems.ToString());
        Response.Headers.Append("X-Total-Pages", result.TotalPages.ToString());

        return Ok(result);
    }

    /// <summary>
    /// [V2] Obtém um usuário específico por ID - Versão melhorada
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <returns>Dados do usuário com links HATEOAS e metadados</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        _logger.LogInformation("GET /api/v2/users/{UserId}", id);

        var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v2/users";
        var user = await _userService.GetUserByIdAsync(id, baseUrl);

        if (user == null)
        {
            _logger.LogWarning("Usuário não encontrado - ID: {UserId}", id);
            return NotFound(new 
            { 
                message = $"Usuário com ID {id} não encontrado",
                version = "2.0",
                timestamp = DateTime.UtcNow 
            });
        }

        Response.Headers.Append("X-API-Version", "2.0");
        return Ok(user);
    }

    /// <summary>
    /// [V2] Cria um novo usuário - Versão melhorada com validações extras
    /// </summary>
    /// <param name="userDto">Dados do usuário a ser criado</param>
    /// <returns>Usuário criado com links HATEOAS</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] UserCreateDto userDto)
    {
        _logger.LogInformation("POST /api/v2/users - Email: {Email}", userDto.Email);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Dados inválidos para criação de usuário");
            return BadRequest(new 
            { 
                errors = ModelState,
                version = "2.0",
                timestamp = DateTime.UtcNow 
            });
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v2/users";
            var user = await _userService.CreateUserAsync(userDto, baseUrl);

            Response.Headers.Append("X-API-Version", "2.0");
            _logger.LogInformation("Usuário criado com sucesso - ID: {UserId}", user.Id);
            
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Erro ao criar usuário: {Message}", ex.Message);
            return Conflict(new 
            { 
                message = ex.Message,
                version = "2.0",
                timestamp = DateTime.UtcNow 
            });
        }
    }

    /// <summary>
    /// [V2] Atualiza um usuário existente - Versão melhorada
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
        _logger.LogInformation("PUT /api/v2/users/{UserId}", id);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Dados inválidos para atualização de usuário");
            return BadRequest(new 
            { 
                errors = ModelState,
                version = "2.0",
                timestamp = DateTime.UtcNow 
            });
        }

        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}/api/v2/users";
            var user = await _userService.UpdateUserAsync(id, userDto, baseUrl);

            if (user == null)
            {
                _logger.LogWarning("Usuário não encontrado para atualização - ID: {UserId}", id);
                return NotFound(new 
                { 
                    message = $"Usuário com ID {id} não encontrado",
                    version = "2.0",
                    timestamp = DateTime.UtcNow 
                });
            }

            Response.Headers.Append("X-API-Version", "2.0");
            _logger.LogInformation("Usuário atualizado com sucesso - ID: {UserId}", id);
            
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Erro ao atualizar usuário: {Message}", ex.Message);
            return Conflict(new 
            { 
                message = ex.Message,
                version = "2.0",
                timestamp = DateTime.UtcNow 
            });
        }
    }

    /// <summary>
    /// [V2] Remove um usuário
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <returns>Status da operação</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        _logger.LogInformation("DELETE /api/v2/users/{UserId}", id);

        var result = await _userService.DeleteUserAsync(id);

        if (!result)
        {
            _logger.LogWarning("Usuário não encontrado para exclusão - ID: {UserId}", id);
            return NotFound(new 
            { 
                message = $"Usuário com ID {id} não encontrado",
                version = "2.0",
                timestamp = DateTime.UtcNow 
            });
        }

        Response.Headers.Append("X-API-Version", "2.0");
        _logger.LogInformation("Usuário deletado com sucesso - ID: {UserId}", id);
        
        return NoContent();
    }
}
