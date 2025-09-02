using api.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace api.Models.Dtos;

public class CreateTaskDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    public Guid? AssignedToExternalId { get; set; }
}