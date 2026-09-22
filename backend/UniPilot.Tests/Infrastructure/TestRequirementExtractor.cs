using UniPilot.Application.Requirements;
using UniPilot.Domain.Requirements;

namespace UniPilot.Tests.Infrastructure;

public sealed class TestRequirementExtractor
    : IRequirementExtractor
{
    public Task<IReadOnlyList<ExtractedRequirement>>
        ExtractAsync(
            IReadOnlyList<RequirementSourcePage> pages,
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExtractedRequirement> requirements =
        [
            new ExtractedRequirement(
                "Implement authentication",
                "The system must support user authentication.",
                RequirementType.Functional,
                RequirementPriority.High,
                pages.FirstOrDefault()?.PageNumber ?? 1)
        ];

        return Task.FromResult(requirements);
    }
}