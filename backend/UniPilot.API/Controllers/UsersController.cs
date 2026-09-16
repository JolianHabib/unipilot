using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        var fullName = User.FindFirst(
            ClaimTypes.Name)?.Value;

        var email = User.FindFirst(
            ClaimTypes.Email)?.Value;

        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            id = userId,
            fullName,
            email
        });
    }
}