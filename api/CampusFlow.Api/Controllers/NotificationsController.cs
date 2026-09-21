using CampusFlow.Api.Auth;
using CampusFlow.Api.Contracts;
using CampusFlow.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController(
    CampusFlowDbContext database
) : ControllerBase
{
    /// <summary>
    /// Returns all notifications belonging to the currently
    /// authenticated user, with newest notifications first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IEnumerable<NotificationResponse>>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();

        var notifications = await database.Notifications
            .AsNoTracking()
            .Include(notification => notification.User)
            .Where(notification =>
                notification.User.FirebaseUid == firebaseUid)
            .OrderByDescending(notification =>
                notification.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Ok(notifications.Select(ToResponse));
    }

    /// <summary>
    /// Updates the read/unread status of a notification.
    /// Users can only update notifications that belong to them.
    /// </summary>
    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType<NotificationResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationResponse>> UpdateReadStatus(
        Guid notificationId,
        UpdateNotificationReadStatusRequest request,
        CancellationToken cancellationToken)
    {
        var notification = await FindOwnedNotification(
            notificationId,
            cancellationToken);

        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = request.IsRead;

        await database.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(notification));
    }

    /// <summary>
    /// Marks every unread notification belonging to the
    /// authenticated user as read.
    /// </summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();

        var unreadNotifications = await database.Notifications
            .Include(notification => notification.User)
            .Where(notification =>
                notification.User.FirebaseUid == firebaseUid &&
                !notification.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await database.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Finds a notification only when it belongs to the
    /// currently authenticated Firebase user.
    /// </summary>
    private async Task<Data.Entities.NotificationEntity?>
        FindOwnedNotification(
            Guid notificationId,
            CancellationToken cancellationToken)
    {
        var firebaseUid = User.RequireFirebaseUid();

        return await database.Notifications
            .Include(notification => notification.User)
            .SingleOrDefaultAsync(
                notification =>
                    notification.NotificationId == notificationId &&
                    notification.User.FirebaseUid == firebaseUid,
                cancellationToken);
    }

    /// <summary>
    /// Converts the database entity into the DTO returned
    /// to the Android client.
    /// </summary>
    private static NotificationResponse ToResponse(
        Data.Entities.NotificationEntity notification)
    {
        return new NotificationResponse(
            notification.NotificationId,
            notification.AssignmentId,
            notification.Title,
            notification.Message,
            notification.Type,
            notification.IsRead,
            notification.CreatedAtUtc);
    }
}