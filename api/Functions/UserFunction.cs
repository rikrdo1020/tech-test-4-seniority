using api.Helpers;
using api.Models;
using api.Models.Dtos;
using api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi;
using System.Net;
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
        [OpenApiOperation(
            operationId: "ProvisionUser",
            tags: new[] { "User" },
            Summary = "Provision the current authenticated user",
            Description = "Creates or updates the profile of the currently authenticated user based on their external ID claim.")]
        [OpenApiRequestBody(
            contentType: "application/json",
            bodyType: typeof(ProvisionUserDto),
            Required = false,
            Description = "Optional provisioning data for the user.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.Created,
            contentType: "application/json",
            bodyType: typeof(UserDto),
            Description = "The user was successfully provisioned and created.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.OK,
            contentType: "application/json",
            bodyType: typeof(UserDto),
            Description = "The user was already provisioned and returned.")]
        [OpenApiResponseWithoutBody(
            statusCode: HttpStatusCode.Unauthorized,
            Description = "Authentication failed or token is missing.")]
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
        [OpenApiOperation(
            operationId: "GetCurrentUser",
            tags: new[] { "User" },
            Summary = "Get current authenticated user",
            Description = "Retrieves the profile of the currently authenticated user based on their external ID claim.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.OK,
            contentType: "application/json",
            bodyType: typeof(UserDto),
            Description = "The authenticated user's profile.")]
        [OpenApiResponseWithoutBody(
            statusCode: HttpStatusCode.Unauthorized,
            Description = "Authentication failed or token is missing.")]
        [OpenApiResponseWithoutBody(
            statusCode: HttpStatusCode.NotFound,
            Description = "No user was found for the given external ID.")]
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
        [OpenApiOperation(operationId: "GetUserByExternalId", tags: new[] { "User" }, Summary = "Retrieve a user by external ID", Description = "Returns a user summary based on the provided external ID.")]
        [OpenApiParameter(
            name: "externalId",
            Required = true,
            Type = typeof(Guid),
            Description = "The external ID of the user.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.OK,
            contentType: "application/json",
            bodyType: typeof(UserSummaryDto),
            Description = "The user summary was successfully retrieved.")]
        [OpenApiResponseWithoutBody(
            statusCode: HttpStatusCode.NotFound,
            Description = "No user was found with the given external ID.")]
        [OpenApiResponseWithoutBody(
            statusCode: HttpStatusCode.BadRequest,
            Description = "The external ID provided is invalid or missing.")]
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
        [OpenApiOperation(
            operationId: "SearchUsers",
            tags: new[] { "User" },
            Summary = "Search users",
            Description = "Returns a paginated list of users filtered by optional search term.")]
        [OpenApiParameter(
            name: "search",
            Required = false,
            Type = typeof(string),
            Description = "Optional search term to filter users by name or email.")]
        [OpenApiParameter(
            name: "page",
            Required = false,
            Type = typeof(int),
            Description = "Page number for pagination (default is 1).")]
        [OpenApiParameter(
            name: "pageSize",
            Required = false,
            Type = typeof(int),
            Description = "Number of items per page (default is 20).")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.OK,
            contentType: "application/json",
            bodyType: typeof(PagedResult<UserDto>),
            Description = "Paginated list of users matching the search criteria.")]
        [OpenApiResponseWithoutBody(
            statusCode: HttpStatusCode.Unauthorized,
            Description = "Authentication failed or token is missing.")]
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
        [OpenApiOperation(
            operationId: "UpdateCurrentUser",
            tags: new[] { "User" },
            Summary = "Update current authenticated user",
            Description = "Updates the profile information of the currently authenticated user based on their external ID claim.")]
        [OpenApiRequestBody(
            contentType: "application/json",
            bodyType: typeof(ProvisionUserDto),
            Required = true,
            Description = "User data to update, including name and email.")]
        [OpenApiResponseWithBody(
            statusCode: HttpStatusCode.OK,
            contentType: "application/json",
            bodyType: typeof(UserDto),
            Description = "The updated user profile.")]
        [OpenApiResponseWithoutBody(
            statusCode: HttpStatusCode.BadRequest,
            Description = "Invalid or missing request body.")]
                [OpenApiResponseWithoutBody(
            statusCode: HttpStatusCode.Unauthorized,
            Description = "Authentication failed or token is missing.")]
        public async Task<IActionResult> UpdateCurrentUser(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "users/me")] HttpRequest req,
            FunctionContext context,
            CancellationToken ct)
        {
            var externalId = GetUserClaim.GetUserExternalId(context);

            var dto = await req.ReadFromJsonAsync<ProvisionUserDto>(JsonOptions, ct);
            if (dto == null)
                return new BadRequestObjectResult("Invalid request body.");

            var updated = await _userService.UpdateCurrentUserAsync(externalId, dto.Name, ct);
            return new OkObjectResult(updated);
        }

    }
}