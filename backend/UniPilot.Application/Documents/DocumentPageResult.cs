namespace UniPilot.Application.Documents;

public sealed record DocumentPageResult(
    int PageNumber,
    string Text);