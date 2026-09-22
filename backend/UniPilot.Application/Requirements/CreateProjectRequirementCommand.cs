using UniPilot.Domain.Requirements;

namespace UniPilot.Application.Requirements;

public sealed record CreateProjectRequirementCommand(
    string Title,
    string Description,
    RequirementType Type,
    RequirementPriority Priority);
