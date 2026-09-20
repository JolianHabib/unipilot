using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.Users;
using UniPilot.Application.Users;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(
    IUserAccountService userAccountService)
    : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult>
        GetCurrentUser(
            CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var user =
            await userAccountService.GetAsync(
                userId,
                cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<IActionResult>
        UpdateProfile(
            [FromBody]
            UpdateProfileRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(
                request.FullName))
        {
            return BadRequest(new
            {
                message =
                    "Full name is required."
            });
        }

        var fullName =
            request.FullName.Trim();

        if (fullName.Length > 100)
        {
            return BadRequest(new
            {
                message =
                    "Full name cannot exceed 100 characters."
            });
        }

        var command =
            new UpdateProfileCommand(
                userId,
                fullName);

        var user =
            await userAccountService
                .UpdateProfileAsync(
                    command,
                    cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost("me/password")]
    public async Task<IActionResult>
        ChangePassword(
            [FromBody]
            ChangePasswordRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(
                request.CurrentPassword))
        {
            return BadRequest(new
            {
                message =
                    "Current password is required."
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

        if (request.CurrentPassword ==
            request.NewPassword)
        {
            return BadRequest(new
            {
                message =
                    "New password must be different from the current password."
            });
        }

        var command =
            new ChangePasswordCommand(
                userId,
                request.CurrentPassword,
                request.NewPassword);

        var result =
            await userAccountService
                .ChangePasswordAsync(
                    command,
                    cancellationToken);

        return result switch
        {
            ChangePasswordResult.Success =>
                NoContent(),

            ChangePasswordResult
                    .InvalidCurrentPassword =>
                BadRequest(new
                {
                    message =
                        "Current password is incorrect."
                }),

            _ => NotFound()
        };
    }

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            value,
            out userId);
    }
}