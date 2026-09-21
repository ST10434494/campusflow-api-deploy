using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Data.Entities;

public sealed class UserSettingsEntity
{
    [Key]
    public Guid UserId { get; set; }

    [Required, MaxLength(10)]
    public string Theme { get; set; } = "system";

    [Required, MaxLength(10)]
    public string Language { get; set; } = "en-ZA";

    public bool NotificationsEnabled { get; set; } = true;

    [Range(10, 90)]
    public int PomodoroWorkMinutes { get; set; } = 25;

    [Range(1, 30)]
    public int PomodoroBreakMinutes { get; set; } = 5;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public UserProfileEntity User { get; set; } = null!;
}

