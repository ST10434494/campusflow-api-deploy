using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Data.Entities;

public sealed class AssignmentEntity
{
    [Key]
    public Guid AssignmentId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid ModuleId { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime DueDateUtc { get; set; }

    [Required, MaxLength(20)]
    public string Priority { get; set; } = "Medium";

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Pending";

    public bool ReminderEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public UserProfileEntity User { get; set; } = null!;

    public ModuleEntity Module { get; set; } = null!;
}