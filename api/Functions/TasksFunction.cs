using api.Helpers;
using api.Models.Dtos;
using api.Models.Entities;
using api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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