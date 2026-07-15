namespace NotificationAuditService.Projects;

/// <summary>
/// Data-access abstraction for <see cref="Project"/> entities. Every read and write is scoped to an
/// organisation so that persistence is the single, consistent enforcement point for tenant isolation.
/// </summary>
public interface IProjectRepository
{
    /// <summary>Persists a new project.</summary>
    /// <param name="project">The project to add; its <see cref="Project.OrganizationId"/> must already be set.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The persisted project, including its generated <see cref="Project.Id"/>.</returns>
    Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a single project by id, scoped to the given organisation.</summary>
    /// <param name="id">The project identifier.</param>
    /// <param name="organizationId">The owning organisation; projects in other organisations are never returned.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The tracked project, or <c>null</c> if it does not exist within the organisation.</returns>
    Task<Project?> GetByIdAsync(int id, string organizationId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a page of projects for a team within an organisation, newest first.</summary>
    /// <param name="organizationId">The owning organisation.</param>
    /// <param name="teamId">The owning team.</param>
    /// <param name="status">Optional status filter; when provided, only projects in that status are returned.</param>
    /// <param name="skip">Number of records to skip (for paging).</param>
    /// <param name="take">Maximum number of records to return.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The matching projects (empty when none match).</returns>
    Task<IReadOnlyList<Project>> GetByTeamAsync(
        string organizationId,
        string teamId,
        ProjectStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Persists changes made to an existing, organisation-scoped project.</summary>
    /// <param name="project">The project to update.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);

    /// <summary>Removes a project.</summary>
    /// <param name="project">The project to remove.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task RemoveAsync(Project project, CancellationToken cancellationToken = default);
}
