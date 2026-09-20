using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.Auth;
using UniPilot.Application.Auth;

namespace UniPilot.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.FullName,
            request.Email,
            request.Password);

        var result = await authService.RegisterAsync(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            return Conflict(new
            {
                message = result.Error
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = result.UserId,
            fullName = result.FullName,
            email = result.Email
        });
    }
    [HttpPost("login")]
public async Task<IActionResult> Login(
    [FromBody] LoginRequest request,
    CancellationToken cancellationToken)
{
    var command = new LoginCommand(
        request.Email,
        request.Password);

    var result = await authService.LoginAsync(
        command,
        cancellationToken);

    if (!result.Succeeded)
    {
        return Unauthorized(new
        {
            message = result.Error
        });
    }

    return Ok(new
    {
        user = new
        {
            id = result.UserId,
            fullName = result.FullName,
            email = result.Email
        },
        accessToken = result.AccessToken,
        expiresAtUtc = result.ExpiresAtUtc
    });
    }
}