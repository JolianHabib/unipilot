namespace UniPilot.Domain.Entities;

public sealed class DocumentPage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectDocumentId { get; set; }

    public ProjectDocument ProjectDocument { get; set; } = null!;

    public int PageNumber { get; set; }

    public string Text { get; set; } = string.Empty;
}