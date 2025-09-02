using api.Helpers;
using api.Models.Dtos;
using api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace api.Functions
{
    public class UsersFunction
    {
        private readonly ILogger<UsersFunction> _logger;
        private readonly IUserService _userService;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public UsersFunction(ILogger<UsersFunction> logger, IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [Function("ProvisionUser")]
        public async Task<IActionResult> ProvisionUser(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "users/provision")] HttpRequest req,
            FunctionContext context,
            CancellationToken ct)
        {
            var externalId = GetUserClaim.GetUserExternalId(context);

            ProvisionUserDto? dto = null;
            if (req.ContentLength > 0)
            {
                dto = await req.ReadFromJsonAsync<ProvisionUserDto>(JsonOptions, ct);
            }

            var created = await _userService.ProvisionCurrentUserAsync(externalId, ct);

            var isFresh = (DateTime.UtcNow - created.CreatedAt).TotalSeconds < 2;
            if (isFresh)
            {
                var location = $"/users/{created.ExternalId}";
                return new CreatedResult(location, created);
            }

            return new OkObjectResult(created);
        }

        [Function("GetCurrentUser")]
        public async Task<IActionResult> GetCurrentUser(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/me")] HttpRequest req,
            FunctionContext context,
            CancellationToken ct)
        {
            var externalId = GetUserClaim.GetUserExternalId(context);

            var user = await _userService.GetCurrentUserAsync(externalId, ct);
            return new OkObjectResult(user);
        }

        [Function("GetUserByExternalId")]
        public async Task<IActionResult> GetUserByExternalId(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{externalId}")] HttpRequest req,
            FunctionContext context,
            string externalId,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(externalId))
                return new BadRequestObjectResult("externalId is required.");

            var summary = await _userService.GetUserSummaryByExternalIdAsync(Guid.Parse(externalId), ct);
            if (summary == null) return new NotFoundResult();
            return new OkObjectResult(summary);
        }

        [Function("SearchUsers")]
        public async Task<IActionResult> SearchUsers(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users")] HttpRequest req,
            FunctionContext context,
            CancellationToken ct)
        {
            var query = req.Query;

            var search = query.Get<string>("search");
            var page = int.TryParse(query["page"].FirstOrDefault(), out var p) ? p : 1;
            var pageSize = int.TryParse(query["pageSize"].FirstOrDefault(), out var ps) ? ps : 20;

            var paged = await _userService.SearchUsersAsync(search, page, pageSize, ct);
            return new OkObjectResult(paged);
        }

        [Function("UpdateCurrentUser")]
        public async Task<IActionResult> UpdateCurrentUser(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "users/me")] HttpRequest req,
            FunctionContext context,
            CancellationToken ct)
        {
            var externalId = GetUserClaim.GetUserExternalId(context);

            var dto = await req.ReadFromJsonAsync<ProvisionUserDto>(JsonOptions, ct);
            if (dto == null)
                return new BadRequestObjectResult("Invalid request body.");

            var updated = await _userService.UpdateCurrentUserAsync(externalId, dto.Name, dto.Email, ct);
            return new OkObjectResult(updated);
        }

    }
}