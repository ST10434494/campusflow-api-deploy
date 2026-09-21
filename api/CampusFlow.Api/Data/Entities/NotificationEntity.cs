using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Data.Entities;

/// <summary>
/// Represents an in-app notification belonging to a CampusFlow user.
/// Notifications may optionally be linked to an assignment.
/// </summary>
public sealed class NotificationEntity
{
    [Key]
    public Guid NotificationId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    // Nullable because not every notification must originate from an assignment.
    public Guid? AssignmentId { get; set; }

    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Type { get; set; } = "AssignmentReminder";

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties.
    public UserProfileEntity User { get; set; } = null!;

    public AssignmentEntity? Assignment { get; set; }
}