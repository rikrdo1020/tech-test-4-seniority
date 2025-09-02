using api.Models.Dtos;

namespace api.Services
{
    public interface IGraphService
    {
        Task<UserDto> GetUserFromGraphAsync(Guid externalId, CancellationToken ct = default);
    }
}
