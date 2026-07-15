using Microsoft.EntityFrameworkCore;
using NotificationAuditService.Data;
using NotificationAuditService.Models;

namespace NotificationAuditService.Services;

public class AuditService : IAuditService
{
    private readonly AuditDbContext _context;

    public AuditService(AuditDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> LogChangeAsync(string entityName, int entityId, string action, string? oldValues = null, string? newValues = null, string? userId = null, string? userName = null, string? ipAddress = null, string? userAgent = null)
    {
        var auditLog = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = userId ?? "System",
            UserName = userName,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
        return auditLog;
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string entityName, int? entityId = null, string? action = null, int? days = null)
    {
        var query = _context.AuditLogs.Where(a => a.EntityName == entityName);

        if (entityId.HasValue)
        {
            query = query.Where(a => a.EntityId == entityId);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (days.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days.Value);
            query = query.Where(a => a.Timestamp >= cutoffDate);
        }

        return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByUserAsync(string userId, int? days = null)
    {
        var query = _context.AuditLogs.Where(a => a.UserId == userId);

        if (days.HasValue)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days.Value);
            query = query.Where(a => a.Timestamp >= cutoffDate);
        }

        return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetAuditLogsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.AuditLogs
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<AuditLog?> GetAuditLogByIdAsync(int id)
    {
        return await _context.AuditLogs.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<int> DeleteOldAuditLogsAsync(int daysOld)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
        var oldLogs = await _context.AuditLogs
            .Where(a => a.Timestamp < cutoffDate)
            .ToListAsync();

        _context.AuditLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();
        return oldLogs.Count;
    }
}
