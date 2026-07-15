using System.ComponentModel.DataAnnotations;

namespace NotificationAuditService.Projects.Contracts;

/// <summary>
/// Request payload for creating a project. Note that the owning organisation is intentionally
/// absent — it is resolved from the authenticated principal so a caller cannot create a project
/// in another tenant's organisation.
/// </summary>
public class CreateProjectRequest
{
    /// <summary>Human-readable project name (1–255 characters).</summary>
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters.")]
    public required string Name { get; set; }

    /// <summary>Optional free-text description (up to 2000 characters).</summary>
    [StringLength(2000, ErrorMessage = "Description must be at most 2000 characters.")]
    public string? Description { get; set; }

    /// <summary>Identifier of the owning team within the caller's organisation.</summary>
    [Required(ErrorMessage = "TeamId is required.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "TeamId must be between 1 and 255 characters.")]
    public required string TeamId { get; set; }
}
