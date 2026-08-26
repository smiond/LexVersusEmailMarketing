using System.Security.Claims;
using LexVersusEmailMarketing.Core.DTOs;
using LexVersusEmailMarketing.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexVersusEmailMarketing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto, CancellationToken ct)
    {
        try
        {
            var result = await authService.RegisterAsync(dto, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto, CancellationToken ct)
    {
        try
        {
            var result = await authService.LoginAsync(dto, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetUserByIdAsync(userId, ct);
        return user is null ? Unauthorized() : Ok(user);
    }
}
