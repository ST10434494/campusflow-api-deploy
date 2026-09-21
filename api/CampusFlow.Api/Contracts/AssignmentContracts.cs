using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Contracts;

public sealed record AssignmentResponse(
    Guid AssignmentId,
    Guid ModuleId,
    string ModuleCode,
    string ModuleName,
    string Title,
    string? Description,
    DateTime DueDateUtc,
    string Priority,
    string Status,
    bool ReminderEnabled);

public sealed class CreateAssignmentRequest
{
    [Required]
    public Guid ModuleId { get; init; }

    [Required, MaxLength(150)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Required]
    public DateTime DueDateUtc { get; init; }

    [Required, MaxLength(20)]
    public string Priority { get; init; } = "Medium";

    [Required, MaxLength(20)]
    public string Status { get; init; } = "Pending";

    public bool ReminderEnabled { get; init; } = true;
}

public sealed class UpdateAssignmentRequest
{
    [Required]
    public Guid ModuleId { get; init; }

    [Required, MaxLength(150)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Required]
    public DateTime DueDateUtc { get; init; }

    // Only supported assignment priority levels are accepted by the API.
    [Required, MaxLength(20)]
    [RegularExpression("^(Low|Medium|High)$",
        ErrorMessage = "Priority must be Low, Medium, or High.")]
    public string Priority { get; init; } = "Medium";

    // Restrict assignment status to the states supported by CampusFlow.
    [Required, MaxLength(20)]
    [RegularExpression("^(Pending|In Progress|Completed)$",
        ErrorMessage = "Status must be Pending, In Progress, or Completed.")]
    public string Status { get; init; } = "Pending";

    // Controls whether CampusFlow should generate reminder notifications.
    public bool ReminderEnabled { get; init; } = true;
}