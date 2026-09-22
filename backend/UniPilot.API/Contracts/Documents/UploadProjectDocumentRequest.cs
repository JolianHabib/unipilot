using System.ComponentModel.DataAnnotations;
using UniPilot.Domain.Entities;

namespace UniPilot.API.Contracts.Documents;

public sealed class UploadProjectDocumentRequest
{
    [Required]
    public IFormFile File { get; init; } = null!;

    public ProjectDocumentType DocumentType { get; init; } =
        ProjectDocumentType.Other;
}