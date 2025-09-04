using api.Helpers;
using api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Text.Json;

public class NotificationsFunction
{
    private readonly INotificationService _notificationService;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public NotificationsFunction(INotificationService service)
    {
        _notificationService = service;
    }

    [Function("GetNotifications")]
    [OpenApiOperation(
        operationId: "GetNotifications",
        tags: new[] { "Notifications" },
        Summary = "Get notifications for the current user",
        Description = "Retrieves a paginated list of notifications for the authenticated user.")]
    [OpenApiParameter(
        name: "page",
        In = ParameterLocation.Query,
        Required = false,
        Type = typeof(int),
        Summary = "Page number",
        Description = "The page number to retrieve.",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiParameter(
        name: "pageSize",
        In = ParameterLocation.Query,
        Required = false,
        Type = typeof(int),
        Summary = "Page size",
        Description = "Number of notifications per page.",
        Visibility = OpenApiVisibilityType.Important)]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(PagedResult<NotificationDto>),
        Description = "Paginated list of notifications for the authenticated user.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.Unauthorized,
        Description = "Authentication failed or token is missing.")]
    public async Task<IActionResult> GetNotifications(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "notifications")] HttpRequest req,
        FunctionContext context)
    {
        
        var userId = GetUserClaim.GetUserExternalId(context);
        if (userId == Guid.Empty)
            return new UnauthorizedResult();

        var query = req.Query;
        var page = query.Get<int?>("page") ?? 1;
        var pageSize = query.Get<int?>("pageSize") ?? 20;

        var result = await _notificationService.GetByUserAsync(userId, page, pageSize);
        return new OkObjectResult(result);
    }

    [Function("CreateNotification")]
    [OpenApiOperation(
        operationId: "CreateNotification",
        tags: new[] { "Notifications" },
        Summary = "Create a new notification",
        Description = "Creates a new notification for a user based on the provided payload.")]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(CreateNotificationDto),
        Required = true,
        Description = "The notification data to create.")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Created,
        contentType: "application/json",
        bodyType: typeof(NotificationDto),
        Description = "The newly created notification.")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.BadRequest,
        contentType: "text/plain",
        bodyType: typeof(string),
        Description = "Invalid request body or missing required fields.")]
    public async Task<IActionResult> CreateNotification(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications")] HttpRequest req)
    {
        var dto = await req.ReadFromJsonAsync<CreateNotificationDto>(JsonOptions);
        if (dto == null)
            return new BadRequestObjectResult("Invalid request body");

        var created = await _notificationService.CreateAsync(dto);
        return new CreatedResult("Task created.", created);
    }

    [Function("MarkAsRead")]
    [OpenApiOperation(
        operationId: "MarkAsRead",
        tags: new[] { "Notifications" },
        Summary = "Mark a notification as read",
        Description = "Marks the specified notification as read for the authenticated user.")]
    [OpenApiParameter(
        name: "id",
        In = ParameterLocation.Path,
        Required = true,
        Type = typeof(Guid),
        Summary = "Notification ID",
        Description = "The unique identifier of the notification to mark as read.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.NoContent,
        Description = "Notification marked as read successfully.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.BadRequest,
        Description = "Invalid notification ID format.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.Unauthorized,
        Description = "Authentication failed or token is missing.")]
    public async Task<IActionResult> MarkAsRead(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications/{id}/read")] HttpRequest req,
        string id)
    {
        if (!Guid.TryParse(id, out var guid)) return new BadRequestResult();

        await _notificationService.MarkAsReadAsync(guid);
        return new NoContentResult();
    }

    [Function("MarkAllAsRead")]
    [OpenApiOperation(
        operationId: "MarkAllAsRead",
        tags: new[] { "Notifications" },
        Summary = "Mark all notifications as read",
        Description = "Marks all notifications as read for the currently authenticated user.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.NoContent,
        Description = "All notifications marked as read successfully.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.Unauthorized,
        Description = "Authentication failed or token is missing.")]
    public async Task<IActionResult> MarkAllAsRead(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "notifications/readAll")] HttpRequest req,
        FunctionContext context)
    {
        var userId = GetUserClaim.GetUserExternalId(context);
        if (userId == Guid.Empty)
            return new UnauthorizedResult();

        await _notificationService.MarkAllAsReadAsync(userId);
        return new NoContentResult();
    }

    [Function("GetUnreadNotificationsCount")]
    [OpenApiOperation(
        operationId: "GetUnreadNotificationsCount",
        tags: new[] { "Notifications" },
        Summary = "Get unread notifications count",
        Description = "Returns the number of unread notifications for the currently authenticated user.")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(object),
        Description = "The count of unread notifications.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.Unauthorized,
        Description = "Authentication failed or token is missing.")]
    public async Task<IActionResult> GetUnreadNotificationsCount(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "notifications/unread-count")] HttpRequest req,
    FunctionContext context,
    CancellationToken ct)
    {
        var userId = GetUserClaim.GetUserExternalId(context);
        if (userId == Guid.Empty)
            return new UnauthorizedResult();

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return new OkObjectResult(new { count });
    }
}