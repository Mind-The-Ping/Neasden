using CSharpFunctionalExtensions;
using Neasden.API.Dto;
using Neasden.Library.Clients;
using Neasden.Models;
using Neasden.Repository;
using Neasden.Repository.Read;

namespace Neasden.API;

public class NotificationRetriever
{
    private readonly IWaterlooClient _waterlooClient;
    private readonly ILogger<NotificationRetriever> _logger;
    private readonly ReadDisruptionRepository _disruptionRepository;
    private readonly ReadNotificationRepository _notificationRepository;

    public NotificationRetriever(
        IWaterlooClient waterlooClient,
        ILogger<NotificationRetriever> logger,
        ReadDisruptionRepository disruptionRepository,
        ReadNotificationRepository notificationRepository)
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

    public async Task<Result<NotificationReturn>> GetNotificiationAsnyc(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetNotificationByIdAsync(id);

        if(notification.IsFailure)
        {
            _logger.LogError(notification.Error);
            return Result.Failure<NotificationReturn>(notification.Error);
        }

        return await BuildNotificationReturnAsync(notification.Value, cancellationToken);
    }

    public async Task<Result<PaginatedResult<NotificationReturn>>> GetNotificationsByUserIdAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var pagedNotificationsResult = await _notificationRepository.GetNotificationIdsByUserIdAsync(userId, page, pageSize);

        if (pagedNotificationsResult.IsFailure)
        {
            _logger.LogError(pagedNotificationsResult.Error);
            return Result.Failure<PaginatedResult<NotificationReturn>>(pagedNotificationsResult.Error);
        }

        var notifications = pagedNotificationsResult.Value.Items;
        if (!notifications.Any()) {
            return Result.Success(new PaginatedResult<NotificationReturn>(
                Enumerable.Empty<NotificationReturn>(), 
                page, 
                pageSize, 
                0));
        }

        var buildTasks = notifications
            .Select(n => BuildNotificationReturnAsync(n, cancellationToken))
            .ToList();

        var builtResults = await Task.WhenAll(buildTasks);

        var successfulNotifications = new List<NotificationReturn>();

        foreach(var result in builtResults)
        {
            if(result.IsSuccess) {
                successfulNotifications.Add(result.Value);
            }
            else {
                _logger.LogWarning("Failed to build paginated notification: {Error}", result.Error);
            }
        }

        var paginatedResult = new PaginatedResult<NotificationReturn>(
            successfulNotifications,
            page,
            pageSize,
            pagedNotificationsResult.Value.TotalCount);

