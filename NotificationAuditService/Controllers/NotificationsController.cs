using Microsoft.AspNetCore.Mvc;
using NotificationAuditService.Models;
using NotificationAuditService.Services;

namespace NotificationAuditService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost]
    public async Task<ActionResult<Notification>> CreateNotification([FromBody] CreateNotificationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var notification = await _notificationService.CreateNotificationAsync(
            request.Title,
            request.Message,
            request.Type,
            request.RecipientId,
            request.RecipientEmail,
            request.RelatedEntityType,
            request.RelatedEntityId
        );

        return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Notification>> GetNotification(int id)
    {
        var notification = await _notificationService.GetNotificationByIdAsync(id);
        if (notification == null)
            return NotFound();

        return Ok(notification);
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<Notification>>> GetUserNotifications(string userId, [FromQuery] bool? isRead = null)
    {
        var notifications = await _notificationService.GetNotificationsForUserAsync(userId, isRead);
        return Ok(notifications);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPut("user/{userId}/read-all")]
    public async Task<ActionResult<int>> MarkAllAsRead(string userId)
    {
        var count = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { markedAsReadCount = count });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var result = await _notificationService.DeleteNotificationAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("cleanup/old")]
    public async Task<ActionResult<int>> DeleteOldNotifications([FromQuery] int daysOld = 30)
    {
        var count = await _notificationService.DeleteOldNotificationsAsync(daysOld);
        return Ok(new { deletedCount = count });
    }
}

public class CreateNotificationRequest
{
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string Type { get; set; }
    public required string RecipientId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}