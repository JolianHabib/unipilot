namespace UniPilot.Application.Requirements;

public interface IRequirementExtractor
{
    Task<IReadOnlyList<ExtractedRequirement>> ExtractAsync(
        IReadOnlyList<RequirementSourcePage> pages,
        CancellationToken cancellationToken = default);
}