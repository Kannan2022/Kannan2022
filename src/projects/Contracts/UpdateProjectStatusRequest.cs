using System.ComponentModel.DataAnnotations;

namespace NotificationAuditService.Projects.Contracts;

/// <summary>Request payload for changing a project's status.</summary>
public class UpdateProjectStatusRequest
{
    /// <summary>The new status to apply. Accepts the enum name, e.g. <c>"InProgress"</c>.</summary>
    [Required(ErrorMessage = "Status is required.")]
    [EnumDataType(typeof(ProjectStatus), ErrorMessage = "Status must be one of: NotStarted, InProgress, OnHold, Completed, Cancelled.")]
    public ProjectStatus Status { get; set; }
}
