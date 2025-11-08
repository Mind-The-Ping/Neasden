using CSharpFunctionalExtensions;
using Neasden.Consumer.Clients.StratfordClient;
using Neasden.Consumer.Dto;
using Neasden.Consumer.Repositories;
using Neasden.Library.Clients;
using Neasden.Models;

namespace Neasden.Consumer;
public class DisruptionNotifer
{
    private readonly TimeZoneInfo _londonTimeZone;
    private readonly IWaterlooClient _waterlooClient;
    private readonly IStratfordClient _stratfordClient;
    private readonly IUserNotifiedRepository _userNotifiedRepository;

    public DisruptionNotifer(
        IWaterlooClient waterlooClient,
        IStratfordClient stratfordClient,
        IUserNotifiedRepository userNotifiedRepository)
    {
        _waterlooClient = waterlooClient ?? 
            throw new ArgumentNullException(nameof(waterlooClient));

        _stratfordClient = stratfordClient ?? 
            throw new ArgumentNullException(nameof(stratfordClient));

        _userNotifiedRepository = userNotifiedRepository ??
            throw new ArgumentNullException(nameof(userNotifiedRepository));

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

        return errors.Count != 0
            ? Result.Failure(string.Join("; ", errors))
            : Result.Success();
    }
}
