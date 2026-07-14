namespace NotificationAuditService.Services;

using Microsoft.EntityFrameworkCore;
using NotificationAuditService.Data;
using NotificationAuditService.Models;

public class ProjectService : IProjectService
{
    private readonly AuditDbContext _context;

    public ProjectService(AuditDbContext context)
    {
        _context = context;
    }

    public async Task<Project> CreateProjectAsync(string name, string teamId, string? description = null, string? teamName = null, int priority = 2, int? budgetInCents = null, DateTime? startDate = null, DateTime? endDate = null, string? createdBy = null)
    {
        var project = new Project
        {
            Name = name,
            TeamId = teamId,
            TeamName = teamName,
            Description = description,
            Status = "Active",
            Priority = priority,
            BudgetInCents = budgetInCents,
            StartDate = startDate,
            EndDate = endDate,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy ?? "System"
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        return await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Project>> GetProjectsByTeamAsync(string teamId)
    {
        return await _context.Projects
            .Where(p => p.TeamId == teamId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Project?> UpdateProjectAsync(int id, string? name = null, string? description = null, int? priority = null, int? budgetInCents = null, DateTime? startDate = null, DateTime? endDate = null, string? updatedBy = null)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null)
            return null;

        if (!string.IsNullOrEmpty(name))
            project.Name = name;

        if (description != null)
            project.Description = description;

        if (priority.HasValue)
            project.Priority = priority.Value;

        if (budgetInCents.HasValue)
            project.BudgetInCents = budgetInCents.Value;

        if (startDate.HasValue)
            project.StartDate = startDate.Value;

        if (endDate.HasValue)
            project.EndDate = endDate.Value;

        project.UpdatedAt = DateTime.UtcNow;
        project.UpdatedBy = updatedBy ?? "System";

        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<Project?> UpdateProjectStatusAsync(int id, string status, string? updatedBy = null)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null)
            return null;

        project.Status = status;
        project.UpdatedAt = DateTime.UtcNow;
        project.UpdatedBy = updatedBy ?? "System";

        await _context.SaveChangesAsync();
        return project;
    }

    public async Task<bool> DeleteProjectAsync(int id)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null)
            return false;

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Project>> GetProjectsByStatusAsync(string status)
    {
        return await _context.Projects
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
