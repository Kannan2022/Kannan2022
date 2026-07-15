namespace NotificationAuditService.Projects.Contracts;

/// <summary>
/// API response contract representing a project. Decoupled from the <see cref="Project"/> entity so
/// that internal storage changes do not leak into the public API surface.
/// </summary>
public class ProjectResponse
{
    /// <summary>Unique project identifier.</summary>
    public int Id { get; set; }

    /// <summary>Identifier of the owning organisation (the caller's own tenant).</summary>
    public required string OrganizationId { get; set; }

    /// <summary>Human-readable project name.</summary>
    public required string Name { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Current lifecycle status (serialised as its name, e.g. <c>"InProgress"</c>).</summary>
    public ProjectStatus Status { get; set; }

    /// <summary>Identifier of the owning team.</summary>
    public required string TeamId { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the last update, if any.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Maps a <see cref="Project"/> entity to its API response representation.</summary>
    /// <param name="project">The entity to map.</param>
    /// <returns>A populated <see cref="ProjectResponse"/>.</returns>
    public static ProjectResponse FromEntity(Project project) => new()
    {
        Id = project.Id,
        OrganizationId = project.OrganizationId,
        Name = project.Name,
        Description = project.Description,
        Status = project.Status,
        TeamId = project.TeamId,
        CreatedAt = project.CreatedAt,
        UpdatedAt = project.UpdatedAt
    };
}
