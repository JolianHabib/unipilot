using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.Application.Documents;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/documents/{documentId:guid}/pages")]
public sealed class DocumentPagesController(
    IProjectDocumentService documentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPages(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var pages = await documentService.GetPagesAsync(
            userId,
            documentId,
            cancellationToken);

        if (pages is null)
        {
            return NotFound(new
            {
                message = "Document was not found."
            });
        }

        return Ok(pages);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(value, out userId);
    }
}