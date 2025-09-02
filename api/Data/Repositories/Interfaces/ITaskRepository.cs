using api.Models;
using api.Models.Dtos;
using api.Models.Entities;

namespace api.Data.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<PagedResult<TaskItem>> GetTasksForUserAsync(
        Guid userId,
        string? search = null,
        TaskItemStatus? status = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
    Task<TaskItem> CreateAsync(TaskItem task, CancellationToken ct = default);
    Task<TaskItem> UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<int> GetTaskCountForUserAsync(
        Guid userId,
        TaskItemStatus? status = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        CancellationToken ct = default);
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
}