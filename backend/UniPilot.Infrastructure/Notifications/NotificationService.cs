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
        await CreateDueDateNotificationsAsync(
            userId,
            cancellationToken);

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

        foreach (
            var notification
            in notifications)
        {
            notification.IsRead = true;
        }

        if (notifications.Count > 0)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
    }

    private async Task
        CreateDueDateNotificationsAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;

        var projects =
            await dbContext.AcademicProjects
                .AsNoTracking()
                .Where(project =>
                    project.Course.OwnerId ==
                        userId &&
                    project.DueDateUtc != null &&
                    project.Status !=
                        AcademicProjectStatus
                            .Completed &&
                    project.Status !=
                        AcademicProjectStatus
                            .Archived)
                .Select(project => new
                {
                    project.Id,
                    project.Title,
                    DueDate =
                        project.DueDateUtc!
                            .Value
                            .Date
                })
                .ToListAsync(
                    cancellationToken);

        foreach (var project in projects)
        {
            var daysRemaining =
                (project.DueDate - today)
                    .Days;

            var reminder =
                CreateDueDateReminder(
                    project.Id,
                    project.Title,
                    project.DueDate,
                    daysRemaining);

            if (reminder is null)
            {
                continue;
            }

            var alreadyExists =
                await dbContext.Notifications
                    .AnyAsync(
                        notification =>
                            notification.UserId ==
                                userId &&
                            notification
                                .DeduplicationKey ==
                                reminder.Key,
                        cancellationToken);

            if (alreadyExists)
            {
                continue;
            }

            dbContext.Notifications.Add(
    new Notification
    {
        UserId = userId,
        Type = reminder.Type,
        Title = reminder.Title,
        Message = reminder.Message,
        ActionUrl =
            $"project:{project.Id}",
        DeduplicationKey =
            reminder.Key
    });
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static DueDateReminder?
        CreateDueDateReminder(
            Guid projectId,
            string projectTitle,
            DateTime dueDate,
            int daysRemaining)
    {
        var dateKey =
            dueDate.ToString("yyyyMMdd");

        if (daysRemaining < 0)
        {
            return new DueDateReminder(
                $"project-{projectId}-due-{dateKey}-overdue",
                NotificationType.Error,
                "Project overdue",
                $"“{projectTitle}” was due {Math.Abs(daysRemaining)} day(s) ago.");
        }

        if (daysRemaining <= 1)
        {
            var message =
                daysRemaining == 0
                    ? $"“{projectTitle}” is due today."
                    : $"“{projectTitle}” is due tomorrow.";

            return new DueDateReminder(
                $"project-{projectId}-due-{dateKey}-one-day",
                NotificationType.Error,
                "Project deadline is very close",
                message);
        }

        if (daysRemaining <= 3)
        {
            return new DueDateReminder(
                $"project-{projectId}-due-{dateKey}-three-days",
                NotificationType.Warning,
                "Project deadline approaching",
                $"“{projectTitle}” is due in {daysRemaining} days.");
        }

        if (daysRemaining <= 7)
        {
            return new DueDateReminder(
                $"project-{projectId}-due-{dateKey}-seven-days",
                NotificationType.Info,
                "Upcoming project deadline",
                $"“{projectTitle}” is due in {daysRemaining} days.");
        }

        return null;
    }

    private sealed record DueDateReminder(
        string Key,
        NotificationType Type,
        string Title,
        string Message);
}