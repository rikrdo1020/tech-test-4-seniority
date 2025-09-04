using api.Models.Dtos;

namespace api.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardAsync(Guid externalId, CancellationToken ct = default);
    }
}