using api.Helpers;
using api.Models.Dtos;
using api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System.Net;
using System.Text.Json;

namespace api.Functions;

public class DashboardFunction
{
    private readonly ILogger<DashboardFunction> _logger;
    private readonly IDashboardService _dashboardService;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DashboardFunction(ILogger<DashboardFunction> logger, IDashboardService dashboardService)
    {
        _logger = logger;
        _dashboardService = dashboardService;
    }

    [Function("GetDashboard")]
    [OpenApiOperation(
        operationId: "GetDashboard",
        tags: new[] { "Dashboard" },
        Summary = "Get dashboard data",
        Description = "Retrieves the dashboard information for the currently authenticated user.")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(DashboardDto),
        Description = "The dashboard data for the authenticated user.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.Unauthorized,
        Description = "Authentication failed or token is missing.")]
    public async Task<IActionResult> GetDashboard(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "dashboard")] HttpRequestData req,
            FunctionContext context,
            CancellationToken ct)
    {
        var externalId = GetUserClaim.GetUserExternalId(context);

        if (externalId == Guid.Empty)
            throw new UnauthorizedAccessException("User not authenticated or missing oid claim.");

        var dashboard = await _dashboardService.GetDashboardAsync(externalId, ct);
        return new OkObjectResult(dashboard);
    }
}