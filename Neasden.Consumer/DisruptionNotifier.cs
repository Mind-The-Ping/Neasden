using CSharpFunctionalExtensions;
using Neasden.Consumer.Clients.StratfordClient;
using Neasden.Consumer.Dto;
using Neasden.Consumer.Repositories;
using Neasden.Library.Clients;
using Neasden.Models;
using Neasden.Repository.Write;

namespace Neasden.Consumer;

public class DisruptionNotifier
{
    private readonly TimeZoneInfo _londonTimeZone;
    private readonly IWaterlooClient _waterlooClient;
    private readonly IStratfordClient _stratfordClient;
    private readonly IUserNotifiedRepository _userNotifiedRepository;
    private readonly IWriteNotificationRepository _notificationRepository;
    private readonly INotificationPublisher _notificationPublisher;

    public DisruptionNotifier(
        IWaterlooClient waterlooClient,
        IStratfordClient stratfordClient,
        IUserNotifiedRepository userNotifiedRepository,
        IWriteNotificationRepository notificationRepository,
        INotificationPublisher notificationPublisher)
    {
        _waterlooClient = waterlooClient ??
            throw new ArgumentNullException(nameof(waterlooClient));

        _stratfordClient = stratfordClient ??
            throw new ArgumentNullException(nameof(stratfordClient));

        _userNotifiedRepository = userNotifiedRepository ??
            throw new ArgumentNullException(nameof(userNotifiedRepository));

        _notificationRepository = notificationRepository ??
            throw new ArgumentNullException(nameof(notificationRepository));

        _notificationPublisher = notificationPublisher ??
            throw new ArgumentNullException(nameof(notificationPublisher));

        _londonTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
    }

    public async Task<Result> NotifyDisruptionAsync(LineDisruptionsDto lineDisruptions)
    {
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _londonTimeZone);

        var affectedJourneysResult = await _waterlooClient.GetAffectedUsersAsync(new AffectedJourney(
            lineDisruptions.Line.Id,
            TimeOnly.FromDateTime(DateTime.UtcNow),
            localTime.DayOfWeek,
            lineDisruptions.DisruptionDtos.Select(x =>
                new AffectedDisruption(
                    x.Id,
                    x.StartStationId,
                    x.EndStationId,
                    x.Severity
                ))));

        if (affectedJourneysResult.IsFailure) {
            return Result.Failure($"Failed to get affected users : {affectedJourneysResult.Error}");
        }

