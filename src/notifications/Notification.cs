namespace NotificationAuditService.Notifications;

public class Notification
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string Type { get; set; } // Info, Warning, Error, Success
    public required string RecipientId { get; set; }
    public string? RecipientEmail { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}