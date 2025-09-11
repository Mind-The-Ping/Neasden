namespace Neasden.Repository.Redis.Models;

public record Disruption(
    Guid Id,
    Guid LineId,
    Guid StartStationId,
    Guid EndStationId,
    string Description,
    DateTime StartTime,
    DateTime EndTime);
