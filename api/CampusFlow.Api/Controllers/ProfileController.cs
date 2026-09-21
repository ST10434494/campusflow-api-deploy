using CampusFlow.Api.Auth;
using CampusFlow.Api.Contracts;
using CampusFlow.Api.Data;
using CampusFlow.Api.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/profile")]
public sealed class ProfileController(CampusFlowDbContext database) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> Get(CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();
        var user = await database.Users
            .SingleOrDefaultAsync(item => item.FirebaseUid == firebaseUid, cancellationToken);
        return user is null ? NotFound() : Ok(user.ToResponse());
    }

    [HttpPatch]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileResponse>> Patch(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();
        var user = await database.Users
            .SingleOrDefaultAsync(item => item.FirebaseUid == firebaseUid, cancellationToken);
        if (user is null) return NotFound();

        if (request.FullName is not null) user.FullName = request.FullName.Trim();
        if (request.Institution is not null) user.Institution = NullWhenBlank(request.Institution);
        if (request.Course is not null) user.Course = NullWhenBlank(request.Course);
        if (request.YearOfStudy.HasValue) user.YearOfStudy = request.YearOfStudy;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await database.SaveChangesAsync(cancellationToken);
        return Ok(user.ToResponse());
    }

    private static string? NullWhenBlank(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

