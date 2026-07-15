using Microsoft.EntityFrameworkCore;
using NotificationAuditService.Data;

namespace NotificationAuditService.Projects;

/// <summary>
/// Entity Framework Core implementation of <see cref="IProjectRepository"/>. All queries filter by
/// <see cref="Project.OrganizationId"/>, so no caller of this repository can reach another tenant's data.
/// </summary>
public class ProjectRepository : IProjectRepository
{
    private readonly AuditDbContext _context;

    public ProjectRepository(AuditDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync(cancellationToken);
        return project;
    }

    /// <inheritdoc />
    public async Task<Project?> GetByIdAsync(int id, string organizationId, CancellationToken cancellationToken = default)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == organizationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Project>> GetByTeamAsync(
        string organizationId,
        string teamId,
        ProjectStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.TeamId == teamId);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
