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
[Route("api/v1/modules")]
public sealed class ModulesController(CampusFlowDbContext database) : ControllerBase
{
    // Return all modules belonging to the currently authenticated user.
    [HttpGet]
    [ProducesResponseType<IEnumerable<ModuleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ModuleResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();

        var modules = await database.Modules
            .Where(module => module.User.FirebaseUid == firebaseUid)
            .OrderBy(module => module.ModuleCode)
            .ToListAsync(cancellationToken);

        return Ok(modules.Select(module => module.ToResponse()));
    }

    // Return a single module only if it belongs to the authenticated user.
    [HttpGet("{moduleId:guid}")]
    [ProducesResponseType<ModuleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleResponse>> GetOne(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var module = await FindOwnedModule(moduleId, cancellationToken);

        return module is null
            ? NotFound()
            : Ok(module.ToResponse());
    }

    // Create a new module for the authenticated user.
    [HttpPost]
    [ProducesResponseType<ModuleResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ModuleResponse>> Create(
        CreateModuleRequest request,
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();

        var owner = await database.Users
            .SingleAsync(
                user => user.FirebaseUid == firebaseUid,
                cancellationToken);

        var module = new ModuleEntity
        {
            UserId = owner.UserId,
            ModuleCode = request.ModuleCode,
            ModuleName = request.ModuleName,
            Lecturer = request.Lecturer,
            Colour = request.Colour
        };

        database.Modules.Add(module);
        await database.SaveChangesAsync(cancellationToken);

        // The created module is returned in the response body.
        return CreatedAtAction(
            nameof(GetAll),
            new { },
            module.ToResponse());
    }

    // Update an existing module owned by the authenticated user.
    [HttpPatch("{moduleId:guid}")]
    [ProducesResponseType<ModuleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleResponse>> Update(
        Guid moduleId,
        UpdateModuleRequest request,
        CancellationToken cancellationToken)
    {
        var module = await FindOwnedModule(moduleId, cancellationToken);

        if (module is null)
        {
            return NotFound();
        }

        module.ModuleCode = request.ModuleCode;
        module.ModuleName = request.ModuleName;
        module.Lecturer = request.Lecturer;
        module.Colour = request.Colour;
        module.UpdatedAtUtc = DateTime.UtcNow;

        await database.SaveChangesAsync(cancellationToken);

        return Ok(module.ToResponse());
    }

    // Delete a module only when no timetable events or assignments depend on it.
    [HttpDelete("{moduleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var module = await FindOwnedModule(moduleId, cancellationToken);

        if (module is null)
        {
            return NotFound();
        }

        // Timetable events reference modules, so they must be removed first.
        var hasTimetableEvents = await database.TimetableEvents
            .AnyAsync(
                timetableEvent => timetableEvent.ModuleId == moduleId,
                cancellationToken);

        if (hasTimetableEvents)
        {
            return Conflict(
                "Remove this module's timetable events before deleting the module.");
        }

        // Assignments also reference modules and must not be orphaned.
        var hasAssignments = await database.Assignments
            .AnyAsync(
                assignment => assignment.ModuleId == moduleId,
                cancellationToken);

        if (hasAssignments)
        {
            return Conflict(
                "Remove this module's assignments before deleting the module.");
        }

        database.Modules.Remove(module);
        await database.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // Central ownership check prevents access to another user's modules.
    private async Task<ModuleEntity?> FindOwnedModule(
        Guid moduleId,
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();

        return await database.Modules
            .Include(module => module.User)
            .SingleOrDefaultAsync(
                module =>
                    module.ModuleId == moduleId &&
                    module.User.FirebaseUid == firebaseUid,
                cancellationToken);
    }
}