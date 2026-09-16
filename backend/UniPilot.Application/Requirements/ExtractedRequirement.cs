using UniPilot.Domain.Requirements;

namespace UniPilot.Application.Requirements;

public sealed record ExtractedRequirement(
    string Title,
    string Description,
    RequirementType Type,
    RequirementPriority Priority,
    int? SourcePageNumber);