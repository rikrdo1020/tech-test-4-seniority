using api.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public enum NotificationType
{
    Generic,
    TaskAssigned,
    TaskDueSoon,
    TaskStatusChanged
}

public class Notification
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    public Guid RecipientUserId { get; set; }

    [ForeignKey(nameof(RecipientUserId))]
    public User? RecipientUser { get; set; }

    public Guid? RelatedTaskId { get; set; }

    [ForeignKey(nameof(RelatedTaskId))]
    public TaskItem? RelatedTask { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Message { get; set; }

    [Required]
    public NotificationType Type { get; set; } = NotificationType.Generic;

    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}