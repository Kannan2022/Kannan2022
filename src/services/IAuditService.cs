using NotificationAuditService.Models;

namespace NotificationAuditService.Services;

public interface IAuditService
{
    Task<AuditLog> LogChangeAsync(string entityName, int entityId, string action, string? oldValues = null, string? newValues = null, string? userId = null, string? userName = null, string? ipAddress = null, string? userAgent = null);
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string entityName, int? entityId = null, string? action = null, int? days = null);
    Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId, int? days = null);
    Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<AuditLog?> GetAuditLogByIdAsync(int id);
    Task<int> DeleteOldAuditLogsAsync(int daysOld);
}
