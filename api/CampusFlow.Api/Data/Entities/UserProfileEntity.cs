using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Data.Entities;

public sealed class UserProfileEntity
{
    [Key]
    public Guid UserId { get; set; } = Guid.NewGuid();

    [Required, MaxLength(128)]
    public string FirebaseUid { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ProfileImage { get; set; }

    [MaxLength(150)]
    public string? Institution { get; set; }

    [MaxLength(150)]
    public string? Course { get; set; }

    [Range(1, 10)]
    public int? YearOfStudy { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public UserSettingsEntity Settings { get; set; } = null!;
}

