using api.Models;
using api.Models.Entities;

public interface INotificationService
{
    Task<NotificationDto> CreateAsync(CreateNotificationDto dto);
    Task<PagedResult<NotificationDto>> GetByUserAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid id);
    Task MarkAllAsReadAsync(Guid userId);

    Task<NotificationDto> CreateFromTaskAssignmentAsync(TaskItem task);
}