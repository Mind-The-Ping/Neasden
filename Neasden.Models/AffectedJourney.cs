namespace Neasden.Models;

public record AffectedJourney(
    Guid LineId,
    TimeOnly QueryTime,
    DayOfWeek QueryDay,
    IEnumerable<AffectedDisruption> Disruptions);
