using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.Application.Requirements;

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
        {
            return Conflict(
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