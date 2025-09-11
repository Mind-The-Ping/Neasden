namespace Neasden.Repository.Redis.Models;

public enum NotificationSentBy
{
    Sms,
    Push,
    Failed
}

public record Notification(
    Guid Id,
    Guid UserId,
    Guid LineId,
    Guid DisruptionId,
    Guid StartStationId,
    Guid EndStationId,
    Guid SeverityId,
    NotificationSentBy NotificationSentBy,
    DateTime SentTime);
