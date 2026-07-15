using Microsoft.AspNetCore.Mvc;
using NotificationAuditService.Models;
using NotificationAuditService.Services;

namespace NotificationAuditService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpPost]
    public async Task<ActionResult<AuditLog>> LogChange([FromBody] LogChangeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var auditLog = await _auditService.LogChangeAsync(
            request.EntityName,
            request.EntityId,
            request.Action,
            request.OldValues,
            request.NewValues,
            request.UserId,
            request.UserName,
            request.IpAddress,
            request.UserAgent
        );

        return CreatedAtAction(nameof(GetAuditLog), new { id = auditLog.Id }, auditLog);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuditLog>> GetAuditLog(int id)
    {
        var log = await _auditService.GetAuditLogByIdAsync(id);
        if (log == null)
            return NotFound();

        return Ok(log);
    }

    [HttpGet("entity/{entityName}")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogs(string entityName, [FromQuery] int? entityId = null, [FromQuery] string? action = null, [FromQuery] int? days = null)
    {
        var logs = await _auditService.GetAuditLogsAsync(entityName, entityId, action, days);
        return Ok(logs);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogsByUser(string userId, [FromQuery] int? days = null)
    {
        var logs = await _auditService.GetAuditLogsByUserAsync(userId, days);
        return Ok(logs);
    }

    [HttpGet("range")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogsByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        if (startDate > endDate)
            return BadRequest("Start date must be before end date.");

        var logs = await _auditService.GetAuditLogsByDateRangeAsync(startDate, endDate);
        return Ok(logs);
    }

    [HttpDelete("cleanup/old")]
    public async Task<ActionResult<int>> DeleteOldAuditLogs([FromQuery] int daysOld = 90)
    {
        var count = await _auditService.DeleteOldAuditLogsAsync(daysOld);
        return Ok(new { deletedCount = count });
    }
}

public class LogChangeRequest
{
    public required string EntityName { get; set; }
    public int EntityId { get; set; }
    public required string Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}