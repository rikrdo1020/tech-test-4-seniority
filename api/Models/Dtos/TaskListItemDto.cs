using api.Models.Entities;

namespace api.Models.Dtos
{
    public class TaskListItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public TaskItemStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public UserSummaryDto? AssignedTo { get; set; }
        public UserSummaryDto? CreatedBy { get; set; }
    }
}
