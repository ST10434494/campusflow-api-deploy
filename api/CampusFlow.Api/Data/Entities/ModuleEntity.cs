using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Data.Entities;

public sealed class ModuleEntity
{
    [Key]
    public Guid ModuleId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    [Required, MaxLength(20)]
    public string ModuleCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string ModuleName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Lecturer { get; set; }

    [MaxLength(20)]
    public string? Colour { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public UserProfileEntity User { get; set; } = null!;
    public ICollection<TimetableEventEntity> TimetableEvents { get; set; } = new List<TimetableEventEntity>();
}
