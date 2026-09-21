namespace CampusFlow.Api.Contracts;


// Notification data returned to the Android application.

public sealed record NotificationResponse(
    Guid NotificationId,
    Guid? AssignmentId,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTime CreatedAtUtc
);


// Request used to update the read status of a notification.
public sealed record UpdateNotificationReadStatusRequest(
    bool IsRead
);
