using NotificationAuditService.Models;

namespace NotificationAuditService.Services;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(string title, string message, string type, string recipientId, string? recipientEmail = null, string? relatedEntityType = null, int? relatedEntityId = null);
    Task<IEnumerable<Notification>> GetNotificationsForUserAsync(string userId, bool? isRead = null);
    Task<Notification?> GetNotificationByIdAsync(int id);
    Task<bool> MarkAsReadAsync(int notificationId);
    Task<int> MarkAllAsReadAsync(string userId);
    Task<bool> DeleteNotificationAsync(int id);
    Task<int> DeleteOldNotificationsAsync(int daysOld);
}