using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Contracts;

public sealed record ModuleResponse(
    Guid ModuleId,
    string ModuleCode,
    string ModuleName,
    string? Lecturer,
    string? Colour);

public sealed class CreateModuleRequest
{
    [Required, MaxLength(20)]
    public string ModuleCode { get; init; } = string.Empty;

    [Required, MaxLength(150)]
    public string ModuleName { get; init; } = string.Empty;

    [MaxLength(150)]
    public string? Lecturer { get; init; }

    [MaxLength(20)]
    public string? Colour { get; init; }
}

public sealed class UpdateModuleRequest
{
    [Required, MaxLength(20)]
    public string ModuleCode { get; init; } = string.Empty;

    [Required, MaxLength(150)]
    public string ModuleName { get; init; } = string.Empty;

    [MaxLength(150)]
    public string? Lecturer { get; init; }

    [MaxLength(20)]
    public string? Colour { get; init; }
}
