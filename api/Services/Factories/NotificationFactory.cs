using api.Models.Entities;

public interface INotificationFactory
{
    Notification Create(CreateNotificationDto dto);
    Notification CreateForTaskAssignment(TaskItem task);
}

public class NotificationFactory : INotificationFactory
{
    /// <summary>
    /// Creates a Notification entity from the provided CreateNotificationDto.
    /// </summary>
    /// <param name="dto">DTO containing RecipientUserId, Title, Message, Type and optional RelatedTaskId.</param>
    /// <returns>A new Notification instance populated from the DTO (does not set Id, CreatedAt or persistence-related fields).</returns>
    /// <remarks>
    /// - This method only constructs the entity; it does not persist it to the database.
    /// - Caller is responsible for validation of DTO values (e.g., ensure RecipientUserId is valid).
    /// - Fields like Id, CreatedAt, IsRead and ReadAt are expected to be set by repository/database when saved.
    /// </remarks>
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

    /// <summary>
    /// Builds an assignment notification for a task when a user is assigned.
    /// </summary>
    /// <param name="task">The TaskItem that was assigned. Must contain Id and AssignedToUserId.</param>
    /// <returns>
    /// A new Notification instance with Type = NotificationType.TaskAssigned and a generated Title and Message.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when task.AssignedToUserId is null (recipient not available).</exception>
    /// <remarks>
    /// - The generated message includes the task title and, if present, a due date formatted as yyyy-MM-dd.
    /// - This method only constructs the entity; persistence (saving) is the caller's responsibility.
    /// - Consider localization of title/message outside this factory if needed.
    /// </remarks>
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