namespace UniPilot.API.Contracts.Requirements;

public sealed class UpdateProjectRequirementRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public string Priority { get; init; } = string.Empty;
}