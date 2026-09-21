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
[Route("api/v1/timetable-events")]
public sealed class TimetableEventsController(CampusFlowDbContext database) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IEnumerable<TimetableEventResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TimetableEventResponse>>> GetAll(
        [FromQuery] Guid? moduleId,
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();
        var query = database.TimetableEvents
            .Where(e => e.User.FirebaseUid == firebaseUid);

        if (moduleId.HasValue)
        {
            query = query.Where(e => e.ModuleId == moduleId.Value);
        }

        var events = await query
            .OrderBy(e => e.DayOfWeek)
            .ThenBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

        return Ok(events.Select(e => e.ToResponse()));
    }

    [HttpPost]
    [ProducesResponseType<TimetableEventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TimetableEventResponse>> Create(
        CreateTimetableEventRequest request,
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();
        var owner = await database.Users
            .SingleAsync(user => user.FirebaseUid == firebaseUid, cancellationToken);

        // the module must actually belong to this user, otherwise someone
        // could point an event at a module they don't own
        var moduleOwned = await database.Modules
            .AnyAsync(m => m.ModuleId == request.ModuleId && m.UserId == owner.UserId, cancellationToken);
        if (!moduleOwned) return NotFound("Module not found.");

        var timetableEvent = new TimetableEventEntity
        {
            UserId = owner.UserId,
            ModuleId = request.ModuleId,
            EventType = request.EventType,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Venue = request.Venue,
            RecurrenceEndDate = request.RecurrenceEndDate
        };

        database.TimetableEvents.Add(timetableEvent);
        await database.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { }, timetableEvent.ToResponse());
    }

    [HttpPatch("{eventId:guid}")]
    [ProducesResponseType<TimetableEventResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TimetableEventResponse>> Update(
        Guid eventId,
        UpdateTimetableEventRequest request,
        CancellationToken cancellationToken)
    {
        var timetableEvent = await FindOwnedEvent(eventId, cancellationToken);
        if (timetableEvent is null) return NotFound();

        var owner = timetableEvent.UserId;
        var moduleOwned = await database.Modules
            .AnyAsync(m => m.ModuleId == request.ModuleId && m.UserId == owner, cancellationToken);
        if (!moduleOwned) return NotFound("Module not found.");

        timetableEvent.ModuleId = request.ModuleId;
        timetableEvent.EventType = request.EventType;
        timetableEvent.DayOfWeek = request.DayOfWeek;
        timetableEvent.StartTime = request.StartTime;
        timetableEvent.EndTime = request.EndTime;
        timetableEvent.Venue = request.Venue;
        timetableEvent.RecurrenceEndDate = request.RecurrenceEndDate;
        timetableEvent.UpdatedAtUtc = DateTime.UtcNow;

        await database.SaveChangesAsync(cancellationToken);
        return Ok(timetableEvent.ToResponse());
    }

    [HttpDelete("{eventId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid eventId, CancellationToken cancellationToken)
    {
        var timetableEvent = await FindOwnedEvent(eventId, cancellationToken);
        if (timetableEvent is null) return NotFound();

        database.TimetableEvents.Remove(timetableEvent);
        await database.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<TimetableEventEntity?> FindOwnedEvent(Guid eventId, CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();
        return await database.TimetableEvents
            .Include(e => e.User)
            .SingleOrDefaultAsync(
                e => e.EventId == eventId && e.User.FirebaseUid == firebaseUid,
                cancellationToken);
    }
}
