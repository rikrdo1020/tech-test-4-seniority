using api.Data.Repositories.Interfaces;
using api.Models;
using api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace api.Data.Repositories.Implementations;

public class TaskRepository : ITaskRepository
{
    private readonly DataContext _context;
    private const int MaxPageSize = 100;
    
    public TaskRepository(DataContext context)
    {
        _context = context;
    }

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

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Tasks
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

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

    public async Task<TaskItem> CreateAsync(TaskItem task, CancellationToken ct = default)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(ct);
        return task;
    }

    public async Task<TaskItem> UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync(ct);
        return task;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await GetByIdAsync(id, ct);
        if (task == null) return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}