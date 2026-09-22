namespace UniPilot.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationResult>>
        GetAllAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

    Task CreateAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? actionUrl = null,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}