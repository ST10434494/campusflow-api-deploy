using System.Security.Claims;

namespace CampusFlow.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static string RequireFirebaseUid(this ClaimsPrincipal principal) =>
        principal.FindFirstValue("firebase_uid")
        ?? throw new UnauthorizedAccessException("Verified Firebase user ID is missing.");
}

