using api.Data.Repositories.Interfaces;
using api.Models;
using api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace api.Data.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly DataContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(DataContext context,  ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResult<User>> QueryAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        const int MaxPageSize = 100;
        if (page <= 0) page = 1;
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var skip = (page - 1) * pageSize;

        IQueryable<User> query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.Like(u.Name, pattern) ||
                EF.Functions.Like(u.Email, pattern));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Name)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<User>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<User?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default)
    {
        if (externalId == Guid.Empty) return null;

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        try
        {
            var entry = await _context.Users.AddAsync(user, ct);
            await _context.SaveChangesAsync(ct);
            return entry.Entity;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogWarning(dbEx, "DbUpdateException when creating User (ExternalId={ExternalId}), attempting re-read", user.ExternalId);

            var existing = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ExternalId == user.ExternalId, ct);

            if (existing != null) return existing;

            throw;
        }
    }

    public async Task<User> UpdateAsync(User user, CancellationToken ct = default)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
        return user;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
            return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}