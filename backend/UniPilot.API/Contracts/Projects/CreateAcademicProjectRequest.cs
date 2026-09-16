using System.ComponentModel.DataAnnotations;

namespace UniPilot.API.Contracts.Projects;

public sealed record CreateAcademicProjectRequest(
    [Required]
    [StringLength(200, MinimumLength = 2)]
    string Title,

    [StringLength(2000)]
    string? Description,

    DateTime? DueDateUtc);