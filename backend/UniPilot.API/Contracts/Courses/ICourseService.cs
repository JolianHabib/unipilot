namespace UniPilot.Application.Courses;

public interface ICourseService
{
    Task<CourseResult> CreateAsync(
        CreateCourseCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CourseResult>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<CourseResult?> UpdateAsync(
        UpdateCourseCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid ownerId,
        Guid courseId,
        CancellationToken cancellationToken = default);
}