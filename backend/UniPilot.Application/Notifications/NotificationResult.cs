namespace UniPilot.Application.Notifications;

public sealed record NotificationResult(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string? ActionUrl,
    bool IsRead,
    DateTime CreatedAtUtc);