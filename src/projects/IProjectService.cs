namespace NotificationAuditService.Projects;

/// <summary>
/// Business operations for managing <see cref="Project"/> entities. Every operation is scoped to an
/// organisation (the tenant); callers must supply the organisation resolved from the authenticated
/// principal, never a value taken from client input.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Creates a new project for a team within the given organisation. The project starts in the
    /// <see cref="ProjectStatus.NotStarted"/> state.
    /// </summary>
    /// <param name="organizationId">The owning organisation (tenant), from the authenticated principal.</param>
    /// <param name="name">The project name (required, 1–255 characters).</param>
    /// <param name="teamId">The owning team identifier (required).</param>
    /// <param name="description">Optional description (up to 2000 characters).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The created project, including its generated identifier.</returns>
    /// <exception cref="ArgumentException">Thrown when any required argument is null, empty, or too long.</exception>
    Task<Project> CreateProjectAsync(
        string organizationId,
        string name,
        string teamId,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of a project owned by the organisation.
    /// </summary>
    /// <param name="organizationId">The owning organisation (tenant), from the authenticated principal.</param>
    /// <param name="id">The identifier of the project to update.</param>
    /// <param name="status">The new status to apply.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The updated project, or <c>null</c> if no matching project exists within the organisation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="organizationId"/> is missing or <paramref name="status"/> is undefined.</exception>
    Task<Project?> UpdateProjectStatusAsync(
        string organizationId,
        int id,
        ProjectStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of projects for a team within the organisation, most recently created first.
    /// </summary>
    /// <param name="organizationId">The owning organisation (tenant), from the authenticated principal.</param>
    /// <param name="teamId">The owning team identifier (required).</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="skip">Number of records to skip; negative values are treated as zero.</param>
    /// <param name="take">Page size; clamped to the range [1, 100].</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching projects (empty when none match).</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="organizationId"/> or <paramref name="teamId"/> is missing.</exception>
    Task<IReadOnlyList<Project>> GetProjectsByTeamAsync(
        string organizationId,
        string teamId,
        ProjectStatus? status = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a project owned by the organisation.
    /// </summary>
    /// <param name="organizationId">The owning organisation (tenant), from the authenticated principal.</param>
    /// <param name="id">The identifier of the project to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><c>true</c> if a project was deleted; <c>false</c> if no matching project exists within the organisation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="organizationId"/> is missing.</exception>
    Task<bool> DeleteProjectAsync(
        string organizationId,
        int id,
        CancellationToken cancellationToken = default);
}
