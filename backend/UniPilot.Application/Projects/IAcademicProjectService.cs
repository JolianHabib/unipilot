namespace UniPilot.Application.Projects;

public interface IAcademicProjectService
{
    Task<AcademicProjectResult?> CreateAsync(
        CreateAcademicProjectCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicProjectResult>?> GetByCourseAsync(
        Guid ownerId,
        Guid courseId,
        CancellationToken cancellationToken = default);
}