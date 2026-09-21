using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Data.Entities;

public sealed class TimetableEventEntity
{
    [Key]
    public Guid EventId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public Guid ModuleId { get; set; }

    [Required, MaxLength(20)]
    public string EventType { get; set; } = string.Empty;

    [Range(1, 7)]
    public int DayOfWeek { get; set; }

    [Required, MaxLength(5)]
    public string StartTime { get; set; } = string.Empty;

    [Required, MaxLength(5)]
    public string EndTime { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Venue { get; set; }

    public DateOnly? RecurrenceEndDate { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public UserProfileEntity User { get; set; } = null!;
    public ModuleEntity Module { get; set; } = null!;
}
