using Neasden.Models;

namespace Neasden.API.Dto;

public record NotificationReturn(
    Guid LineId, 
    Guid DisruptionId, 
    Guid StartStationId,
    Guid EndStationId,
    Severity Severity,
    NotificationSentBy NotificationSentBy,
    DateTime SentTime);
