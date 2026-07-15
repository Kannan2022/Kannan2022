namespace NotificationAuditService.Projects;

/// <summary>
/// Business-logic implementation of <see cref="IProjectService"/>. Validates input, applies domain
/// rules, emits structured logs, and delegates all persistence to <see cref="IProjectRepository"/>.
/// </summary>
public class ProjectService : IProjectService
{
    /// <summary>Maximum page size a caller may request from <see cref="GetProjectsByTeamAsync"/>.</summary>
    private const int MAX_PAGE_SIZE = 100;

    /// <summary>Maximum allowed length for the project name.</summary>
    private const int MAX_NAME_LENGTH = 255;

    /// <summary>Maximum allowed length for the project description.</summary>
    private const int MAX_DESCRIPTION_LENGTH = 2000;

    private readonly IProjectRepository _repository;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(IProjectRepository repository, ILogger<ProjectService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Project> CreateProjectAsync(
        string organizationId,
        string name,
        string teamId,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        RequireOrganizationId(organizationId);
        RequireNonEmpty(name, nameof(name), MAX_NAME_LENGTH);
        RequireNonEmpty(teamId, nameof(teamId), MAX_NAME_LENGTH);
        if (description is { Length: > MAX_DESCRIPTION_LENGTH })
            throw new ArgumentException($"Description must be at most {MAX_DESCRIPTION_LENGTH} characters.", nameof(description));

        _logger.LogInformation(
            "Creating project for organisation {OrganizationId}, team {TeamId}", organizationId, teamId);

        var project = new Project
        {
            OrganizationId = organizationId,
            Name = name,
            Description = description,
            TeamId = teamId,
            Status = ProjectStatus.NotStarted,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(project, cancellationToken);

        _logger.LogInformation(
            "Project {ProjectId} created for organisation {OrganizationId}", project.Id, organizationId);
        return project;
    }

    /// <inheritdoc />
    public async Task<Project?> UpdateProjectStatusAsync(
        string organizationId,
        int id,
        ProjectStatus status,
        CancellationToken cancellationToken = default)
    {
        RequireOrganizationId(organizationId);
        if (!Enum.IsDefined(status))
            throw new ArgumentException($"'{status}' is not a valid project status.", nameof(status));

        var project = await _repository.GetByIdAsync(id, organizationId, cancellationToken);
        if (project is null)
        {
            _logger.LogWarning(
                "Project {ProjectId} not found for organisation {OrganizationId} on status update",
                id, organizationId);
            return null;
        }

        project.Status = status;
        project.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(project, cancellationToken);

        _logger.LogInformation(
            "Project {ProjectId} status updated to {Status} for organisation {OrganizationId}",
            id, status, organizationId);
        return project;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Project>> GetProjectsByTeamAsync(
        string organizationId,
        string teamId,
        ProjectStatus? status = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        RequireOrganizationId(organizationId);
        RequireNonEmpty(teamId, nameof(teamId), MAX_NAME_LENGTH);

        var normalizedSkip = Math.Max(0, skip);
        var normalizedTake = Math.Clamp(take, 1, MAX_PAGE_SIZE);

        return await _repository.GetByTeamAsync(
            organizationId, teamId, status, normalizedSkip, normalizedTake, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProjectAsync(
        string organizationId,
        int id,
        CancellationToken cancellationToken = default)
    {
        RequireOrganizationId(organizationId);

        var project = await _repository.GetByIdAsync(id, organizationId, cancellationToken);
        if (project is null)
        {
            _logger.LogWarning(
                "Project {ProjectId} not found for organisation {OrganizationId} on deletion",
                id, organizationId);
            return false;
        }

        await _repository.RemoveAsync(project, cancellationToken);

        _logger.LogInformation(
            "Project {ProjectId} deleted for organisation {OrganizationId}", id, organizationId);
        return true;
    }

    /// <summary>Guards that the tenant organisation id is present.</summary>
    private static void RequireOrganizationId(string organizationId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            throw new ArgumentException("Organisation id is required.", nameof(organizationId));
    }

    /// <summary>Guards that a required string is present and within its maximum length.</summary>
    private static void RequireNonEmpty(string value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} is required.", paramName);
        if (value.Length > maxLength)
            throw new ArgumentException($"{paramName} must be at most {maxLength} characters.", paramName);
    }
}
