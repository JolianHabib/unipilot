namespace UniPilot.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } =
        null!;

    public NotificationType Type { get; set; } =
        NotificationType.Info;

    public string Title { get; set; } =
        string.Empty;

    public string Message { get; set; } =
        string.Empty;

    public string? ActionUrl { get; set; }

    public string? DeduplicationKey {
        get;
        set;
    }

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; } =
        DateTime.UtcNow;
}