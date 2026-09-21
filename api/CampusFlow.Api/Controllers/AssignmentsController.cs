using CampusFlow.Api.Auth;
using CampusFlow.Api.Contracts;
using CampusFlow.Api.Data;
using CampusFlow.Api.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/assignments")]
public sealed class AssignmentsController(CampusFlowDbContext database)
    : ControllerBase
{
    // Return only assignments belonging to the currently authenticated user.
    [HttpGet]
    [ProducesResponseType<IEnumerable<AssignmentResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AssignmentResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();

        var assignments = await database.Assignments
            .Include(assignment => assignment.Module)
            .Include(assignment => assignment.User)
            .Where(assignment =>
                assignment.User.FirebaseUid == firebaseUid)
            .OrderBy(assignment => assignment.DueDateUtc)
            .ToListAsync(cancellationToken);

        return Ok(assignments.Select(ToResponse));
    }

    // Create an assignment only if the selected module belongs to this user.
    [HttpPost]
    [ProducesResponseType<AssignmentResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentResponse>> Create(
        CreateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();

        var owner = await database.Users
            .SingleAsync(
                user => user.FirebaseUid == firebaseUid,
                cancellationToken);

        var module = await database.Modules
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.ModuleId == request.ModuleId &&
                    candidate.UserId == owner.UserId,
                cancellationToken);

        if (module is null)
        {
            return NotFound(
                "The selected module could not be found.");
        }

        // Reject empty or whitespace-only assignment titles.
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(
                "Assignment title is required.");
        }

        // Prevent assignments from being created with expired deadlines.
        if (request.DueDateUtc <= DateTime.UtcNow)
        {
            return BadRequest(
                "The assignment due date must be in the future.");
        }

        var assignment = new AssignmentEntity
        {
            UserId = owner.UserId,
            ModuleId = module.ModuleId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            DueDateUtc = request.DueDateUtc,
            Priority = request.Priority.Trim(),
            Status = request.Status.Trim(),
            ReminderEnabled = request.ReminderEnabled
        };

        database.Assignments.Add(assignment);

        // Store an in-app reminder when reminders are enabled.
        // The Android system notification will be scheduled separately.
        if (assignment.ReminderEnabled)
        {
            database.Notifications.Add(
                CreateReminderNotification(
                    assignment,
                    owner.UserId));
        }

        await database.SaveChangesAsync(cancellationToken);

        // Module is already available from the ownership check above.
        assignment.Module = module;

        return CreatedAtAction(
            nameof(GetAll),
            new { },
            ToResponse(assignment));
    }

    // Update an existing assignment while preserving user ownership.
    [HttpPatch("{assignmentId:guid}")]
    [ProducesResponseType<AssignmentResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignmentResponse>> Update(
        Guid assignmentId,
        UpdateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await FindOwnedAssignment(
            assignmentId,
            cancellationToken);

        if (assignment is null)
        {
            return NotFound();
        }

        var firebaseUid = User.RequireFirebaseUid();

        // Ensure users cannot move an assignment into another user's module.
        var module = await database.Modules
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.ModuleId == request.ModuleId &&
                    candidate.User.FirebaseUid == firebaseUid,
                cancellationToken);

        if (module is null)
        {
            return NotFound(
                "The selected module could not be found.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(
                "Assignment title is required.");
        }

        if (request.DueDateUtc <= DateTime.UtcNow)
        {
            return BadRequest(
                "The assignment due date must be in the future.");
        }

        assignment.ModuleId = module.ModuleId;
        assignment.Module = module;
        assignment.Title = request.Title.Trim();
        assignment.Description = request.Description?.Trim();
        assignment.DueDateUtc = request.DueDateUtc;
        assignment.Priority = request.Priority.Trim();
        assignment.Status = request.Status.Trim();
        assignment.ReminderEnabled = request.ReminderEnabled;
        assignment.UpdatedAtUtc = DateTime.UtcNow;

        var existingReminder = await database.Notifications
            .SingleOrDefaultAsync(
                notification =>
                    notification.AssignmentId ==
                        assignment.AssignmentId &&
                    notification.UserId ==
                        assignment.UserId &&
                    notification.Type ==
                        "AssignmentReminder",
                cancellationToken);

        if (assignment.ReminderEnabled)
        {
            if (existingReminder is null)
            {
                // Reminder was switched on, so create its in-app record.
                database.Notifications.Add(
                    CreateReminderNotification(
                        assignment,
                        assignment.UserId));
            }
            else
            {
                // Keep reminder text synchronized with assignment edits.
                existingReminder.Title =
                    $"Assignment reminder: {assignment.Title}";

                existingReminder.Message =
                    BuildReminderMessage(assignment);
            }
        }
        else if (existingReminder is not null)
        {
            // Remove the stored reminder when the user disables it.
            database.Notifications.Remove(existingReminder);
        }

        await database.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(assignment));
    }

    // Delete only an assignment owned by the authenticated user.
    [HttpDelete("{assignmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var assignment = await FindOwnedAssignment(
            assignmentId,
            cancellationToken);

        if (assignment is null)
        {
            return NotFound();
        }

        // Explicitly remove notifications associated with this assignment.
        // This prevents stale reminders and also gives predictable behaviour
        // when using EF Core's InMemory provider during unit testing.
        var relatedNotifications = await database.Notifications
            .Where(notification =>
                notification.AssignmentId ==
                    assignment.AssignmentId &&
                notification.UserId ==
                    assignment.UserId)
            .ToListAsync(cancellationToken);

        if (relatedNotifications.Count > 0)
        {
            database.Notifications.RemoveRange(
                relatedNotifications);
        }

        database.Assignments.Remove(assignment);

        await database.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // Central ownership check prevents users accessing another user's assignments.
    private async Task<AssignmentEntity?> FindOwnedAssignment(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();

        return await database.Assignments
            .Include(assignment => assignment.Module)
            .Include(assignment => assignment.User)
            .SingleOrDefaultAsync(
                assignment =>
                    assignment.AssignmentId ==
                        assignmentId &&
                    assignment.User.FirebaseUid ==
                        firebaseUid,
                cancellationToken);
    }

    // Creates the stored in-app reminder associated with an assignment.
    private static NotificationEntity CreateReminderNotification(
        AssignmentEntity assignment,
        Guid userId)
    {
        return new NotificationEntity
        {
            UserId = userId,
            AssignmentId = assignment.AssignmentId,
            Title =
                $"Assignment reminder: {assignment.Title}",
            Message = BuildReminderMessage(assignment),
            Type = "AssignmentReminder",
            IsRead = false
        };
    }

    // Generates consistent reminder text for create and update operations.
    private static string BuildReminderMessage(
        AssignmentEntity assignment)
    {
        return $"{assignment.Title} is due on " +
               $"{assignment.DueDateUtc:dd MMM yyyy 'at' HH:mm} UTC.";
    }

    // Convert the database entity into the DTO returned to the Android client.
    private static AssignmentResponse ToResponse(
        AssignmentEntity assignment)
    {
        return new AssignmentResponse(
            assignment.AssignmentId,
            assignment.ModuleId,
            assignment.Module.ModuleCode,
            assignment.Module.ModuleName,
            assignment.Title,
            assignment.Description,
            assignment.DueDateUtc,
            assignment.Priority,
            assignment.Status,
            assignment.ReminderEnabled);
    }
}