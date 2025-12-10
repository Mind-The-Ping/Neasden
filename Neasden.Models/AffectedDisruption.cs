namespace Neasden.Models;

public record AffectedDisruption(
    Guid Id,
    Guid StartStationId,
    Guid EndStationId,
    Severity Serverity);