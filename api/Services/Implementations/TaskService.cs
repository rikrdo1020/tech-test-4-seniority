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

    /// <summary>
    /// Retrieves a paged list of tasks for the current user (provisioned from an external ID),
    /// applies optional filters and scope ("assigned" or "created"), maps tasks to DTOs, and returns the paged result.
    /// </summary>
    /// <param name="currentUserExternalId">External ID of the current user.</param>
    /// <param name="search">Optional text filter for title/description.</param>
    /// <param name="status">Optional task status filter.</param>
    /// <param name="dueDateFrom">Optional inclusive lower bound for due date.</param>
    /// <param name="dueDateTo">Optional inclusive upper bound for due date.</param>
    /// <param name="page">Page number (minimum 1).</param>
    /// <param name="pageSize">Page size (clamped by caller/repo).</param>
    /// <param name="scope">Scope filter: "assigned" or "created".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PagedResult of TaskListItemDto containing items and total count for the selected scope.</returns>
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

    /// <summary>
    /// Retrieves a task by ID for the current user (provisioned from external ID).
    /// Verifies user is owner or assignee, maps task to detail DTO.
    /// </summary>
    /// <param name="currentUserExternalId">External ID of the current user.</param>
    /// <param name="taskId">ID of the task to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>TaskDetailDto containing task details.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if task does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if user is not owner or assignee.</exception>
    public async Task<TaskDetailDto> GetByIdAsync(Guid currentUserExternalId, Guid taskId, CancellationToken ct = default)
    {
        var currentUser = await _userService.ProvisionCurrentUserAsync(currentUserExternalId, ct);
        var task = await _taskRepo.GetByIdAsync(taskId, ct)
                   ?? throw new KeyNotFoundException("Task not found.");

        if (!IsOwnerOrAssignee(task, currentUser.Id))
            throw new UnauthorizedAccessException("You are not authorized to view this task.");

        return MapToDetailDto(task);
    }

    /// <summary>
    /// Creates a new task for the current user (provisioned from external ID).
    /// Assigns task to another user if AssignedToExternalId is provided.
    /// </summary>
    /// <param name="currentUserExternalId">External ID of the current user (creator).</param>
    /// <param name="dto">CreateTaskDto containing task details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>TaskDetailDto of the created task.</returns>
    /// <exception cref="InvalidOperationException">Thrown if assigned user is not found.</exception>
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

    /// <summary>
    /// Updates an existing task for the current user (provisioned from external ID).
    /// Verifies user is owner or assignee. Updates task fields and assignee if provided.
    /// </summary>
    /// <param name="currentUserExternalId">External ID of the current user.</param>
    /// <param name="taskId">ID of the task to update.</param>
    /// <param name="dto">UpdateTaskDto containing updated task details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>TaskDetailDto of the updated task.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if task does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if user is not owner or assignee.</exception>
    /// <exception cref="InvalidOperationException">Thrown if assigned user is not found.</exception>
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

    /// <summary>
    /// Updates the status of an existing task for the current user (provisioned from external ID).
    /// Verifies user is owner or assignee.
    /// </summary>
    /// <param name="currentUserExternalId">External ID of the current user.</param>
    /// <param name="taskId">ID of the task to update.</param>
    /// <param name="status">New TaskItemStatus to set.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>TaskDetailDto of the updated task.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if task does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if user is not owner or assignee.</exception>
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

    /// <summary>
    /// Deletes a task for the current user (provisioned from external ID).
    /// Verifies user is owner or assignee.
    /// </summary>
    /// <param name="currentUserExternalId">External ID of the current user.</param>
    /// <param name="taskId">ID of the task to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if task was deleted, false otherwise.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if task does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if user is not owner or assignee.</exception>
    public async Task<bool> DeleteAsync(Guid currentUserExternalId, Guid taskId, CancellationToken ct = default)
    {
        var currentUser = await _userService.ProvisionCurrentUserAsync(currentUserExternalId, ct);
        var task = await _taskRepo.GetByIdAsync(taskId, ct) ?? throw new KeyNotFoundException("Task not found.");

        if (!IsOwnerOrAssignee(task, currentUser.Id))
            throw new UnauthorizedAccessException("You are not authorized to delete this task.");

        return await _taskRepo.DeleteAsync(taskId, ct);
    }

    /// <summary>
    /// Checks if the given user ID is the creator or assignee of the task.
    /// </summary>
    /// <param name="t">TaskItem to check.</param>
    /// <param name="userId">User ID to verify.</param>
    /// <returns>True if user is creator or assignee, false otherwise.</returns>
    private bool IsOwnerOrAssignee(TaskItem t, Guid userId)
    {
        return t.CreatedByUserId == userId || (t.AssignedToUserId.HasValue && t.AssignedToUserId.Value == userId);
    }

    /// <summary>
    /// Maps a <see cref="TaskItem"/> to a <see cref="TaskListItemDto"/>, including user summaries and overdue calculation.
    /// </summary>
    /// <param name="t">Task item to map.</param>
    /// <returns>Mapped <see cref="TaskListItemDto"/>.</returns>
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

    /// <summary>
    /// Maps a <see cref="TaskItem"/> to a <see cref="TaskDetailDto"/>, including full user details and timestamps.
    /// </summary>
    /// <param name="t">Task item to map.</param>
    /// <returns>Mapped <see cref="TaskDetailDto"/>.</returns>
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