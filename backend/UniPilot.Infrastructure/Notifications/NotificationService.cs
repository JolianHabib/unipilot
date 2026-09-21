using Microsoft.EntityFrameworkCore;
using UniPilot.Application.Notifications;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;

namespace UniPilot.Infrastructure.Notifications;

public sealed class NotificationService(
    AppDbContext dbContext)
    : INotificationService
{
    public async Task<
        IReadOnlyList<NotificationResult>>
        GetAllAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.UserId == userId)
            .OrderByDescending(notification =>
                notification.CreatedAtUtc)
            .Take(50)
            .Select(notification =>
                new NotificationResult(
                    notification.Id,
                    notification.Type.ToString(),
                    notification.Title,
                    notification.Message,
                    notification.ActionUrl,
                    notification.IsRead,
                    notification.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(
        Guid userId,
        string type,
        string title,
        string message,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<NotificationType>(
                type,
                true,
                out var notificationType))
        {
            notificationType =
                NotificationType.Info;
        }

        var notification =
            new Notification
            {
                UserId = userId,
                Type = notificationType,
                Title = title.Trim(),
                Message = message.Trim(),
                ActionUrl =
                    string.IsNullOrWhiteSpace(
                        actionUrl)
                        ? null
                        : actionUrl.Trim()
            };

        dbContext.Notifications.Add(
            notification);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification =
            await dbContext.Notifications
                .SingleOrDefaultAsync(
                    item =>
                        item.Id ==
                            notificationId &&
                        item.UserId == userId,
                    cancellationToken);

        if (notification is null)
        {
            return false;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return true;
    }

    public async Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var notifications =
            await dbContext.Notifications
                .Where(notification =>
                    notification.UserId ==
                        userId &&
                    !notification.IsRead)
                .ToListAsync(
                    cancellationToken);

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        if (notifications.Count > 0)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
    }
}