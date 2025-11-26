namespace CoderamaOpsAI.Dal.Entities;

public class Notification
{
    public int Id { get; set; }
    public int? OrderId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Metadata { get; set; }  // JSON for userId, total, etc.
    public DateTime CreatedAt { get; set; }

    public Order? Order { get; set; }
}

public enum NotificationType
{
    OrderCompleted,
    OrderExpired
}
