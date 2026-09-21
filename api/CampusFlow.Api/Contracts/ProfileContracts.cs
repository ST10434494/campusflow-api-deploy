using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Contracts;

public sealed record UserProfileResponse(
    Guid UserId,
    string FullName,
    string Email,
    string? ProfileImage,
    string? Institution,
    string? Course,
    int? YearOfStudy);

public sealed class UpdateProfileRequest
{
    [MinLength(2), MaxLength(100)]
    public string? FullName { get; init; }

    [MaxLength(150)]
    public string? Institution { get; init; }

    [MaxLength(150)]
    public string? Course { get; init; }

    [Range(1, 10)]
    public int? YearOfStudy { get; init; }
}

