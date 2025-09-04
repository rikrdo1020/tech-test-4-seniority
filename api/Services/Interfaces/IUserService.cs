using api.Models;
using api.Models.Dtos;
using api.Models.Entities;

namespace api.Services.Interfaces;

public interface IUserService
{
    Task<UserDto> ProvisionCurrentUserAsync(
        Guid externalId,
        CancellationToken ct = default);

    Task<UserDto> GetCurrentUserAsync(
        Guid externalId,
        CancellationToken ct = default);

    Task<UserSummaryDto?> GetUserSummaryByExternalIdAsync(
        Guid externalId,
        CancellationToken ct = default);

    Task<PagedResult<UserSummaryDto>> SearchUsersAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<UserDto> UpdateCurrentUserAsync(
        Guid externalId,
        string? name = null,
        CancellationToken ct = default);

    Task<UserDto> CreateNewUserAsync(
        Guid externalId,
        string name,
        string email,
        CancellationToken ct = default);
}