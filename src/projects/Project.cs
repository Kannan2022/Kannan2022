namespace NotificationAuditService.Projects;

/// <summary>
/// Domain entity representing a project. Every project is owned by exactly one organisation
/// (the tenant boundary) and belongs to a team within that organisation.
/// </summary>
public class Project
{
    /// <summary>Database-generated primary key.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Identifier of the owning organisation. This is the multi-tenant isolation key and is always
    /// derived from the authenticated principal — never from client-supplied input.
    /// </summary>
    public required string OrganizationId { get; set; }

    /// <summary>Human-readable project name (1–255 characters).</summary>
    public required string Name { get; set; }

    /// <summary>Optional free-text description (up to 2000 characters).</summary>
    public string? Description { get; set; }

    /// <summary>Current lifecycle status of the project.</summary>
    public ProjectStatus Status { get; set; }

    /// <summary>Identifier of the owning team within the organisation.</summary>
    public required string TeamId { get; set; }

    /// <summary>UTC timestamp when the project was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the most recent update, or <c>null</c> if never updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}