        return Result.Success(paginatedResult);
    }

    public async Task<Result<PaginatedResult<NotificationReturn>>> GetNotificationsByUserLatestIdAsync(
       Guid userId,
       DateTime lastChecked,
       CancellationToken cancellationToken = default)
    {
        var pagedNotificationsResult = await _notificationRepository.GetNotificationIdsByUserIdLatestAsync(userId, lastChecked);

        if (pagedNotificationsResult.IsFailure)
        {
            _logger.LogError(pagedNotificationsResult.Error);
            return Result.Failure<PaginatedResult<NotificationReturn>>(pagedNotificationsResult.Error);
        }

        var notifications = pagedNotificationsResult.Value.Items;
        if (!notifications.Any())
        {
            return Result.Success(new PaginatedResult<NotificationReturn>(
                [],
                1,
                0,
                0));
        }

        var buildTasks = notifications
           .Select(n => BuildNotificationReturnAsync(n, cancellationToken))
           .ToList();

        var builtResults = await Task.WhenAll(buildTasks);

        var successfulNotifications = new List<NotificationReturn>();

        foreach (var result in builtResults)
        {
            if (result.IsSuccess) {
                successfulNotifications.Add(result.Value);
            }
            else {
                _logger.LogWarning("Failed to build latest notifications: {Error}", result.Error);
            }
        }

        var paginatedResult = new PaginatedResult<NotificationReturn>(
            successfulNotifications,
            1,
            pagedNotificationsResult.Value.Items.Count(),
            pagedNotificationsResult.Value.TotalCount);

        return Result.Success(paginatedResult);
    }

    private async Task<Result<NotificationReturn>> BuildNotificationReturnAsync(
        Notification notification, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lineTask = _waterlooClient.GetLinesById([notification.LineId], cancellationToken);
            var journeyStartStationTask = _waterlooClient.GetStationsById([notification.StartStationId], cancellationToken);
            var journeyEndStationTask = _waterlooClient.GetStationsById([notification.EndStationId], cancellationToken);
            var affectedStationsTask = _waterlooClient.GetStationsById(notification.AffectedStationIds, cancellationToken);
            var severityTask = _disruptionRepository.GetDisruptionSeverityByIdAsync(notification.SeverityId);
            var disruptionTask = _disruptionRepository.GetDisruptionByIdAsync(notification.DisruptionId);
            var descriptionTask = _disruptionRepository.GetDisruptionDescriptionByIdAsync(notification.DescriptionId);


            await Task.WhenAll(
                lineTask, 
                journeyStartStationTask, 
                journeyEndStationTask, 
                affectedStationsTask,
                severityTask,
                disruptionTask,
                descriptionTask);

            if (severityTask.Result.IsFailure) {
                return Result.Failure<NotificationReturn>(severityTask.Result.Error);
            }

           
            if (disruptionTask.Result.IsFailure) {
                return Result.Failure<NotificationReturn>(disruptionTask.Result.Error);
            }

            if (severityTask.Result.IsFailure) {
                return Result.Failure<NotificationReturn>(severityTask.Result.Error);
            }

            if (descriptionTask.Result.IsFailure) {
                return Result.Failure<NotificationReturn>(descriptionTask.Result.Error);
            }

            if (lineTask.Result.IsFailure) {
                return Result.Failure<NotificationReturn>(lineTask.Result.Error);
            }

            if (journeyStartStationTask.Result.IsFailure || journeyStartStationTask.Result.IsFailure) {
                return Result.Failure<NotificationReturn>("Failed to fetch start or end station for the journey.");
            }

            if (affectedStationsTask.Result.IsFailure) {
                return Result.Failure<NotificationReturn>(affectedStationsTask.Result.Error);
            }

            var disruptionStartStationTask = _waterlooClient.GetStationsById([disruptionTask.Result.Value.StartStationId], cancellationToken);
            var disruptionEndStationTask = _waterlooClient.GetStationsById([disruptionTask.Result.Value.EndStationId], cancellationToken);

            await Task.WhenAll(disruptionStartStationTask, disruptionEndStationTask);

            if (disruptionStartStationTask.Result.IsFailure || disruptionEndStationTask.Result.IsFailure) {
                return Result.Failure<NotificationReturn>("Failed to fetch start or end station for disruption.");
            }

            var line = lineTask.Result.Value.First();
            var journeyStartStation = journeyStartStationTask.Result.Value.First();
            var journeyEndStation = journeyEndStationTask.Result.Value.First();
            var disruptionStartStation = disruptionStartStationTask.Result.Value.First();
            var disruptionEndStation = disruptionEndStationTask.Result.Value.First();
            var affectedStations = affectedStationsTask.Result.Value.ToList();
            var disruption = disruptionTask.Result.Value;

            var notificationReturn = new NotificationReturn(
               line,
               journeyStartStation,
               journeyEndStation,
               disruptionStartStation,
               disruptionEndStation,
               affectedStations,
               severityTask.Result.Value.Severity,
               notification.SentTime,
               disruption.StartTime,
               disruption.EndTime,
               descriptionTask.Result.Value.Description
           );

            return Result.Success(notificationReturn);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error building NotificationReturn for {notification.Id}");
            return Result.Failure<NotificationReturn>($"Error building NotificationReturn: {ex.Message}");
        }
    }
}
