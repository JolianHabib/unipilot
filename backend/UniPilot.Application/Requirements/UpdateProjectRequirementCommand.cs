using UniPilot.Domain.Requirements;

namespace UniPilot.Application.Requirements;

public sealed record UpdateProjectRequirementCommand(
    string Title,
    string Description,
    RequirementType Type,
    RequirementPriority Priority);