        var affectedJourneysByDisruption =
         affectedJourneysResult.Value
             .GroupBy(x => x.DisruptionId)
             .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var disruption in lineDisruptions.DisruptionDtos)
        {
            affectedJourneysByDisruption
                .TryGetValue(disruption.Id,out var affectedJourneysForDisruption);

            var result = await NotifySingularDisruptionAsync(disruption, affectedJourneysForDisruption ?? []);

            if (result.IsFailure) {
                return result;
            }
        }

        return Result.Success();
    }

    public async Task NotifyDisruptionEndAsync(DisruptionEnd disruptionEnd)
    {
        var notifiedEntries = await _userNotifiedRepository
            .GetJourneysByDisruptionIdAsync(disruptionEnd.Id);

        await _notificationPublisher.PublishResolvedAsync(notifiedEntries);
        await _userNotifiedRepository.DeleteByDisruptionIdAsync(disruptionEnd.Id);
    }

    private async Task<Result> NotifySingularDisruptionAsync(
        DisruptionDto disruption,
        List<AffectedUser> affectedJourneys)
    {
        var allNotifiedEntries = await _userNotifiedRepository
            .GetJourneysByDisruptionIdAsync(disruption.Id);

        var affectedJourneyIds = affectedJourneys
            .Select(x => x.JourneyId)
            .ToHashSet();

        var notifiedEntriesToRemove = allNotifiedEntries
            .Where(x => !affectedJourneyIds.Contains(x.JourneyId))
            .ToList();

        if (notifiedEntriesToRemove.Count > 0) {
            await _userNotifiedRepository.DeleteJourneysAsync(notifiedEntriesToRemove);
        }

        var existingNotifiedEntriesStillAffected = allNotifiedEntries
            .Where(x => affectedJourneyIds.Contains(x.JourneyId))
            .ToList();

        var newlyAffectedJourneys = affectedJourneys.ToList();
        var entriesToNotifyByJourneyId = new Dictionary<Guid, Journey>();

        foreach (var existingNotifiedEntry in existingNotifiedEntriesStillAffected)
        {
            if (existingNotifiedEntry.Severity == disruption.Severity)
            {
                var matchingAffectedJourney = newlyAffectedJourneys
                    .SingleOrDefault(x => x.JourneyId == existingNotifiedEntry.JourneyId);

                if (matchingAffectedJourney is not null) {
                    newlyAffectedJourneys.Remove(matchingAffectedJourney);
                }

                continue;
            }

            entriesToNotifyByJourneyId[existingNotifiedEntry.JourneyId] = existingNotifiedEntry;
        }

        var userDetailsResult = await _stratfordClient.GetUserDetailsAsync(
            [.. newlyAffectedJourneys.Select(x => x.UserId)]);

        if (userDetailsResult.IsFailure) {
            return Result.Failure($"Failed to get users details : {userDetailsResult.Error}");
        }

        var phoneLookup = userDetailsResult.Value?
            .ToDictionary(
                u => u.Id,
                u => new { u.PhoneNumber, u.PhoneOS })
            ?? [];

        var errors = new List<string>();

        foreach (var newlyAffectedJourney in newlyAffectedJourneys)
        {
            if (!phoneLookup.TryGetValue(newlyAffectedJourney.UserId, out var phoneDetails))
            {
                errors.Add($"Failed to find phone number for {newlyAffectedJourney.JourneyId}");
                continue;
            }

            var notifiedEntry = new Journey(
                newlyAffectedJourney.JourneyId,
                newlyAffectedJourney.UserId,
                disruption.Id,
                disruption.Line,
                newlyAffectedJourney.StartStation,
                newlyAffectedJourney.EndStation,
                disruption.Severity,
                phoneDetails.PhoneNumber,
                phoneDetails.PhoneOS,
                newlyAffectedJourney.EndTime,
                newlyAffectedJourney.AffectedStations);

            entriesToNotifyByJourneyId[notifiedEntry.JourneyId] = notifiedEntry;
        }

        var finalEntriesToNotify = entriesToNotifyByJourneyId.Values.ToList();
        var notifications = NotificationsCreate(disruption, finalEntriesToNotify);

        var notificationAdd = await _notificationRepository.AddNotificationsAsync(notifications);

        if (notificationAdd.IsFailure)
        {
            errors.Add($"Failed to add notifications, Error: {notificationAdd.Error}");
            return Result.Failure(string.Join("; ", errors));
        }

        await _notificationPublisher.PublishAsync(finalEntriesToNotify);
        await _userNotifiedRepository.SaveJourneysAsync(finalEntriesToNotify);

        return errors.Count != 0
            ? Result.Failure(string.Join("; ", errors))
            : Result.Success();
    }

    private static List<Notification> NotificationsCreate(
        DisruptionDto disruptionDto,
        IEnumerable<Journey> notifiedEntries)
    {
        var notifications = new List<Notification>();

        foreach (var notifiedEntry in notifiedEntries)
        {
            var notification = new Notification()
            {
                Id = Guid.NewGuid(),
                UserId = notifiedEntry.UserId,
                LineId = disruptionDto.Line.Id,
                DisruptionId = disruptionDto.Id,
                StartStationId = notifiedEntry.StartStation.Id,
                EndStationId = notifiedEntry.EndStation.Id,
                SeverityId = disruptionDto.SeverityId,
                DescriptionId = disruptionDto.DescriptionId,
                SentTime = DateTime.UtcNow,
                AffectedStationIds = [.. notifiedEntry.AffectedStations.Select(x => x.Id)],
            };

            notifiedEntry.NotificationId = notification.Id;
            notifications.Add(notification);
        }

        return notifications;
    }
}
