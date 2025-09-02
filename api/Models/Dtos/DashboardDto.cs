
namespace api.Models.Dtos
{
    public class DashboardDto
    {
        public string RelevantPeriod { get; set; } = string.Empty;
        public List<TaskListItemDto> TasksDueToday { get; set; } = new();
        public List<TaskListItemDto> TasksDueThisWeek { get; set; } = new();
        public List<TaskListItemDto> TasksDueThisMonth { get; set; } = new();
        public List<TaskListItemDto> UpcomingTasks { get; set; } = new();
        public TaskCountsDto Counts { get; set; } = new();
    }

    public class TaskCountsDto
    {
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Done { get; set; }
        public int Total { get; set; }
    }
}
