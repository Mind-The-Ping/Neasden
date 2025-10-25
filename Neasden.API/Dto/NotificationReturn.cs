using Neasden.API.Model;
using Neasden.Models;

namespace Neasden.API.Dto;

public record NotificationReturn(
    Line Line, 
    Station StartStation,
    Station EndStation,
    IEnumerable<Station> AffectedStations,
    Severity Severity,
    DateTime SentDate,
    DateTime DisruptionStart,
    DateTime DisruptionEnd,
    string DisruptionDescription);
