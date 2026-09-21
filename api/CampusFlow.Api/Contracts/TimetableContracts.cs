using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Contracts;

public sealed record TimetableEventResponse(
    Guid EventId,
    Guid ModuleId,
    string EventType,
    int DayOfWeek,
    string StartTime,
    string EndTime,
    string? Venue,
    DateOnly? RecurrenceEndDate);

public sealed class CreateTimetableEventRequest : IValidatableObject
{
    [Required]
    public Guid ModuleId { get; init; }

    [Required, RegularExpression("lecture|tutorial|practical")]
    public string EventType { get; init; } = string.Empty;

    [Range(1, 7)]
    public int DayOfWeek { get; init; }

    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")]
    public string StartTime { get; init; } = string.Empty;

    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")]
    public string EndTime { get; init; } = string.Empty;

    [MaxLength(150)]
    public string? Venue { get; init; }

    public DateOnly? RecurrenceEndDate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(StartTime) && !string.IsNullOrEmpty(EndTime) &&
            string.CompareOrdinal(EndTime, StartTime) <= 0)
        {
            yield return new ValidationResult(
                "End time must be after start time.",
                [nameof(EndTime)]);
        }
    }
}

public sealed class UpdateTimetableEventRequest : IValidatableObject
{
    [Required]
    public Guid ModuleId { get; init; }

    [Required, RegularExpression("lecture|tutorial|practical")]
    public string EventType { get; init; } = string.Empty;

    [Range(1, 7)]
    public int DayOfWeek { get; init; }

    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")]
    public string StartTime { get; init; } = string.Empty;

    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")]
    public string EndTime { get; init; } = string.Empty;

    [MaxLength(150)]
    public string? Venue { get; init; }

    public DateOnly? RecurrenceEndDate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(StartTime) && !string.IsNullOrEmpty(EndTime) &&
            string.CompareOrdinal(EndTime, StartTime) <= 0)
        {
            yield return new ValidationResult(
                "End time must be after start time.",
                [nameof(EndTime)]);
        }
    }
}
