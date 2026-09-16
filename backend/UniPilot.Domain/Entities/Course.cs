namespace UniPilot.Domain.Entities;

public sealed class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public Guid OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<AcademicProject> Projects { get; set; } =
    new List<AcademicProject>();
}