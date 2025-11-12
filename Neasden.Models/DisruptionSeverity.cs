namespace Neasden.Models;

public enum Severity
{
    Minor = 0,
    Severe = 1,
    Closed = 2
}

public class DisruptionSeverity
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public Severity Severity { get; set; }
}
