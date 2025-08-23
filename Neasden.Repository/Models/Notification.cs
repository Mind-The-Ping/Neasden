namespace Neasden.Repository.Models;

public enum NotificationSentBy
{
    Sms,
    Push,
    Failed
}

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LineId { get; set; }
    public Guid UserId { get; set; }
    public Guid DisruptionId { get; set; }
    public Guid StartStationId { get; set; }
    public Guid EndStationId { get; set; }
    public NotificationSentBy NotificationSentBy { get; set; }
    public DateTime DateTime { get; set; }
}
