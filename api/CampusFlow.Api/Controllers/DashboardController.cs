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
[Route("api/v1/dashboard")]
public sealed class DashboardController(CampusFlowDbContext database) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DashboardSummaryResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryResponse>> Get(CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();

        var moduleCount = await database.Modules
            .CountAsync(m => m.User.FirebaseUid == firebaseUid, cancellationToken);

        // events repeat weekly by day of week, so the full list already is
        // the week's schedule, no separate date range needed
        var upcomingEvents = await database.TimetableEvents
            .Where(e => e.User.FirebaseUid == firebaseUid)
            .OrderBy(e => e.DayOfWeek)
            .ThenBy(e => e.StartTime)
            .Take(5)
            .ToListAsync(cancellationToken);

        return Ok(new DashboardSummaryResponse(
            moduleCount,
            upcomingEvents.Select(e => e.ToResponse()).ToList()));
    }
}
