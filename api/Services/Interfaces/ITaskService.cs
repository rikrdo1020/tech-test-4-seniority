using api.Models;
using api.Models.Dtos;
using api.Models.Entities;

namespace api.Services.Interfaces;

public interface ITaskService
{
    Task<PagedResult<TaskListItemDto>> GetTasksAsync(
        Guid currentUserExternalId,
        string? search = null,
        TaskItemStatus? status = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        int page = 1,
        int pageSize = 20,
        string scope = "assigned", // "assigned" | "created" | "all"
        CancellationToken ct = default);

    Task<TaskDetailDto> GetByIdAsync(
        Guid currentUserExternalId,
        Guid taskId,
        CancellationToken ct = default);

    Task<TaskDetailDto> CreateAsync(
        Guid currentUserExternalId,
        CreateTaskDto dto,
        CancellationToken ct = default);

    Task<TaskDetailDto> UpdateAsync(
        Guid currentUserExternalId,
        Guid taskId,
        UpdateTaskDto dto,
        CancellationToken ct = default);

    Task<TaskDetailDto> UpdateStatusAsync(
        Guid currentUserExternalId,
        Guid taskId,
        TaskItemStatus status,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(
        Guid currentUserExternalId,
        Guid taskId,
        CancellationToken ct = default);
}