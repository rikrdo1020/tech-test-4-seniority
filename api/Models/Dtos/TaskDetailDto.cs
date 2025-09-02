namespace api.Models.Dtos
{
    public class TaskDetailDto : TaskListItemDto
    {
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
