using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPilot.API.Contracts.Documents;
using UniPilot.Application.Documents;

namespace UniPilot.API.Controllers;

[ApiController]
[Authorize]
[Route(
    "api/projects/{projectId:guid}/documents")]
public sealed class ProjectDocumentsController(
    IProjectDocumentService documentService)
    : ControllerBase
{
    private const long MaximumFileSize =
        10 * 1024 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(
        MultipartBodyLengthLimit =
            11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        Guid projectId,
        [FromForm]
        UploadProjectDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var file = request.File;

        if (file.Length == 0)
        {
            return BadRequest(new
            {
                message =
                    "The PDF file is empty."
            });
        }

        if (file.Length > MaximumFileSize)
        {
            return BadRequest(new
            {
                message =
                    "The PDF file cannot exceed 10 MB."
            });
        }

        var originalFileName =
            Path.GetFileName(file.FileName);

        if (
            originalFileName.Length > 255 ||
            !string.Equals(
                Path.GetExtension(
                    originalFileName),
                ".pdf",
                StringComparison
                    .OrdinalIgnoreCase)
        )
        {
            return BadRequest(new
            {
                message =
                    "Only PDF files are supported."
            });
        }

        await using var input =
            file.OpenReadStream();

        await using var content =
            new MemoryStream();

        await input.CopyToAsync(
            content,
            cancellationToken);

        if (!HasPdfSignature(content))
        {
            return BadRequest(new
            {
                message =
                    "The uploaded file is not a valid PDF."
            });
        }

        content.Position = 0;

        var command =
            new UploadProjectDocumentCommand(
                userId,
                projectId,
                originalFileName,
                "application/pdf",
                file.Length,
                request.DocumentType,
                content);

        var result =
            await documentService.UploadAsync(
                command,
                cancellationToken);

        return result.Status switch
        {
            UploadDocumentStatus.Success =>
                StatusCode(
                    StatusCodes
                        .Status201Created,
                    result.Document),

            UploadDocumentStatus.Duplicate =>
                Conflict(new
                {
                    message =
                        "This PDF was already uploaded to the project."
                }),

            _ => NotFound(new
            {
                message =
                    "Project was not found."
            })
        };
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var documents =
            await documentService
                .GetByProjectAsync(
                    userId,
                    projectId,
                    cancellationToken);

        if (documents is null)
        {
            return NotFound(new
            {
                message =
                    "Project was not found."
            });
        }

        return Ok(documents);
    }

    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Delete(
        Guid projectId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(
                out var userId))
        {
            return Unauthorized();
        }

        var deleted =
            await documentService.DeleteAsync(
                userId,
                projectId,
                documentId,
                cancellationToken);

        if (!deleted)
        {
            return NotFound(new
            {
                message =
                    "Document was not found."
            });
        }

        return NoContent();
    }

    private static bool HasPdfSignature(
        Stream content)
    {
        if (content.Length < 5)
        {
            return false;
        }

        content.Position = 0;

        Span<byte> signature =
            stackalloc byte[5];

        var bytesRead =
            content.Read(signature);

        content.Position = 0;

        return bytesRead == 5 &&
               signature.SequenceEqual(
                   new byte[]
                   {
                       0x25,
                       0x50,
                       0x44,
                       0x46,
                       0x2D
                   });
    }

    private bool TryGetCurrentUserId(
        out Guid userId)
    {
        var value =
            User.FindFirst(
                ClaimTypes.NameIdentifier)
                ?.Value;

        return Guid.TryParse(
            value,
            out userId);
    }
}