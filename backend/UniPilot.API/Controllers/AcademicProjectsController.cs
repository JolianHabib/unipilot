using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.Projects;
using UniPilot.Application.Projects;
using UniPilot.Domain.Entities;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route(
    "api/courses/{courseId:guid}/projects")]
public sealed class AcademicProjectsController(
    IAcademicProjectService projectService)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid courseId,
        [FromBody]
        CreateAcademicProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(
                request.Title))
        {
            return BadRequest(new
            {
                message =
                    "Project title is required."
            });
        }

        var command =
            new CreateAcademicProjectCommand(
                userId,
                courseId,
                request.Title,
                request.Description,
                request.DueDateUtc);

        var project =
            await projectService.CreateAsync(
                command,
                cancellationToken);

        if (project is null)
        {
            return NotFound(new
            {
                message =
                    "Course was not found."
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
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var projects =
            await projectService
                .GetByCourseAsync(
                    userId,
                    courseId,
                    cancellationToken);

        if (projects is null)
        {
            return NotFound(new
            {
                message =
                    "Course was not found."
            });
        }

        return Ok(projects);
    }

    [HttpPut("{projectId:guid}")]
    public async Task<IActionResult> Update(
        Guid courseId,
        Guid projectId,
        [FromBody]
        UpdateAcademicProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(
                request.Title))
        {
            return BadRequest(new
            {
                message =
                    "Project title is required."
            });
        }

        if (
            !Enum.TryParse<
                AcademicProjectStatus>(
                request.Status,
                true,
                out var status) ||
            !Enum.IsDefined(status)
        )
        {
            return BadRequest(new
            {
                message =
                    "Project status is invalid."
            });
        }

        var command =
            new UpdateAcademicProjectCommand(
                userId,
                courseId,
                projectId,
                request.Title,
                request.Description,
                request.DueDateUtc,
                status);

        var project =
            await projectService.UpdateAsync(
                command,
                cancellationToken);

        if (project is null)
        {
            return NotFound(new
            {
                message =
                    "Project was not found."
            });
        }

        return Ok(project);
    }

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> Delete(
        Guid courseId,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var projects =
            await projectService
                .GetByCourseAsync(
                    userId,
                    courseId,
                    cancellationToken);

        if (
            projects is null ||
            !projects.Any(project =>
                project.Id == projectId)
        )
        {
            return NotFound(new
            {
                message =
                    "Project was not found."
            });
        }

        var deleted =
            await projectService.DeleteAsync(
                userId,
                projectId,
                cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message =
                    "Project was not found."
            });
        }

        return NoContent();
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