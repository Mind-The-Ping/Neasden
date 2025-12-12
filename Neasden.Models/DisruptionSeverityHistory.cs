namespace Neasden.Models;

public class DisruptionSeverityHistory
{
    public Guid Id { get; set; }
    public Guid DisruptionId { get; set; }
    public Severity CurrentSeverity { get; set; }
    public Severity? PreviousSeverity { get; set; }
    public DateTime CreatedAt { get; set; }
}
