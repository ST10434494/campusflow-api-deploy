using CampusFlow.Api.Auth;
using CampusFlow.Api.Contracts;
using CampusFlow.Api.Data;
using CampusFlow.Api.Data.Entities;
using CampusFlow.Api.Mappings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/settings")]
public sealed class SettingsController(CampusFlowDbContext database) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UserSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSettingsResponse>> Get(CancellationToken cancellationToken)
    {
        var settings = await FindSettings(cancellationToken);
        return settings is null ? NotFound() : Ok(settings.ToResponse());
    }

    [HttpPatch]
    [ProducesResponseType<UserSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSettingsResponse>> Patch(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var settings = await FindSettings(cancellationToken);
        if (settings is null) return NotFound();

        if (request.Theme is not null) settings.Theme = request.Theme;
        if (request.Language is not null) settings.Language = request.Language;
        if (request.NotificationsEnabled.HasValue)
            settings.NotificationsEnabled = request.NotificationsEnabled.Value;
        if (request.PomodoroWorkMinutes.HasValue)
            settings.PomodoroWorkMinutes = request.PomodoroWorkMinutes.Value;
        if (request.PomodoroBreakMinutes.HasValue)
            settings.PomodoroBreakMinutes = request.PomodoroBreakMinutes.Value;

        if (settings.PomodoroBreakMinutes >= settings.PomodoroWorkMinutes)
        {
            ModelState.AddModelError(
                nameof(request.PomodoroBreakMinutes),
                "Break duration must be shorter than focus duration.");
            return ValidationProblem(ModelState);
        }

        settings.UpdatedAtUtc = DateTime.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        return Ok(settings.ToResponse());
    }

    private async Task<UserSettingsEntity?> FindSettings(
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();
        return await database.UserSettings
            .Include(settings => settings.User)
            .SingleOrDefaultAsync(
                settings => settings.User.FirebaseUid == firebaseUid,
                cancellationToken);
    }
}
