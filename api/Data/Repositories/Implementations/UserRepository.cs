using api.Data.Repositories.Interfaces;
using api.Models;
using api.Models.Entities;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>
    /// Query users with optional search and paging.
    /// </summary>
    /// <param name="search">Optional name/email search term (wildcard match).</param>
    /// <param name="page">Page number (minimum 1).</param>
    /// <param name="pageSize">Page size (clamped between 1 and 100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PagedResult containing Items, TotalCount, Page and PageSize.</returns>
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

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    /// <param name="id">User ID to find.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User if found, null otherwise.</returns>
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    /// <summary>
    /// Gets a user by ExternalId.
    /// </summary>
    /// <param name="externalId">External ID to find.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>User if found, null otherwise or if externalId is empty.</returns>
    public async Task<User?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default)
    {
        if (externalId == Guid.Empty) return null;

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);
    }

    /// <summary>
    /// Adds a new user. If a user with the same ExternalId already exists, returns the existing user.
    /// </summary>
    /// <param name="user">User to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The added or existing user.</returns>
    /// <exception cref="ArgumentNullException">Thrown if user is null.</exception>
    /// <exception cref="DbUpdateException">Thrown if save fails and no existing user is found.</exception>
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

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="user">User with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated user.</returns>
    /// <exception cref="ArgumentNullException">Thrown if user is null.</exception>
    /// <exception cref="DbUpdateConcurrencyException">Thrown if user does not exist.</exception>
    public async Task<User> UpdateAsync(User user, CancellationToken ct = default)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
        return user;
    }

    /// <summary>
    /// Deletes a user by ID.
    /// </summary>
    /// <param name="id">ID of user to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if user was deleted, false if not found.</returns>
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