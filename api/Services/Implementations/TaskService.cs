using api.Data.Repositories.Interfaces;
using api.Models;
using api.Models.Dtos;
using api.Models.Entities;
using api.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace api.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepo;
    private readonly IUserService _userService;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository taskRepo, 
        IUserService userService, 
        ILogger<TaskService> logger)
    {
        _taskRepo = taskRepo;
        _userService = userService;
        _logger = logger;
    }

    public async Task<PagedResult<TaskListItemDto>> GetTasksAsync(
        Guid currentUserExternalId,
        string? search = null,
        TaskItemStatus? status = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        int page = 1,
        int pageSize = 20,
        string scope = "assigned",
        CancellationToken ct = default)
    {
        var currentUser = await _userService.ProvisionCurrentUserAsync(currentUserExternalId, ct);
        Guid userId = currentUser.Id;

        var paged = await _taskRepo.GetTasksForUserAsync(
            userId, search, status, dueDateFrom, dueDateTo, page, pageSize, ct);

        IEnumerable<TaskItem> items = paged.Items;
        if (string.Equals(scope, "created", StringComparison.OrdinalIgnoreCase))
            items = items.Where(t => t.CreatedByUserId == userId);
        else if (string.Equals(scope, "assigned", StringComparison.OrdinalIgnoreCase))
            items = items.Where(t => t.AssignedToUserId == userId);

        var itemsList = items.ToList();
        var dtoItems = itemsList.Select(MapToListItemDto).ToList();

        int totalForScope = await _taskRepo.GetTaskCountForUserAsync(
            userId,
            status,
            dueDateFrom, 
            dueDateTo,
            ct);

        if (string.Equals(scope, "created", StringComparison.OrdinalIgnoreCase))
        {
            var allPaged = await _taskRepo.GetTasksForUserAsync(userId, search, status, dueDateFrom, dueDateTo, 1, 1, ct);
            totalForScope = allPaged.TotalCount;
        }

        return new PagedResult<TaskListItemDto>
        {
            Items = dtoItems,
            TotalCount = totalForScope,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TaskDetailDto> GetByIdAsync(Guid currentUserExternalId, Guid taskId, CancellationToken ct = default)
    {
        var currentUser = await _userService.ProvisionCurrentUserAsync(currentUserExternalId, ct);
        var task = await _taskRepo.GetByIdAsync(taskId, ct)
                   ?? throw new KeyNotFoundException("Task not found.");

        if (!IsOwnerOrAssignee(task, currentUser.Id))
            throw new UnauthorizedAccessException("You are not authorized to view this task.");

        return MapToDetailDto(task);
    }

    public async Task<TaskDetailDto> CreateAsync(
        Guid currentUserExternalId,
        CreateTaskDto dto,
        CancellationToken ct = default)
    {
        var currentUser = await _userService.ProvisionCurrentUserAsync(currentUserExternalId, ct);

        var entity = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            DueDate = dto.DueDate,
            ItemStatus = TaskItemStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUser.Id
        };

        // assignedTo optional
        if (dto.AssignedToExternalId.HasValue)
        {
            var assignedUser = await _userService.GetUserSummaryByExternalIdAsync(dto.AssignedToExternalId.Value, ct);
            if (assignedUser == null)
                throw new InvalidOperationException("Assigned user not found.");
            entity.AssignedToUserId = assignedUser.Id;
        }

        var created = await _taskRepo.CreateAsync(entity, ct);
        return MapToDetailDto(created);
    }

    public async Task<TaskDetailDto> UpdateAsync(
        Guid currentUserExternalId,
        Guid taskId,
        UpdateTaskDto dto,
        CancellationToken ct = default)
    {
        var currentUser = await _userService.ProvisionCurrentUserAsync(currentUserExternalId, ct);
        var task = await _taskRepo.GetByIdAsync(taskId, ct) ?? throw new KeyNotFoundException("Task not found.");

        if (!IsOwnerOrAssignee(task, currentUser.Id))
            throw new UnauthorizedAccessException("You are not authorized to update this task.");

        task.Title = dto.Title.Trim();
        task.Description = dto.Description?.Trim();
        task.DueDate = dto.DueDate;
        task.ItemStatus = dto.ItemStatus;
        task.UpdatedAt = DateTime.UtcNow;

        if (dto.AssignedToExternalId.HasValue)
        {
            var assignedUser = await _userService.GetUserSummaryByExternalIdAsync(dto.AssignedToExternalId.Value, ct);
            if (assignedUser == null)
                throw new InvalidOperationException("Assigned user not found.");
            task.AssignedToUserId = assignedUser.Id;
        }
        else
        {
            task.AssignedToUserId = null;
        }

        var updated = await _taskRepo.UpdateAsync(task, ct);
        return MapToDetailDto(updated);
    }

    public async Task<TaskDetailDto> UpdateStatusAsync(
        Guid currentUserExternalId,
        Guid taskId,
        TaskItemStatus status,
        CancellationToken ct = default)
    {
        var currentUser = await _userService.ProvisionCurrentUserAsync(currentUserExternalId, ct);
        var task = await _taskRepo.GetByIdAsync(taskId, ct) ?? throw new KeyNotFoundException("Task not found.");

        if (!IsOwnerOrAssignee(task, currentUser.Id))
            throw new UnauthorizedAccessException("You are not authorized to change the status of this task.");

        task.ItemStatus = status;
        task.UpdatedAt = DateTime.UtcNow;

        var updated = await _taskRepo.UpdateAsync(task, ct);
        return MapToDetailDto(updated);
    }

    public async Task<bool> DeleteAsync(Guid currentUserExternalId, Guid taskId, CancellationToken ct = default)
    {
        var currentUser = await _userService.ProvisionCurrentUserAsync(currentUserExternalId, ct);
        var task = await _taskRepo.GetByIdAsync(taskId, ct) ?? throw new KeyNotFoundException("Task not found.");

        if (!IsOwnerOrAssignee(task, currentUser.Id))
            throw new UnauthorizedAccessException("You are not authorized to delete this task.");

        return await _taskRepo.DeleteAsync(taskId, ct);
    }

    private bool IsOwnerOrAssignee(TaskItem t, Guid userId)
    {
        return t.CreatedByUserId == userId || (t.AssignedToUserId.HasValue && t.AssignedToUserId.Value == userId);
    }

    private TaskListItemDto MapToListItemDto(TaskItem t)
    {
        return new TaskListItemDto
        {
            Id = t.Id,
            Title = t.Title,
            ItemStatus = t.ItemStatus,
            DueDate = t.DueDate,
            IsOverdue = t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.UtcNow.Date,
            AssignedTo = t.AssignedToUser != null ? new UserSummaryDto { Id = t.AssignedToUser.Id, Name = t.AssignedToUser.Name, Email = t.AssignedToUser.Email, ExternalId = t.AssignedToUser.ExternalId } : null,
            CreatedBy = t.CreatedByUser != null ? new UserSummaryDto { Id = t.CreatedByUser.Id, Name = t.CreatedByUser.Name, Email = t.CreatedByUser.Email, ExternalId = t.AssignedToUser.ExternalId } : null
        };
    }

    private TaskDetailDto MapToDetailDto(TaskItem t)
    {
        return new TaskDetailDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            ItemStatus = t.ItemStatus,
            DueDate = t.DueDate,
            IsOverdue = t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.UtcNow.Date,
            AssignedTo = t.AssignedToUser != null ? new UserSummaryDto { Id = t.AssignedToUser.Id, Name = t.AssignedToUser.Name, Email = t.AssignedToUser.Email, ExternalId = t.AssignedToUser.ExternalId } : null,
            CreatedBy = t.CreatedByUser != null ? new UserSummaryDto { Id = t.CreatedByUser.Id, Name = t.CreatedByUser.Name, Email = t.CreatedByUser.Email, ExternalId = t.AssignedToUser.ExternalId } : null,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }

    
}