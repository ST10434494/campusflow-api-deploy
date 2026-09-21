using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Contracts;

public sealed class GoogleAuthRequest
{
    [Required(ErrorMessage = "A Firebase ID token is required.")]
    public string IdToken { get; init; } = string.Empty;
}

public sealed record AuthResponse(
    UserProfileResponse User,
    UserSettingsResponse Settings);

