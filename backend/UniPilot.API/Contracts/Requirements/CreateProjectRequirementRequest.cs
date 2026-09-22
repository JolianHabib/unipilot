namespace UniPilot.API.Contracts.Requirements;

public sealed class CreateProjectRequirementRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;
}
