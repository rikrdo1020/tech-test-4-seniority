using api.Helpers;
using api.Models;
using api.Models.Dtos;
using api.Models.Entities;
using api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace api.Functions;

public class TasksFunction
{
    private readonly ILogger<TasksFunction> _logger;
    private readonly ITaskService _taskService;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    
    public TasksFunction(ILogger<TasksFunction> logger, ITaskService taskService)
    {
        _logger = logger;
        _taskService = taskService;
    }

    [Function("CreateTask")]
    [OpenApiOperation(
        operationId: "CreateTask",
        tags: new[] { "Task" },
        Summary = "Create a new task",
        Description = "Creates a new task for the currently authenticated user based on the provided task data.")]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(CreateTaskDto),
        Required = true,
        Description = "Task data including title, description, due date, and status.")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.Created,
        contentType: "application/json",
        bodyType: typeof(TaskDetailDto),
        Description = "The newly created task.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.BadRequest,
        Description = "Invalid or missing request body.")]
        [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.Unauthorized,
        Description = "Authentication failed or token is missing.")]
    public async Task<IActionResult> CreateTask(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "tasks")] HttpRequest req,
        FunctionContext context,
        CancellationToken ct)
    {
        var dto = await req.ReadFromJsonAsync<CreateTaskDto>(JsonOptions, ct);
        if (dto == null)
            return new BadRequestObjectResult("Invalid request body");

        var externalId = GetUserClaim.GetUserExternalId(context);

        var result = await _taskService.CreateAsync(externalId, dto, ct);
        return new CreatedResult("Task created.", result);
    }

    [Function("GetAllTasks")]
    [OpenApiOperation(
        operationId: "GetAllTasks",
        tags: new[] { "Task" },
        Summary = "Retrieve all tasks for the current user",
        Description = "Returns a paginated list of tasks assigned to the authenticated user, with optional filters for search, status, due date range, and scope.")]
    [OpenApiParameter(
        name: "search",
        Required = false,
        Type = typeof(string),
        Description = "Optional search term to filter tasks by title or description.")]
    [OpenApiParameter(
        name: "status",
        Required = false,
        Type = typeof(string),
        Description = "Optional task status filter (e.g., Pending, Completed).")]
    [OpenApiParameter(
        name: "dueDateFrom",
        Required = false,
        Type = typeof(string),
        Description = "Optional start date to filter tasks by due date (ISO format).")]
    [OpenApiParameter(
        name: "dueDateTo",
        Required = false,
        Type = typeof(string),
        Description = "Optional end date to filter tasks by due date (ISO format).")]
    [OpenApiParameter(
        name: "scope",
        Required = false,
        Type = typeof(string),
        Description = "Optional scope filter (e.g., 'assigned', 'created'). Default is 'assigned'.")]
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
        bodyType: typeof(PagedResult<TaskDetailDto>),
        Description = "Paginated list of tasks matching the filter criteria.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.Unauthorized,
        Description = "Authentication failed or token is missing.")]
    public async Task<IActionResult> GetAllTasks(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks")] HttpRequest req,
        FunctionContext context,
        CancellationToken ct)
    {
        var externalId = GetUserClaim.GetUserExternalId(context);

        var query = req.Query;

        var search = query.Get<string>("search");
        var statusStr = query.Get<string>("status");
        var dueFromStr = query.Get<string>("dueDateFrom");
        var dueToStr = query.Get<string>("dueDateTo");
        var scope = query.Get<string>("scope") ?? "assigned";
        var page = query.Get<int?>("page") ?? 1;
        var pageSize = query.Get<int?>("pageSize") ?? 20;

        var status = Enum.TryParse<TaskItemStatus>(statusStr, out var s) ? s : (TaskItemStatus?)null;
        var dueFrom = DateTime.TryParse(dueFromStr, out var df) ? df : (DateTime?)null;
        var dueTo = DateTime.TryParse(dueToStr, out var dt) ? dt : (DateTime?)null;

        var result = await _taskService.GetTasksAsync(
            externalId, search, status, dueFrom, dueTo, page, pageSize, scope, ct);

        return new OkObjectResult(result);
    }

    [Function("GetTaskById")]
    [OpenApiOperation(
    operationId: "GetTaskById",
    tags: new[] { "Task" },
    Summary = "Retrieve a task by ID",
    Description = "Returns the details of a specific task assigned to the authenticated user.")]
    [OpenApiParameter(
    name: "id",
    In = ParameterLocation.Path,
    Required = true,
    Type = typeof(Guid),
    Description = "The unique identifier of the task to retrieve.")]
    [OpenApiResponseWithBody(
    statusCode: HttpStatusCode.OK,
    contentType: "application/json",
    bodyType: typeof(TaskDetailDto),
    Description = "The requested task details.")]
    [OpenApiResponseWithoutBody(
    statusCode: HttpStatusCode.NotFound,
    Description = "No task was found with the given ID.")]
    [OpenApiResponseWithoutBody(
    statusCode: HttpStatusCode.Unauthorized,
    Description = "Authentication failed or token is missing.")]

    public async Task<IActionResult> GetTaskById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks/{id:guid}")] HttpRequest req,
        FunctionContext context,
        Guid id,
        CancellationToken ct)
    {
        var externalId = GetUserClaim.GetUserExternalId(context);

        var task = await _taskService.GetByIdAsync(externalId, id, ct);
        return new OkObjectResult(task);
    }

    [Function("UpdateTask")]
    [OpenApiOperation(
        operationId: "UpdateTask",
        tags: new[] { "Task" },
        Summary = "Update an existing task",
        Description = "Updates the details of a specific task assigned to the authenticated user.")]
    [OpenApiParameter(
        name: "id",
        Required = true,
        Type = typeof(Guid),
        Description = "The unique identifier of the task to update.")]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(UpdateTaskDto),
        Required = true,
        Description = "Updated task data including title, description, due date, and status.")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(TaskDetailDto),
        Description = "The updated task.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.BadRequest,
        Description = "Invalid or missing request body.")]
        [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.Unauthorized,
        Description = "Authentication failed or token is missing.")]
    public async Task<IActionResult> UpdateTask(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "tasks/{id:guid}")] HttpRequest req,
        FunctionContext context,
        Guid id,
        CancellationToken ct)
    {
        var dto = await req.ReadFromJsonAsync<UpdateTaskDto>(JsonOptions, ct);
        if (dto == null)
            return new BadRequestObjectResult("Invalid request body");

        var externalId = GetUserClaim.GetUserExternalId(context);

        var result = await _taskService.UpdateAsync(externalId, id, dto, ct);
        return new OkObjectResult(result);
    }

    [Function("UpdateTaskStatus")]
    [OpenApiOperation(
        operationId: "UpdateTaskStatus",
        tags: new[] { "Task" },
        Summary = "Update the status of a task",
        Description = "Updates the status of a specific task assigned to the authenticated user.")]
    [OpenApiParameter(
        name: "id",
        Required = true,
        Type = typeof(Guid),
        Description = "The unique identifier of the task to update.")]
    [OpenApiRequestBody(
        contentType: "application/json",
        bodyType: typeof(object), // Puedes reemplazar con un DTO si lo defines
        Required = true,
        Description = "JSON body containing the new status value. Example: { \"status\": \"Completed\" }")]
    [OpenApiResponseWithBody(
        statusCode: HttpStatusCode.OK,
        contentType: "application/json",
        bodyType: typeof(TaskDetailDto),
        Description = "The updated task with the new status.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.BadRequest,
        Description = "Invalid or missing status value in the request body.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.Unauthorized,
        Description = "Authentication failed or token is missing.")]
    public async Task<IActionResult> UpdateTaskStatus(
        [HttpTrigger(AuthorizationLevel.Function, "patch", Route = "tasks/{id:guid}/status")] HttpRequest req,
        FunctionContext context,
        Guid id,
        CancellationToken ct)
    {
        using var sr = new StreamReader(req.Body);
        var json = await sr.ReadToEndAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("status", out var statusElement) ||
            !Enum.TryParse<TaskItemStatus>(statusElement.GetString(), out var status))
        {
            return new BadRequestObjectResult("Invalid status");
        }

        var externalId = GetUserClaim.GetUserExternalId(context);

        var result = await _taskService.UpdateStatusAsync(externalId, id, status, ct);
        return new OkObjectResult(result);
    }

    [Function("DeleteTask")]
    [OpenApiOperation(
        operationId: "DeleteTask",
        tags: new[] { "Task" },
        Summary = "Delete a task",
        Description = "Deletes a specific task assigned to the authenticated user.")]
    [OpenApiParameter(
        name: "id",
        Required = true,
        Type = typeof(Guid),
        Description = "The unique identifier of the task to delete.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.NoContent,
        Description = "The task was successfully deleted.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.NotFound,
        Description = "No task was found with the given ID.")]
    [OpenApiResponseWithoutBody(
        statusCode: HttpStatusCode.Unauthorized,
        Description = "Authentication failed or token is missing.")]
    public async Task<IActionResult> DeleteTask(
        [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "tasks/{id:guid}")] HttpRequest req,
        FunctionContext context,
        Guid id,
        CancellationToken ct)
    {
        var externalId = GetUserClaim.GetUserExternalId(context);

        var deleted = await _taskService.DeleteAsync(externalId, id, ct);
        if (!deleted)
            return new NotFoundResult();

        return new NoContentResult();
    }

}