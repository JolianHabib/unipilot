namespace UniPilot.Domain.Entities;

public sealed class ProjectDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AcademicProjectId { get; set; }

    public AcademicProject AcademicProject { get; set; } = null!;

    public string OriginalFileName { get; set; } = string.Empty;

    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } =
        "application/pdf";

    public long FileSizeBytes { get; set; }

    public string ContentHash { get; set; } = string.Empty;

    public ProjectDocumentType DocumentType { get; set; }

    public DocumentProcessingStatus ProcessingStatus { get; set; } =
        DocumentProcessingStatus.Uploaded;

    public int PageCount { get; set; }

    public string? FailureReason { get; set; }

    public DateTime UploadedAtUtc { get; set; } =
        DateTime.UtcNow;

    public ICollection<DocumentPage> Pages { get; set; } =
        new List<DocumentPage>();

    public ICollection<ProjectRequirement> Requirements { get; set; } = new List<ProjectRequirement>();
}