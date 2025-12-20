namespace Neasden.Models;

public record UnReadNotification(
    Guid UserId,
    Guid NotificationId,
    DateTime CreatedAt);