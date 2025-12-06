using Neasden.Models;

namespace Neasden.Consumer.Dto;

public record DisruptionDto(
    Guid Id,
    Line Line,
    Guid StartStationId,
    Guid EndStationId,
    Guid OriginalStartStationId,
    Guid OriginalEndStationId,
    Severity Severity,
    Guid SeverityId,
    Guid DescriptionId,
    DateTime StartTime);
