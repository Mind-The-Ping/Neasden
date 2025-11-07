namespace Neasden.Models;

public enum Severity
{
    Good = 0,
    Minor = 1,
    Severe = 2,
    Suspended = 3,
    Closed = 4
}

public class DisruptionSeverity
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public Severity Severity { get; set; }
}
