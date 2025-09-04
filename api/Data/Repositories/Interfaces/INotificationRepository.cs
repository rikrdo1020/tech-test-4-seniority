
using api.Models;

public interface INotificationRepository
{
    Task<Notification> AddAsync(Notification notification);
    Task<PagedResult<Notification>> GetByUserAsync(Guid userId, int page, int pageSize);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task<Notification?> GetByIdAsync(Guid id);
    Task MarkAsReadAsync(Guid id);
    Task MarkAllAsReadAsync(Guid userId);
}