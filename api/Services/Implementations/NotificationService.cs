using api.Models;
using api.Models.Entities;
using api.Services.Interfaces;
using api.Services.Publishers;



public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepo;
    private readonly INotificationFactory _factory;
    private readonly INotificationPublisher _publisher;
    private readonly IUserService _userService;

    public NotificationService(
        INotificationRepository repo,
        INotificationFactory factory,
        INotificationPublisher publisher,
        IUserService userService)
    {
        _notificationRepo = repo;
        _factory = factory;
        _publisher = publisher;
        _userService = userService;
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
    {
        var user = await _userService.GetCurrentUserAsync(dto.RecipientUserId);
        if (user == null)
            throw new ArgumentException("User not found", nameof(dto.RecipientUserId));

        var newDto = new CreateNotificationDto(
            user.Id,
            dto.Title,
            dto.Message,
            dto.Type,
            dto.RelatedTaskId
        );

        var entity = _factory.Create(newDto);
        var saved = await _notificationRepo.AddAsync(entity);

        await _publisher.PublishAsync(saved);

        return MapToDto(saved);
    }

    public async Task<NotificationDto> CreateFromTaskAssignmentAsync(TaskItem task)
    {
        var entity = _factory.CreateForTaskAssignment(task);
        var saved = await _notificationRepo.AddAsync(entity);
        await _publisher.PublishAsync(saved);
        return MapToDto(saved);
    }

    public async Task<PagedResult<NotificationDto>> GetByUserAsync(Guid externalId, int page = 1, int pageSize = 20)
    {
        var user = await _userService.GetCurrentUserAsync(externalId);
        if(user == null)
            throw new ArgumentException("User not found", nameof(externalId));

        var paged = await _notificationRepo.GetByUserAsync(user.Id, page, pageSize);
        var items = paged.Items.Select(MapToDto).ToList();
        return new PagedResult<NotificationDto>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }

    public async Task<int> GetUnreadCountAsync(Guid externalUserId)
    {
        var user = await _userService.GetCurrentUserAsync(externalUserId);
        if(user == null) 
            throw new ArgumentException("User not found", nameof(externalUserId));
        return await _notificationRepo.GetUnreadCountAsync(user.Id);
    }

    public async Task MarkAsReadAsync(Guid id) => await _notificationRepo.MarkAsReadAsync(id);

    public async Task MarkAllAsReadAsync(Guid externalUserId)
    { 
        var user = await _userService.GetCurrentUserAsync(externalUserId);
        if(user == null) 
            throw new ArgumentException("User not found", nameof(externalUserId));
        await _notificationRepo.MarkAllAsReadAsync(user.Id);
    }

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