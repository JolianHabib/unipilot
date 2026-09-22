using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.Application.Activities;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/activities")]
public sealed class ProjectActivitiesController(
    IProjectActivityService activityService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var activities = await activityService.GetByProjectAsync(
            userId,
            projectId,
            cancellationToken);

        if (activities is null)
        {
            return NotFound(new
            {
                message = "Project was not found."
            });
        }

        return Ok(activities);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }
}
