using NotificationAuditService.Models;

namespace NotificationAuditService.Services;

public interface IProjectService
{
    Task<Project> CreateProjectAsync(string name, string teamId, string? description = null, string? teamName = null, int priority = 2, int? budgetInCents = null, DateTime? startDate = null, DateTime? endDate = null, string? createdBy = null);
    Task<Project?> GetProjectByIdAsync(int id);
    Task<IEnumerable<Project>> GetProjectsByTeamAsync(string teamId);
    Task<Project?> UpdateProjectAsync(int id, string? name = null, string? description = null, int? priority = null, int? budgetInCents = null, DateTime? startDate = null, DateTime? endDate = null, string? updatedBy = null);
    Task<Project?> UpdateProjectStatusAsync(int id, string status, string? updatedBy = null);
    Task<bool> DeleteProjectAsync(int id);
    Task<IEnumerable<Project>> GetProjectsByStatusAsync(string status);
}
