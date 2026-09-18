using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.Tasks;
using UniPilot.Application.Tasks;
using UniPilot.Domain.Tasks;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/tasks")]
public sealed class ProjectTasksController(
    IProjectTaskService projectTaskService)
    : ControllerBase
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

        var tasks =
            await projectTaskService.GetByProjectAsync(
                userId,
                projectId,
                cancellationToken);

        if (tasks is null)
        {
            return NotFound(new
            {
                message = "Project was not found."
            });
        }

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid projectId,
        [FromBody] CreateProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Title is required."
            });
        }

        if (!Enum.TryParse<ProjectTaskPriority>(
                request.Priority,
                true,
                out var priority))
        {
            return BadRequest(new
            {
                message =
                    "Priority must be Low, Medium, or High."
            });
        }

        var command = new CreateProjectTaskCommand(
            userId,
            projectId,
            request.ProjectRequirementId,
            request.Title,
            request.Description,
            priority,
            request.DueDateUtc);

        try
        {
            var projectTask =
                await projectTaskService.CreateAsync(
                    command,
                    cancellationToken);

            if (projectTask is null)
            {
                return NotFound(new
                {
                    message = "Project was not found."
                });
            }

            return StatusCode(
                StatusCodes.Status201Created,
                projectTask);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPut("{taskId:guid}")]
    public async Task<IActionResult> Update(
        Guid projectId,
        Guid taskId,
        [FromBody] UpdateProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new
            {
                message = "Title is required."
            });
        }

        if (!Enum.TryParse<ProjectTaskPriority>(
                request.Priority,
                true,
                out var priority))
        {
            return BadRequest(new
            {
                message =
                    "Priority must be Low, Medium, or High."
            });
        }

        var command = new UpdateProjectTaskCommand(
            userId,
            projectId,
            taskId,
            request.ProjectRequirementId,
            request.Title,
            request.Description,
            priority,
            request.DueDateUtc);

        try
        {
            var projectTask =
                await projectTaskService.UpdateAsync(
                    command,
                    cancellationToken);

            if (projectTask is null)
            {
                return NotFound(new
                {
                    message = "Task was not found."
                });
            }

            return Ok(projectTask);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }

    [HttpPatch("{taskId:guid}/move")]
    public async Task<IActionResult> Move(
        Guid projectId,
        Guid taskId,
        [FromBody] MoveProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        if (!Enum.TryParse<ProjectTaskStatus>(
                request.Status,
                true,
                out var status))
        {
            return BadRequest(new
            {
                message =
                    "Status must be ToDo, InProgress, or Done."
            });
        }

        if (request.Position < 0)
        {
            return BadRequest(new
            {
                message =
                    "Position cannot be negative."
            });
        }

        var command = new MoveProjectTaskCommand(
            userId,
            projectId,
            taskId,
            status,
            request.Position);

        var projectTask =
            await projectTaskService.MoveAsync(
                command,
                cancellationToken);

        if (projectTask is null)
        {
            return NotFound(new
            {
                message = "Task was not found."
            });
        }

        return Ok(projectTask);
    }

    [HttpDelete("{taskId:guid}")]
    public async Task<IActionResult> Delete(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var deleted =
            await projectTaskService.DeleteAsync(
                userId,
                projectId,
                taskId,
                cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Task was not found."
            });
        }

        return NoContent();
    }

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out userId);
    }
}