using api.Data.Repositories.Interfaces;
using api.Models;
using api.Models.Dtos;
using api.Models.Entities;
using api.Services.Interfaces;

namespace api.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IGraphService _graphService;
    public UserService(IUserRepository userRepository, IGraphService graphService)
    {
        _userRepository = userRepository;
        _graphService = graphService;
    }

    public async Task<UserDto> ProvisionCurrentUserAsync(
        Guid externalId,
        CancellationToken ct = default)
    {
        if (externalId == Guid.Empty)
            throw new ArgumentException("ExternalId is required", nameof(externalId));

        var existing = await _userRepository.GetByExternalIdAsync(externalId, ct);
        if (existing != null)
        {
            return MapToDto(existing);
        }

        var graphUser = await _graphService.GetUserFromGraphAsync(externalId, ct);
        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Name = !string.IsNullOrWhiteSpace(graphUser.Name) ? graphUser.Name : "Unknown",
            Email = !string.IsNullOrWhiteSpace(graphUser.Email) ? graphUser.Email : $"{externalId}@no-reply.local",
            CreatedAt = DateTime.UtcNow
        };

        var created = await _userRepository.AddAsync(user, ct);
        return MapToDto(created);
    }

    public async Task<UserDto> GetCurrentUserAsync(
        Guid externalId,
        CancellationToken ct = default)
    {
        if(externalId == Guid.Empty)
            throw new ArgumentException("ExternalId is required", nameof(externalId));

        var user = await _userRepository.GetByExternalIdAsync(externalId, ct);
        if (user != null) return MapToDto(user);

        return await ProvisionCurrentUserAsync(externalId, ct);
    }

    public async Task<UserSummaryDto?> GetUserSummaryByExternalIdAsync(
        Guid externalId,
        CancellationToken ct = default)
    {
        if (externalId == Guid.Empty) return null;

        var user = await _userRepository.GetByExternalIdAsync(externalId, ct);
        return user != null ? MapToSummaryDto(user) : null;
    }

    public async Task<PagedResult<UserSummaryDto>> SearchUsersAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var paged = await _userRepository.QueryAsync(search, page, pageSize, ct);

        var summaries = paged.Items.Select(MapToSummaryDto).ToList();

        return new PagedResult<UserSummaryDto>
        {
            Items = summaries,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }

    public async Task<UserDto> UpdateCurrentUserAsync(
        Guid externalId,
        string? name = null,
        string? email = null,
        CancellationToken ct = default)
    {
        if (externalId == Guid.Empty)
            throw new ArgumentException("ExternalId is required", nameof(externalId));

        var user = await _userRepository.GetByExternalIdAsync(externalId, ct)
                   ?? throw new KeyNotFoundException("User not found");

        if (!string.IsNullOrWhiteSpace(name))
            user.Name = name.Trim();

        if (!string.IsNullOrWhiteSpace(email))
            user.Email = email.Trim();

        var updated = await _userRepository.UpdateAsync(user, ct);
        return MapToDto(updated);
    }

    public async Task<UserDto> CreateNewUserAsync(
        Guid externalId,
        string name,
        string email,
        CancellationToken ct = default)
    {
        if (externalId == Guid.Empty)
            throw new ArgumentException("ExternalId is required", nameof(externalId));
        var existing = await _userRepository.GetByExternalIdAsync(externalId, ct);
        if (existing != null)
            throw new InvalidOperationException("User with the same ExternalId already exists");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        var user = new User
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Name = name.Trim(),
            Email = email.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        var created = await _userRepository.AddAsync(user, ct);
        return MapToDto(created);
    }

    private UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        ExternalId = u.ExternalId,
        Name = u.Name,
        Email = u.Email,
        CreatedAt = u.CreatedAt
    };

    private UserSummaryDto MapToSummaryDto(User u) => new()
    {
        Id = u.Id,
        ExternalId = u.ExternalId,
        Name = u.Name,
        Email = u.Email
    };
}