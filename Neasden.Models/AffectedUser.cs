namespace Neasden.Models;

public record AffectedUser(
    Guid Id,
    Guid UserId,
    Station StartStation,
    Station EndStation,
    IEnumerable<Station> AffectedStations,
    TimeOnly EndTime);
