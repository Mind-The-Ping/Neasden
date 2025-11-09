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

    public async Task<Result> NotifyDisruptionAsync(DisruptionDto disruption)
    {
        var notifiedUsers = await _userNotifiedRepository
            .GetUsersByDisruptionIdAsync(disruption.Id);

        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _londonTimeZone);

        var affectedUsers = await _waterlooClient.GetAffectedUsersAsync(
           disruption.Line.Id,
           disruption.StartStationId,
           disruption.EndStationId,
           disruption.Severity,
           TimeOnly.FromDateTime(DateTime.UtcNow),
           localTime.DayOfWeek);

        if (affectedUsers.IsFailure) {
            return Result.Failure($"Failed to get affected users : {affectedUsers.Error}");
        }

        var newUsers = affectedUsers.Value.ToList();
        var usersToNotify = new Dictionary<Guid, User>();

        foreach (var notifiedUser in notifiedUsers)
        {
            if (notifiedUser.Severity == disruption.Severity)
            {
                var newUser = newUsers.SingleOrDefault(x => x.Id == notifiedUser.Id);
                if (newUser is not null) {
                    newUsers.Remove(newUser);
                }
                continue;
            }

            usersToNotify[notifiedUser.Id] = notifiedUser;
        }

        var userDetails = await _stratfordClient.GetUserDetailsAsync(
            [.. newUsers.Select(x => x.Id)]);

        if (userDetails.IsFailure) {
            return Result.Failure($"Failed to get users details : {userDetails.Error}");
        }

        var phoneLookup = userDetails.Value?
         .ToDictionary(
             u => u.Id,
             u => new { u.PhoneNumber, u.PhoneOS })
         ?? [];

        var errors = new List<string>();

        foreach (var newUser in newUsers)
        {
            if (!phoneLookup.TryGetValue(newUser.Id, out var phoneDetails))
            {
                errors.Add($"Failed to find phone number for {newUser.Id}");
                continue;
            }

            var user = new User(
               newUser.Id,
               disruption.Id,
               disruption.Line,
               newUser.StartStation,
               newUser.EndStation,
               disruption.Severity,
               phoneDetails.PhoneNumber,
               phoneDetails.PhoneOS,
               newUser.EndTime,
               newUser.AffectedStations);

            usersToNotify[user.Id] = user;
        }

        var finalUsersToNotify = usersToNotify.Values.ToList();
        var notifications = NotificationsCreate(disruption, finalUsersToNotify);

        var notificationAdd = await _notificationRepository.AddNotificationsAsync(notifications);

        if(notificationAdd.IsFailure) 
        {
            errors.Add($"Failed to add notifications, Error: {notificationAdd.Error}");
            return Result.Failure(string.Join("; ", errors));
        }

        await _notificationPublisher.PublishAsync(notifications);

        return errors.Count != 0
            ? Result.Failure(string.Join("; ", errors))
            : Result.Success();
    }

    public async Task NotifyDisruptionEndAsync(DisruptionEnd disruptionEnd)
    {
        var notifiedUsers = await _userNotifiedRepository
            .GetUsersByDisruptionIdAsync(disruptionEnd.Id);

        await _notificationPublisher.PublishResolvedAsync(notifiedUsers);
        await _userNotifiedRepository.DeleteByDisruptionIdAsync(disruptionEnd.Id);
    }

    private static List<Notification> NotificationsCreate(
        DisruptionDto disruptionDto,
        IEnumerable<User> users)
    {
        var notifications = new List<Notification>();

        foreach (var user in users)
        {
            var notification = new Notification()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                LineId = disruptionDto.Line.Id,
                DisruptionId = disruptionDto.Id,
                StartStationId = user.StartStation.Id,
                EndStationId = user.EndStation.Id,
                SeverityId = disruptionDto.SeverityId,
                DescriptionId = disruptionDto.DescriptionId,
                SentTime = DateTime.UtcNow,
                AffectedStationIds = [.. user.AffectedStations.Select(x => x.Id)],
            };

            notifications.Add(notification);
        }

        return notifications;
    }
}
