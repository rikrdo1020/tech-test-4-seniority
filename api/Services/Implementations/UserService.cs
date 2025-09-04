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

    /// <summary>
    /// Provisions a user by external ID. If user exists, returns it. Otherwise, fetches from Graph and creates new user.
    /// </summary>
    /// <param name="externalId">External ID (e.g., Azure AD ObjectId) of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>UserDto of the provisioned user.</returns>
    /// <exception cref="ArgumentException">Thrown if externalId is empty.</exception>
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

    /// <summary>
    /// Gets the current user by external ID. If not found, provisions the user from Graph.
    /// </summary>
    /// <param name="externalId">External ID (e.g., Azure AD ObjectId) of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>UserDto of the user.</returns>
    /// <exception cref="ArgumentException">Thrown if externalId is empty.</exception>
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

    /// <summary>
    /// Gets a user summary by external ID.
    /// </summary>
    /// <param name="externalId">External ID of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>UserSummaryDto if found, null otherwise or if externalId is empty.</returns>
    public async Task<UserSummaryDto?> GetUserSummaryByExternalIdAsync(
        Guid externalId,
        CancellationToken ct = default)
    {
        if (externalId == Guid.Empty) return null;

        var user = await _userRepository.GetByExternalIdAsync(externalId, ct);
        return user != null ? MapToSummaryDto(user) : null;
    }

    /// <summary>
    /// Searches users with optional text filter and paging, maps results to UserSummaryDto.
    /// </summary>
    /// <param name="search">Optional text to search in user name or email.</param>
    /// <param name="page">Page number (minimum 1).</param>
    /// <param name="pageSize">Page size (clamped by repository).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PagedResult of UserSummaryDto containing items and total count.</returns>
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

    /// <summary>
    /// Updates the current user's name and/or email by external ID.
    /// </summary>
    /// <param name="externalId">External ID of the user to update.</param>
    /// <param name="name">New name (if provided and not empty).</param>
    /// <param name="email">New email (if provided and not empty).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated UserDto.</returns>
    /// <exception cref="ArgumentException">Thrown if externalId is empty.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if user does not exist.</exception>
    public async Task<UserDto> UpdateCurrentUserAsync(
        Guid externalId,
        string? name = null,
        CancellationToken ct = default)
    {
        if (externalId == Guid.Empty)
            throw new ArgumentException("ExternalId is required", nameof(externalId));

        var user = await _userRepository.GetByExternalIdAsync(externalId, ct)
                   ?? throw new KeyNotFoundException("User not found");

        if (!string.IsNullOrWhiteSpace(name))
            user.Name = name.Trim();

        var updated = await _userRepository.UpdateAsync(user, ct);
        return MapToDto(updated);
    }

    /// <summary>
    /// Creates a new user with the provided external ID, name and email.
    /// </summary>
    /// <param name="externalId">External ID of the user (required).</param>
    /// <param name="name">User name (required).</param>
    /// <param name="email">User email (required).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>UserDto of the created user.</returns>
    /// <exception cref="ArgumentException">Thrown if externalId is empty or name/email are missing.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a user with the same ExternalId already exists.</exception>
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

    /// <summary>
    /// Maps a <see cref="User"/> entity to a <see cref="UserDto"/>.
    /// </summary>
    /// <param name="u">User entity to map.</param>
    /// <returns>Mapped <see cref="UserDto"/>.</returns>
    private UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        ExternalId = u.ExternalId,
        Name = u.Name,
        Email = u.Email,
        CreatedAt = u.CreatedAt
    };

    /// <summary>
    /// Maps a <see cref="User"/> entity to a <see cref="UserSummaryDto"/>.
    /// </summary>
    /// <param name="u">User entity to map.</param>
    /// <returns>Mapped <see cref="UserSummaryDto"/>.</returns>
    private UserSummaryDto MapToSummaryDto(User u) => new()
    {
        Id = u.Id,
        ExternalId = u.ExternalId,
        Name = u.Name,
        Email = u.Email
    };
}