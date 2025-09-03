using api.Models.Entities;

public interface INotificationFactory
{
    Notification Create(CreateNotificationDto dto);
    Notification CreateForTaskAssignment(TaskItem task);
}

public class NotificationFactory : INotificationFactory
{
    public Notification Create(CreateNotificationDto dto)
    {
        return new Notification
        {
            RecipientUserId = dto.RecipientUserId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            RelatedTaskId = dto.RelatedTaskId
        };
    }

    public Notification CreateForTaskAssignment(TaskItem task)
    {
        var title = $"Assigned task: {task.Title}";
        var message = $"You have been assigned task \"{task.Title}\"" +
                      (task.DueDate.HasValue ? $" with due date {task.DueDate:yyyy-MM-dd}." : ".");

        if (!task.AssignedToUserId.HasValue)
            throw new ArgumentException("Task must have AssignedToUserId to create assignment notification.");

        return new Notification
        {
            RecipientUserId = task.AssignedToUserId.Value,
            Title = title,
            Message = message,
            Type = NotificationType.TaskAssigned,
            RelatedTaskId = task.Id
        };
    }
}