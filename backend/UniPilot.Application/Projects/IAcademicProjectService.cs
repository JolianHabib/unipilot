namespace UniPilot.Application.Projects;

public interface IAcademicProjectService
{
    Task<AcademicProjectResult?> CreateAsync(
        CreateAcademicProjectCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicProjectResult>?>
        GetByCourseAsync(
            Guid ownerId,
            Guid courseId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AcademicProjectResult>>
        GetByOwnerAsync(
            Guid ownerId,
            CancellationToken cancellationToken = default);

    Task<AcademicProjectResult?> UpdateAsync(
        UpdateAcademicProjectCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid ownerId,
        Guid academicProjectId,
        CancellationToken cancellationToken = default);
}