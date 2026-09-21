using System.ComponentModel.DataAnnotations;

namespace CampusFlow.Api.Contracts;

public sealed record GoogleAuthRequest(
    [property: Required] string IdToken);

public sealed record AuthResponse(
    UserProfileResponse User,
    UserSettingsResponse Settings);

