using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.Projects;
using UniPilot.Application.Projects;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/courses/{courseId:guid}/projects")]
public sealed class AcademicProjectsController(
    IAcademicProjectService projectService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid courseId,
        [FromBody] CreateAcademicProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var command = new CreateAcademicProjectCommand(
            userId,
            courseId,
            request.Title,
            request.Description,
            request.DueDateUtc);

        var project = await projectService.CreateAsync(
            command,
            cancellationToken);

        if (project is null)
        {
            return NotFound(new
            {
                message = "Course was not found."
            });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            project);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid courseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var projects = await projectService.GetByCourseAsync(
            userId,
            courseId,
            cancellationToken);

        if (projects is null)
        {
            return NotFound(new
            {
                message = "Course was not found."
            });
        }

        return Ok(projects);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(value, out userId);
    }
}