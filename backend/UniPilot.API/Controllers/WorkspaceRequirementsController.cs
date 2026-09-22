using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.Application.Requirements;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/requirements")]
public sealed class WorkspaceRequirementsController(
    IProjectRequirementService requirementService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var requirements =
            await requirementService
                .GetByOwnerAsync(
                    userId,
                    cancellationToken);

        return Ok(requirements);
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