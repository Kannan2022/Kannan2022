namespace NotificationAuditService.Models;

public class Project
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string TeamId { get; set; }
    public string? TeamName { get; set; }
    public required string Status { get; set; } // Active, Inactive, OnHold, Completed, Archived
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int? BudgetInCents { get; set; } // Stored in cents for precision
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Priority { get; set; } // 1=Low, 2=Medium, 3=High, 4=Critical
}
