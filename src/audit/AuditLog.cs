namespace NotificationAuditService.Audit;

public class AuditLog
{
    public int Id { get; set; }
    public required string EntityName { get; set; }
    public int EntityId { get; set; }
    public required string Action { get; set; } // Create, Update, Delete
    public string? OldValues { get; set; } // JSON serialized
    public string? NewValues { get; set; } // JSON serialized
    public required string UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}