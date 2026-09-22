namespace UniPilot.API.Contracts.Projects;

public sealed class UpdateAcademicProjectRequest
{
    public string Title { get; set; } =
        string.Empty;

    public string? Description { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public string Status { get; set; } =
        "Draft";
}