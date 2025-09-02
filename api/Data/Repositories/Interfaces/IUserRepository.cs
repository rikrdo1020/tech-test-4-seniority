using api.Models;
using api.Models.Entities;

namespace api.Data.Repositories.Interfaces;

public interface IUserRepository
{
    Task<PagedResult<User>> QueryAsync(string? search = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByExternalIdAsync(Guid externalId, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task<User> UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}