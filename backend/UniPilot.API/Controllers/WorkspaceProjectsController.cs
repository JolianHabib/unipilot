using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.Application.Projects;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public sealed class WorkspaceProjectsController(
    IAcademicProjectService projectService)
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

        var projects =
            await projectService
                .GetByOwnerAsync(
                    userId,
                    cancellationToken);

        return Ok(projects);
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