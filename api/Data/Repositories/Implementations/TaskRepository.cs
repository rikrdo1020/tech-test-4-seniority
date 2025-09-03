using api.Data.Repositories.Interfaces;
using api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using api.Models;

namespace api.Data.Repositories.Implementations;

public class TaskRepository : ITaskRepository
{
    private readonly DataContext _context;
    
    public TaskRepository(DataContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves a paged list of tasks for the specified user (as creator or assignee),
    /// with optional text search, status and due date filters.
    /// </summary>
    /// <param name="userId">ID of the user (creator or assignee) to fetch tasks for.</param>
    /// <param name="search">Optional substring to match against Title or Description.</param>
    /// <param name="status">Optional TaskItemStatus to filter by.</param>
    /// <param name="dueDateFrom">Optional inclusive lower bound for DueDate.</param>
    /// <param name="dueDateTo">Optional inclusive upper bound for DueDate.</param>
    /// <param name="page">Page number (normalized to minimum 1).</param>
    /// <param name="pageSize">Page size (clamped to a maximum of 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PagedResult&lt;TaskItem&gt; containing Items, TotalCount, Page and PageSize.</returns>
    public async Task<PagedResult<TaskItem>> GetTasksForUserAsync(
        Guid userId,
        string? search = null,
        TaskItemStatus? status = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        const int maxPageSize = 100;
        pageSize = Math.Min(pageSize, maxPageSize);

        var query = _context.Tasks
            .AsNoTracking()
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(t => EF.Functions.Like(t.Title, $"%{s}%") || EF.Functions.Like(t.Description, $"%{s}%"));
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.ItemStatus == status.Value);
        }

        if (dueDateFrom.HasValue)
        {
            query = query.Where(t => t.DueDate >= dueDateFrom.Value);
        }

        if (dueDateTo.HasValue)
        {
            query = query.Where(t => t.DueDate <= dueDateTo.Value);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TaskItem>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Gets a task by ID, including related CreatedByUser and AssignedToUser entities.
    /// </summary>
    /// <param name="id">Task ID to find.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>TaskItem if found, null otherwise.</returns>
    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <summary>
    /// Gets the total count of tasks for a user (as creator or assignee),
    /// with optional status and due date filters.
    /// </summary>
    /// <param name="userId">ID of the user (creator or assignee) to count tasks for.</param>
    /// <param name="status">Optional TaskItemStatus to filter by.</param>
    /// <param name="dueDateFrom">Optional inclusive lower bound for DueDate.</param>
    /// <param name="dueDateTo">Optional inclusive upper bound for DueDate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Total count of matching tasks.</returns>
    public async Task<int> GetTaskCountForUserAsync(
        Guid userId,
        TaskItemStatus? status = null,
        DateTime? dueDateFrom = null,
        DateTime? dueDateTo = null,
        CancellationToken ct = default)
    {
        var query = _context.Tasks
            .Where(t => t.CreatedByUserId == userId || t.AssignedToUserId == userId);

        if (status.HasValue)
        {
            query = query.Where(t => t.ItemStatus == status.Value);
        }

        if (dueDateFrom.HasValue)
        {
            query = query.Where(t => t.DueDate >= dueDateFrom.Value);
        }

        if (dueDateTo.HasValue)
        {
            query = query.Where(t => t.DueDate <= dueDateTo.Value);
        }

        return await query.CountAsync(ct);
    }

    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="task">Task to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created task.</returns>
    /// <exception cref="DbUpdateException">Thrown if save fails (e.g., foreign key constraint violation).</exception>
    public async Task<TaskItem> CreateAsync(TaskItem task, CancellationToken ct = default)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(ct);
        return task;
    }

    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="task">Task with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated task.</returns>
    /// <exception cref="DbUpdateConcurrencyException">Thrown if task does not exist.</exception>
    public async Task<TaskItem> UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync(ct);
        return task;
    }

    /// <summary>
    /// Deletes a task by ID.
    /// </summary>
    /// <param name="id">ID of task to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if task was deleted, false if not found.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await GetByIdAsync(id, ct);
        if (task == null) return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}