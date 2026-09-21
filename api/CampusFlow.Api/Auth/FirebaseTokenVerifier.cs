using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace CampusFlow.Api.Auth;

/// <summary>
/// Centralises Firebase Admin initialisation and Firebase ID-token verification.
/// Credentials are supplied through environment variables in Render.
/// </summary>
public sealed class FirebaseTokenVerifier
{
    private readonly FirebaseAuth _firebaseAuth;

    public FirebaseTokenVerifier(IConfiguration configuration)
    {
        var projectId = configuration["Firebase:ProjectId"];

        if (string.IsNullOrWhiteSpace(projectId) ||
            projectId.StartsWith("REPLACE_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Firebase project ID is not configured. " +
                "Set the Firebase__ProjectId environment variable.");
        }

        var serviceAccountJson =
            configuration["Firebase:ServiceAccountJson"];

        if (string.IsNullOrWhiteSpace(serviceAccountJson))
        {
            throw new InvalidOperationException(
                "Firebase service-account credentials are not configured. " +
                "Set the Firebase__ServiceAccountJson environment variable.");
        }

        GoogleCredential credential;

        try
        {
            credential = GoogleCredential.FromJson(serviceAccountJson);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Firebase__ServiceAccountJson does not contain valid " +
                "Firebase service-account JSON.",
                exception);
        }

        /*
         * FirebaseApp.DefaultInstance returns null when the default Firebase
         * application has not been created. It does not necessarily throw an
         * InvalidOperationException, so the null value must be checked.
         */
        var firebaseApp = FirebaseApp.DefaultInstance;

        if (firebaseApp is null)
        {
            firebaseApp = FirebaseApp.Create(new AppOptions
            {
                Credential = credential,
                ProjectId = projectId
            });
        }

        _firebaseAuth = FirebaseAuth.GetAuth(firebaseApp);
    }

    /// <summary>
    /// Verifies a Firebase ID token sent by the Android application.
    /// </summary>
    public Task<FirebaseToken> VerifyAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new ArgumentException(
                "A Firebase ID token is required.",
                nameof(idToken));
        }

        return _firebaseAuth.VerifyIdTokenAsync(
            idToken,
            cancellationToken);
    }
}
