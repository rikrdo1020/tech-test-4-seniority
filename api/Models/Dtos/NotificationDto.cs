
public record CreateNotificationDto(
    Guid RecipientUserId,
    string Title,
    string? Message,
    NotificationType Type = NotificationType.Generic,
    Guid? RelatedTaskId = null
);

public record NotificationDto(
    Guid Id,
    Guid RecipientUserId,
    Guid? RelatedTaskId,
    string Title,
    string? Message,
    NotificationType Type,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt
);