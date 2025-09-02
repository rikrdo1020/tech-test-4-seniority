using api.Helpers;
using api.Models.Dtos;
using api.Models.Entities;
using api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace api.Functions;

public class DashboardFunction
{
    private readonly ILogger<DashboardFunction> _logger;
    private readonly ITaskService _taskService;
    private readonly IUserService _userService;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DashboardFunction(ILogger<DashboardFunction> logger, ITaskService taskService, IUserService userService)
    {
        _logger = logger;
        _taskService = taskService;
        _userService = userService;
    }

    [Function("GetDashboard")]
    public async Task<IActionResult> GetDashboard(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "dashboard")] HttpRequestData req,
            FunctionContext context,
            CancellationToken ct)
    {
        var externalId = GetUserClaim.GetUserExternalId(context);

        if (externalId == Guid.Empty)
            throw new UnauthorizedAccessException("User not authenticated or missing oid claim.");

        // Ensure local user and get display name
        var currentUser = await _userService.GetCurrentUserAsync(externalId, ct);

        // Date ranges (UTC). Adjust if you want to use user's timezone.
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);

        var startOfWeek = todayStart.AddDays(-(int)todayStart.DayOfWeek); // Sunday-based; adjust if Monday is start
        var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

        var startOfMonth = new DateTime(todayStart.Year, todayStart.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

        // Scope: "all" (where user is creator or assignee)
        const string scope = "all";

        // Fetch top N tasks for each period (pageSize small)
        const int topN = 5;
        var tasksTodayPaged = await _taskService.GetTasksAsync(externalId, null, null, todayStart, todayEnd, 1, topN, scope, ct);
        var tasksWeekPaged = await _taskService.GetTasksAsync(externalId, null, null, startOfWeek, endOfWeek, 1, topN, scope, ct);
        var tasksMonthPaged = await _taskService.GetTasksAsync(externalId, null, null, startOfMonth, endOfMonth, 1, topN, scope, ct);

        // Upcoming tasks (next 30 days)
        var upcomingFrom = todayStart.AddDays(1);
        var upcomingTo = todayStart.AddDays(30);
        var upcomingPaged = await _taskService.GetTasksAsync(externalId, null, null, upcomingFrom, upcomingTo, 1, topN, scope, ct);

        // Counts per status — use GetTasksAsync with status + pageSize=1 to read TotalCount from PagedResult
        var pendingPaged = await _taskService.GetTasksAsync(externalId, null, TaskItemStatus.Pending, null, null, 1, 1, scope, ct);
        var inProgressPaged = await _taskService.GetTasksAsync(externalId, null, TaskItemStatus.InProgress, null, null, 1, 1, scope, ct);
        var donePaged = await _taskService.GetTasksAsync(externalId, null, TaskItemStatus.Done, null, null, 1, 1, scope, ct);

        var counts = new TaskCountsDto
        {
            Pending = pendingPaged.TotalCount,
            InProgress = inProgressPaged.TotalCount,
            Done = donePaged.TotalCount,
            Total = pendingPaged.TotalCount + inProgressPaged.TotalCount + donePaged.TotalCount
        };

        var dashboard = new DashboardDto
        {
            RelevantPeriod = DetermineRelevantPeriod(tasksTodayPaged.TotalCount, tasksWeekPaged.TotalCount, tasksMonthPaged.TotalCount),
            TasksDueToday = tasksTodayPaged.Items.ToList(),
            TasksDueThisWeek = tasksWeekPaged.Items.ToList(),
            TasksDueThisMonth = tasksMonthPaged.Items.ToList(),
            UpcomingTasks = upcomingPaged.Items.ToList(),
            Counts = counts
        };

        return new OkObjectResult(dashboard);
    }

    private string DetermineRelevantPeriod(int todayCount, int weekCount, int monthCount)
    {
        if (todayCount > 0) return "for today";
        if (weekCount > 0) return "for this week";
        if (monthCount > 0) return "for this month";
        return "upcoming";
    }
}