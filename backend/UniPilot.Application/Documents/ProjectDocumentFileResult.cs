namespace UniPilot.Application.Documents;

public sealed record ProjectDocumentFileResult(
    Stream Content,
    string ContentType,
    string FileName);