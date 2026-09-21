using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace CampusFlow.Api.Auth;

/// <summary>
/// Centralises Firebase Admin initialisation and ID-token verification.
/// Credentials are supplied by GOOGLE_APPLICATION_CREDENTIALS locally or
/// by an Azure App Service environment setting in the hosted prototype.
/// </summary>
public sealed class FirebaseTokenVerifier
{
    private readonly FirebaseAuth _firebaseAuth;

    public FirebaseTokenVerifier(IConfiguration configuration)
    {
        var projectId = configuration["Firebase:ProjectId"];
        if (string.IsNullOrWhiteSpace(projectId) || projectId.StartsWith("REPLACE_"))
        {
            throw new InvalidOperationException(
                "Firebase:ProjectId is not configured. Set Firebase__ProjectId before starting the API.");
        }

        var serviceAccountJson = configuration["Firebase:ServiceAccountJson"];
        var credential = string.IsNullOrWhiteSpace(serviceAccountJson)
            ? GoogleCredential.GetApplicationDefault()
            : GoogleCredential.FromJson(serviceAccountJson);

        FirebaseApp app;
        try
        {
            app = FirebaseApp.DefaultInstance;
        }
        catch (InvalidOperationException)
        {
            app = FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = projectId
            });
        }

        _firebaseAuth = FirebaseAuth.GetAuth(app);
    }

    public Task<FirebaseToken> VerifyAsync(string idToken, CancellationToken cancellationToken = default) =>
        _firebaseAuth.VerifyIdTokenAsync(idToken, cancellationToken);
}
