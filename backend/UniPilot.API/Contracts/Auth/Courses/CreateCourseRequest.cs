using System.ComponentModel.DataAnnotations;

namespace UniPilot.API.Contracts.Courses;

public sealed record CreateCourseRequest(
    [Required]
    [StringLength(150, MinimumLength = 2)]
    string Name,

    [StringLength(50)]
    string? Code,

    [StringLength(1000)]
    string? Description);