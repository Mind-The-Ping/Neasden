using Neasden.Models;

namespace Neasden.API.Dto;

public record NotificationReturn(
    Line Line, 
    Station JourneyStartStation,
    Station JourneyEndStation,
    Station DisruptionStartStation,
    Station DisruptionEndStation,
    IEnumerable<Station> AffectedStations,
    Severity Severity,
    DateTime SentDate,
    DateTime DisruptionStart,
    DateTime DisruptionEnd,
    string DisruptionDescription);
