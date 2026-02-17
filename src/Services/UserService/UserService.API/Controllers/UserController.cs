using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Commands;
using UserService.Application.DTOs;
using UserService.Application.Handlers;
using UserService.Application.Queries;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly LoginHandler _loginHandler;
    private readonly GetUserProfileHandler _getUserProfileHandler;

    public UserController(
        RegisterUserHandler registerUserHandler,
        LoginHandler loginHandler,
        GetUserProfileHandler getUserProfileHandler)
    {
        _registerUserHandler = registerUserHandler;
        _loginHandler = loginHandler;
        _getUserProfileHandler = getUserProfileHandler;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterUserCommand command)
    {
        try
        {
            var result = await _registerUserHandler.HandleAsync(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var command = new LoginCommand
            {
                Email = loginDto.Email,
                Password = loginDto.Password
            };

            var result = await _loginHandler.HandleAsync(command);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("profile/{userId}")]
    public async Task<ActionResult<UserDto>> GetProfile(Guid userId)
    {
        var query = new GetUserProfileQuery { UserId = userId };
        var result = await _getUserProfileHandler.HandleAsync(query);

        if (result == null)
            return NotFound(new { message = "User not found" });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var query = new GetUserProfileQuery { UserId = userId };
        var result = await _getUserProfileHandler.HandleAsync(query);

        if (result == null)
            return NotFound(new { message = "User not found" });

        return Ok(result);
    }
}
