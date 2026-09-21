using CampusFlow.Api.Auth;
using CampusFlow.Api.Contracts;
using CampusFlow.Api.Mappings;
using CampusFlow.Api.Services;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusFlow.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    FirebaseTokenVerifier verifier,
    UserProvisioningService users,
    ILogger<AuthController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("google")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Google(
        GoogleAuthRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var firebaseToken = await verifier.VerifyAsync(request.IdToken, cancellationToken);
            var user = await users.ProvisionAsync(firebaseToken, cancellationToken);
            return Ok(new AuthResponse(user.ToResponse(), user.Settings.ToResponse()));
        }
        catch (FirebaseAuthException error)
        {
            logger.LogWarning(error, "Firebase rejected the Google authentication exchange");
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed",
                detail: "The Firebase ID token is invalid or expired.");
        }
    }
}

