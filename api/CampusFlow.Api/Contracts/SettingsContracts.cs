using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Contracts;

public sealed record UserSettingsResponse(
    string Theme,
    string Language,
    bool NotificationsEnabled,
    int PomodoroWorkMinutes,
    int PomodoroBreakMinutes);

public sealed class UpdateSettingsRequest : IValidatableObject
{
    [RegularExpression("system|light|dark")]
    public string? Theme { get; init; }

    [RegularExpression("en-ZA|zu-ZA|af-ZA")]
    public string? Language { get; init; }

    public bool? NotificationsEnabled { get; init; }

    [Range(10, 90)]
    public int? PomodoroWorkMinutes { get; init; }

    [Range(1, 30)]
    public int? PomodoroBreakMinutes { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PomodoroWorkMinutes.HasValue && PomodoroBreakMinutes.HasValue &&
            PomodoroBreakMinutes.Value >= PomodoroWorkMinutes.Value)
        {
            yield return new ValidationResult(
                "Break duration must be shorter than focus duration.",
                [nameof(PomodoroBreakMinutes)]);
        }
    }
}

