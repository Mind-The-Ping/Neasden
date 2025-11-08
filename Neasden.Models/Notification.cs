namespace Neasden.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LineId { get; set; }
    public Guid DisruptionId { get; set; }
    public Guid StartStationId { get; set; }
    public Guid EndStationId { get; set; }
    public Guid SeverityId { get; set; }
    public Guid DescriptionId { get; set; }
    public DateTime SentTime { get; set; }
    public required IList<Guid> AffectedStationIds { get; set; }
}
