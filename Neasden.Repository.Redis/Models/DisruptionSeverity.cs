namespace Neasden.Repository.Redis.Models;

public enum Severity
{
    Good = 0,
    Minor = 1,
    Severe = 2,
    Suspended = 3,
    Closed = 4
}

public record DisruptionSeverity(
    Guid Id,
    Guid DisruptionId,
    DateTime StartTime,
    Severity Severity);
