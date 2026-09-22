namespace UniPilot.Application.Documents;

public sealed record ExtractedPdfPage(
    int PageNumber,
    string Text);