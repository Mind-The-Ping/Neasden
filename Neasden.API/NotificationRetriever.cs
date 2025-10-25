using CSharpFunctionalExtensions;
using Neasden.API.Client;
using Neasden.API.Dto;
using Neasden.API.Model;
using Neasden.Repository.Repositories;

namespace Neasden.API;

public class NotificationRetriever
{
    private readonly IWaterlooClient _waterlooClient;
    private readonly ILogger<NotificationRetriever> _logger;
    private readonly DisruptionRepository _disruptionRepository;
    private readonly NotificationRepository _notificationRepository;

    public NotificationRetriever(
        IWaterlooClient waterlooClient,
        ILogger<NotificationRetriever> logger,
        DisruptionRepository disruptionRepository,
        NotificationRepository notificationRepository)
    {
        _waterlooClient = waterlooClient ??
            throw new ArgumentNullException(nameof(waterlooClient));

        _logger = logger ??
             throw new ArgumentNullException(nameof(logger));

        _disruptionRepository = disruptionRepository ??
             throw new ArgumentNullException(nameof(disruptionRepository));

        _notificationRepository = notificationRepository ??
            throw new ArgumentNullException(nameof(notificationRepository));
    }

    public async Task<Result<NotificationReturn>> GetNotificationAsync(Guid id)
    {
        var notification = await _notificationRepository
           .GetNotificationByIdAsync(id);

        if(notification.IsFailure) 
        {
            _logger.LogError(notification.Error);
            return Result.Failure<NotificationReturn>(notification.Error);
        }

        var notificationVal = notification.Value;

        var severity = await _disruptionRepository
            .GetDisruptionSeverityByIdAsync(notificationVal.SeverityId);

        if (severity.IsFailure)
        {
            _logger.LogError(severity.Error);
            return Result.Failure<NotificationReturn>(severity.Error);
        }

        var disruption = await _disruptionRepository
            .GetDisruptionByIdAsync(notificationVal.DisruptionId);

        var disruptionDescription = await _disruptionRepository
            .GetDisruptionDescriptionByIdAsync(notificationVal.DescriptionId);

        if (disruptionDescription.IsFailure)
        {
            _logger.LogError(disruptionDescription.Error);
            return Result.Failure<NotificationReturn>(disruptionDescription.Error);
        }

        var line = await _waterlooClient.GetLineById(notificationVal.LineId);

        if (line.IsFailure)
        {
            _logger.LogError(line.Error);
            return Result.Failure<NotificationReturn>(line.Error);
        }

        var startStation = await _waterlooClient.GetStationById(notificationVal.StartStationId);

        if (startStation.IsFailure)
        {
            _logger.LogError(startStation.Error);
            return Result.Failure<NotificationReturn>(startStation.Error);
        }

        var endStation = await _waterlooClient.GetStationById(notificationVal.EndStationId);

        if (endStation.IsFailure)
        {
            _logger.LogError(endStation.Error);
            return Result.Failure<NotificationReturn>(endStation.Error);
        }

        var affectedStations = 
            new List<Station>(notificationVal.AffectedStationIds.Count);

        foreach (var stationId in notificationVal.AffectedStationIds)
        {
            var station = await _waterlooClient.GetStationById(stationId);

            if (station.IsFailure)
            {
                _logger.LogError(station.Error);
                return Result.Failure<NotificationReturn>(station.Error);
            }

            affectedStations.Add(station.Value);
        }

        return Result.Success(new NotificationReturn(
            line.Value,
            startStation.Value,
            endStation.Value,
            affectedStations,
            severity.Value.Severity,
            notificationVal.SentTime,
            disruption.Value.StartTime,
            disruption.Value.EndTime,
            disruptionDescription.Value.Description));
    }

    public async Task<Result<IEnumerable<NotificationReturn>>> GetNotificationsByUserIdAsync(Guid userId)
    {
        var notifications = await _notificationRepository
            .GetNotificationsByUserId(userId);

        if (notifications.IsFailure)
        {
            _logger.LogError(notifications.Error);
            return Result.Failure<IEnumerable<NotificationReturn>>(notifications.Error);
        }

        var results = new List<NotificationReturn>();
        var notificationsVal = notifications.Value;

        foreach (var notification in notificationsVal)
        {
            var severity = await _disruptionRepository
                .GetDisruptionSeverityByIdAsync(notification.SeverityId);

            if (severity.IsFailure)
            {
                _logger.LogError(notifications.Error);
                return Result.Failure<IEnumerable<NotificationReturn>>(notifications.Error);
            }

            results.Add(new NotificationReturn(
            notification.LineId,
            notification.DisruptionId,
            notification.StartStationId,
            notification.EndStationId,
            severity.Value.Severity,
            notification.NotificationSentBy,
            notification.SentTime));
        }

        return Result.Success<IEnumerable<NotificationReturn>>(results);
    }
}
