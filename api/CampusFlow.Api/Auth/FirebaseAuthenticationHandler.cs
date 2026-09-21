using System.Security.Claims;
using System.Text.Encodings.Web;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CampusFlow.Api.Auth;

public sealed class FirebaseAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    FirebaseTokenVerifier verifier)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Firebase";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var idToken = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return AuthenticateResult.Fail("Bearer token is empty.");
        }

        try
        {
            FirebaseToken token = await verifier.VerifyAsync(idToken, Context.RequestAborted);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, token.Uid),
                new("firebase_uid", token.Uid)
            };

            AddOptionalClaim(token, claims, "email", ClaimTypes.Email);
            AddOptionalClaim(token, claims, "name", ClaimTypes.Name);

            var identity = new ClaimsIdentity(claims, SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return AuthenticateResult.Success(ticket);
        }
        catch (FirebaseAuthException error)
        {
            Logger.LogWarning(error, "Firebase rejected an API bearer token");
            return AuthenticateResult.Fail("Firebase ID token is invalid or expired.");
        }
    }

    private static void AddOptionalClaim(
        FirebaseToken token,
        ICollection<Claim> claims,
        string sourceName,
        string targetName)
    {
        if (token.Claims.TryGetValue(sourceName, out var value) && value is not null)
        {
            claims.Add(new Claim(targetName, value.ToString()!));
        }
    }
}

