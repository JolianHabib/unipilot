using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.Requirements;
using UniPilot.Application.Requirements;
using UniPilot.Domain.Requirements;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class ProjectRequirementsController(
    IProjectRequirementService requirementService)
    : ControllerBase
{
    [HttpPost(
        "documents/{documentId:guid}/requirements/extract")]
    public async Task<IActionResult> Extract(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        try
        {
            var requirements =
                await requirementService
                    .ExtractFromDocumentAsync(
                        ownerId,
                        documentId,
                        cancellationToken);

            if (requirements is null)
            {
                return NotFound();
            }

            return Ok(requirements);
        }
        catch (InvalidOperationException exception)
            when (
                exception.Message ==
                "The document is not ready for requirement extraction.")
        {
            return Conflict(
                new
                {
                    message = exception.Message
                });
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = exception.Message
                });
        }
    }

    [HttpGet(
        "projects/{projectId:guid}/requirements")]
    public async Task<IActionResult> GetByProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var requirements =
            await requirementService.GetByProjectAsync(
                ownerId,
                projectId,
                cancellationToken);

        if (requirements is null)
        {
            return NotFound();
        }

        return Ok(requirements);
    }

    [HttpPatch(
        "requirements/{requirementId:guid}")]
    public async Task<IActionResult> SetCompletion(
        Guid requirementId,
        SetRequirementCompletionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var requirement =
            await requirementService.SetCompletionAsync(
                ownerId,
                requirementId,
                request.IsCompleted,
                cancellationToken);

        if (requirement is null)
        {
            return NotFound();
        }

        return Ok(requirement);
    }

    [HttpDelete(
        "requirements/{requirementId:guid}")]
    public async Task<IActionResult> Delete(
        Guid requirementId,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        var deleted =
            await requirementService.DeleteAsync(
                ownerId,
                requirementId,
                cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut(
        "requirements/{requirementId:guid}")]
    public async Task<IActionResult> Update(
        Guid requirementId,
        UpdateProjectRequirementRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(
                request.Title))
        {
            return BadRequest(
                new
                {
                    message = "Title is required."
                });
        }

        if (string.IsNullOrWhiteSpace(
                request.Description))
        {
            return BadRequest(
                new
                {
                    message =
                        "Description is required."
                });
        }

        if (!Enum.TryParse<RequirementType>(
                request.Type,
                ignoreCase: true,
                out var type))
        {
            return BadRequest(
                new
                {
                    message =
                        "Invalid requirement type."
                });
        }

        if (!Enum.TryParse<RequirementPriority>(
                request.Priority,
                ignoreCase: true,
                out var priority))
        {
            return BadRequest(
                new
                {
                    message =
                        "Invalid requirement priority."
                });
        }

        var command =
            new UpdateProjectRequirementCommand(
                request.Title,
                request.Description,
                type,
                priority);

        var requirement =
            await requirementService.UpdateAsync(
                ownerId,
                requirementId,
                command,
                cancellationToken);

        if (requirement is null)
        {
            return NotFound();
        }

        return Ok(requirement);
    }

    private bool TryGetOwnerId(
        out Guid ownerId)
    {
        var ownerIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            ownerIdValue,
            out ownerId);
    }
}