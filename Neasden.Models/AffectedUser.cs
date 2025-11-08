namespace Neasden.Models;

public record AffectedUser(
    Guid Id,
    Station StartStation,
    Station EndStation,
    IEnumerable<Station> AffectedStations,
    TimeOnly EndTime);
