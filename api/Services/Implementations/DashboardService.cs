using api.Models.Dtos;
using api.Models.Entities;
using api.Services.Interfaces;

namespace api.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ITaskService _taskService;
        private readonly IUserService _userService;

        public DashboardService(ITaskService taskService, IUserService userService)
        {
            _taskService = taskService;
            _userService = userService;
        }

        /// <summary>
        /// Builds the dashboard for a user by aggregating task lists and counts for several time periods.
        /// </summary>
        /// <param name="externalId">The user's external identifier (e.g. OID) used to scope tasks.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="DashboardDto"/> containing:
        /// - RelevantPeriod (string),
        /// - TasksDueToday, TasksDueThisWeek, TasksDueThisMonth, UpcomingTasks (lists of tasks),
        /// - Counts (TaskCountsDto with Pending, InProgress, Done, Total).
        /// </returns>
        /// <remarks>
        /// - Ensures the user exists by calling IUserService.GetCurrentUserAsync.
        /// - Uses UTC-based date ranges (today, week, month, upcoming 30 days).
        /// - Fetches top N tasks (topN = 5) per period and uses GetTasksAsync with paging to obtain lists and counts.
        /// - Counts are derived from PagedResult.TotalCount (GetTasksAsync called with pageSize = 1 for each status).
        /// - Scope is hard-coded to "all" (creator or assignee) to match previous behavior.
        /// - Consider batching/parallelization, caching, or reducing queries for performance on high load.
        /// </remarks>
        public async Task<DashboardDto> GetDashboardAsync(Guid externalId, CancellationToken ct = default)
        {
            var currentUser = await _userService.GetCurrentUserAsync(externalId, ct);

            var todayStart = DateTime.UtcNow.Date;
            var todayEnd = todayStart.AddDays(1).AddTicks(-1);

            var startOfWeek = todayStart.AddDays(-(int)todayStart.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

            var startOfMonth = new DateTime(todayStart.Year, todayStart.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

            const string scope = "all";
            const int topN = 5;

            var tasksTodayPaged = await _taskService.GetTasksAsync(externalId, null, null, todayStart, todayEnd, 1, topN, scope, ct);
            var tasksWeekPaged = await _taskService.GetTasksAsync(externalId, null, null, startOfWeek, endOfWeek, 1, topN, scope, ct);
            var tasksMonthPaged = await _taskService.GetTasksAsync(externalId, null, null, startOfMonth, endOfMonth, 1, topN, scope, ct);

            var upcomingFrom = todayStart.AddDays(1);
            var upcomingTo = todayStart.AddDays(30);
            var upcomingPaged = await _taskService.GetTasksAsync(externalId, null, null, upcomingFrom, upcomingTo, 1, topN, scope, ct);

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

            return dashboard;
        }

        /// <summary>
        /// Chooses the most relevant period label based on counts, with precedence: today &gt; week &gt; month.
        /// </summary>
        /// <param name="todayCount">Number of items due today.</param>
        /// <param name="weekCount">Number of items due this week.</param>
        /// <param name="monthCount">Number of items due this month.</param>
        /// <returns>
        /// A string key representing the selected period:
        /// "for today", "for this week", "for this month", or "upcoming".
        /// </returns>
        /// <remarks>
        /// - Returns the earliest period that has at least one item.
        /// - Intended for simple user-facing labels; keep business logic here if priority rules change.
        /// </remarks>
        private static string DetermineRelevantPeriod(int todayCount, int weekCount, int monthCount)
        {
            if (todayCount > 0) return "for today";
            if (weekCount > 0) return "for this week";
            if (monthCount > 0) return "for this month";
            return "upcoming";
        }
    }
}