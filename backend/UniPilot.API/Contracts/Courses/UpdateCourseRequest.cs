namespace UniPilot.API.Contracts.Courses;

public sealed class UpdateCourseRequest
{
    public string Name { get; set; } =
        string.Empty;

    public string? Code { get; set; }

    public string? Description { get; set; }
}