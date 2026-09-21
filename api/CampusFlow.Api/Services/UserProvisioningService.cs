using CampusFlow.Api.Data;
using CampusFlow.Api.Data.Entities;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;

namespace CampusFlow.Api.Services;

public sealed class UserProvisioningService(
    CampusFlowDbContext database,
    ILogger<UserProvisioningService> logger)
{
    public async Task<UserProfileEntity> ProvisionAsync(
        FirebaseToken token,
        CancellationToken cancellationToken)
    {
        var existingUser = await database.Users
            .Include(user => user.Settings)
            .SingleOrDefaultAsync(user => user.FirebaseUid == token.Uid, cancellationToken);

        var email = Claim(token, "email")
            ?? throw new InvalidOperationException("The Google account did not provide an email address.");
        var name = Claim(token, "name") ?? email.Split('@')[0];
        var picture = Claim(token, "picture");

        if (existingUser is not null)
        {
            existingUser.Email = email;
            existingUser.FullName = string.IsNullOrWhiteSpace(existingUser.FullName)
                ? name
                : existingUser.FullName;
            existingUser.ProfileImage = picture ?? existingUser.ProfileImage;
            existingUser.UpdatedAtUtc = DateTime.UtcNow;
            await database.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Existing CampusFlow profile refreshed for Firebase UID {FirebaseUid}", token.Uid);
            return existingUser;
        }

        var user = new UserProfileEntity
        {
            FirebaseUid = token.Uid,
            FullName = name,
            Email = email,
            ProfileImage = picture,
            Settings = new UserSettingsEntity()
        };
        database.Users.Add(user);
        await database.SaveChangesAsync(cancellationToken);
        logger.LogInformation("New CampusFlow profile created for Firebase UID {FirebaseUid}", token.Uid);
        return user;
    }

    private static string? Claim(FirebaseToken token, string name) =>
        token.Claims.TryGetValue(name, out var value) ? value?.ToString() : null;
}

