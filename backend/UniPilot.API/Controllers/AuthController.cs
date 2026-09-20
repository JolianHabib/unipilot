using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.Auth;
using UniPilot.Application.Auth;

namespace UniPilot.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService)
    : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody]
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command =
            new RegisterCommand(
                request.FullName,
                request.Email,
                request.Password);

        var result =
            await authService.RegisterAsync(
                command,
                cancellationToken);

        if (!result.Succeeded)
        {
            return Conflict(new
            {
                message = result.Error
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                id = result.UserId,
                fullName =
                    result.FullName,
                email = result.Email
            });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody]
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command =
            new LoginCommand(
                request.Email,
                request.Password);

        var result =
            await authService.LoginAsync(
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
                fullName =
                    result.FullName,
                email = result.Email
            },
            accessToken =
                result.AccessToken,
            expiresAtUtc =
                result.ExpiresAtUtc
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult>
        ForgotPassword(
            [FromBody]
            ForgotPasswordRequest request,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
                request.Email))
        {
            return BadRequest(new
            {
                message =
                    "Email is required."
            });
        }

        await authService
            .ForgotPasswordAsync(
                new ForgotPasswordCommand(
                    request.Email),
                cancellationToken);

        return Ok(new
        {
            message =
                "If an account exists for this email, a password reset link has been sent."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult>
        ResetPassword(
            [FromBody]
            ResetPasswordRequest request,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
                request.Email) ||
            string.IsNullOrWhiteSpace(
                request.Token))
        {
            return BadRequest(new
            {
                message =
                    "The password reset link is invalid."
            });
        }

        if (string.IsNullOrWhiteSpace(
                request.NewPassword) ||
            request.NewPassword.Length < 8)
        {
            return BadRequest(new
            {
                message =
                    "New password must contain at least 8 characters."
            });
        }

        var result =
            await authService
                .ResetPasswordAsync(
                    new ResetPasswordCommand(
                        request.Email,
                        request.Token,
                        request.NewPassword),
                    cancellationToken);

        if (result !=
            ResetPasswordResult.Success)
        {
            return BadRequest(new
            {
                message =
                    "The password reset link is invalid or has expired."
            });
        }

        return NoContent();
    }
}