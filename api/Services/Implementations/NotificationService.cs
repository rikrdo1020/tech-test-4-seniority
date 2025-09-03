using api.Models;
using api.Models.Entities;

public interface INotificationPublisher
{
    Task PublishAsync(Notification notification); 
}

public class NoOpNotificationPublisher : INotificationPublisher
{
    public Task PublishAsync(Notification notification) => Task.CompletedTask;
}

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly INotificationFactory _factory;
    private readonly INotificationPublisher _publisher;

    public NotificationService(
        INotificationRepository repo,
        INotificationFactory factory,
        INotificationPublisher publisher)
    {
        _repo = repo;
        _factory = factory;
        _publisher = publisher;
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
    {
        var entity = _factory.Create(dto);
        var saved = await _repo.AddAsync(entity);

        // Publish for real-time delivery (SignalR / push) — default no-op
        await _publisher.PublishAsync(saved);

        return MapToDto(saved);
    }

    public async Task<NotificationDto> CreateFromTaskAssignmentAsync(TaskItem task)
    {
        var entity = _factory.CreateForTaskAssignment(task);
        var saved = await _repo.AddAsync(entity);
        await _publisher.PublishAsync(saved);
        return MapToDto(saved);
    }

    public async Task<PagedResult<NotificationDto>> GetByUserAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var paged = await _repo.GetByUserAsync(userId, page, pageSize);
        var items = paged.Items.Select(MapToDto).ToList();
        return new PagedResult<NotificationDto>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(Guid userId) => await _repo.GetUnreadCountAsync(userId);

    public async Task MarkAsReadAsync(Guid id) => await _repo.MarkAsReadAsync(id);

    public async Task MarkAllAsReadAsync(Guid userId) => await _repo.MarkAllAsReadAsync(userId);

    private NotificationDto MapToDto(Notification n)
    {
        return new NotificationDto(
            n.Id,
            n.RecipientUserId,
            n.RelatedTaskId,
            n.Title,
            n.Message,
            n.Type,
            n.IsRead,
            n.CreatedAt,
            n.ReadAt);
    }
}