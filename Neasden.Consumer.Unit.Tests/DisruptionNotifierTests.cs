using CSharpFunctionalExtensions;
using FluentAssertions;
using Neasden.Consumer.Clients.StratfordClient;
using Neasden.Consumer.Dto;
using Neasden.Consumer.Models;
using Neasden.Consumer.Repositories;
using Neasden.Library.Clients;
using Neasden.Models;
using Neasden.Repository.Write;
using NSubstitute;

namespace Neasden.Consumer.Unit.Tests;
public class DisruptionNotifierTests
{
    private readonly DisruptionNotifier _notifier;

    private readonly IWaterlooClient _waterlooClient;
    private readonly IStratfordClient _stratfordClient;
    private readonly IUserNotifiedRepository _userNotifiedRepository;
    private readonly IWriteNotificationRepository _writeNotificationRepository;
    private readonly INotificationPublisher _notificationPublisher;

    private readonly Line _line;
    private readonly Station _startStation;
    private readonly Station _endStation;
    private readonly TimeOnly _endTime;
    private readonly IEnumerable<Station> _affectedStations;

    public DisruptionNotifierTests()
    {
        _line = new Line(Guid.Parse("8c3a4d59-f2e0-46a8-9f56-ec27eaffded9"), "District");
        _startStation = new Station(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End");
        _endStation = new Station(Guid.Parse("968bc258-138c-45cf-83c0-599705285d25"), "West Ham");
        _endTime = TimeOnly.FromDateTime(DateTime.UtcNow.AddMinutes(30));
        _affectedStations = [
            new Station(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End"),
            new Station(Guid.Parse("3db408d6-248a-4ef7-8486-203e87cc408a"), "Bow Road"),
            new Station(Guid.Parse("a391396c-6921-4202-ace2-2d5033bfac1f"), "Bromley By Bow"),
            new Station(Guid.Parse("968bc258-138c-45cf-83c0-599705285d25"), "West Ham"),
            ];

        _waterlooClient =  Substitute.For<IWaterlooClient>();
        _stratfordClient = Substitute.For<IStratfordClient>();
        _userNotifiedRepository =  Substitute.For<IUserNotifiedRepository>();
        _writeNotificationRepository = Substitute.For<IWriteNotificationRepository>();
        _notificationPublisher = Substitute.For<INotificationPublisher>();

        _notifier = new DisruptionNotifier(
            _waterlooClient,
            _stratfordClient,
            _userNotifiedRepository,
            _writeNotificationRepository,
            _notificationPublisher);
    }

    [Fact]
    public async Task DisruptionNotifier_NotifyDisruptionAsync_New_Users_Disruption_Notifys_New_Users_Only()
    {
        _userNotifiedRepository.GetUsersByDisruptionIdAsync(Arg.Any<Guid>())
          .Returns(Enumerable.Empty<User>());

        _userNotifiedRepository.SaveUsersAsync(Arg.Any<IEnumerable<User>>())
            .Returns(Result.Success());

        _userNotifiedRepository.DeleteByDisruptionIdAsync(Arg.Any<Guid>())
            .Returns(Task.CompletedTask);

        var affectedUsers = new List<AffectedUser>
        {
            new(Guid.NewGuid(), _startStation, _endStation, _affectedStations, _endTime),
            new(Guid.NewGuid(), _startStation, _endStation, _affectedStations, _endTime)
        };

        _waterlooClient.GetAffectedUsersAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Severity>(),
            Arg.Any<TimeOnly>(),
            Arg.Any<DayOfWeek>())
            .Returns(Result.Success<IEnumerable<AffectedUser>>(affectedUsers));

        var userDetails = new List<UserDetails>
        {
            new(affectedUsers.First().Id, "+447123456789", PhoneOS.Android),
            new(affectedUsers.Last().Id, "+447234567890", PhoneOS.IOS)
        };

        _stratfordClient.GetUserDetailsAsync(Arg.Any<IEnumerable<Guid>>())
            .Returns(Result.Success<IEnumerable<UserDetails>>(userDetails));

        var disruption = new DisruptionDto(
           Guid.NewGuid(),
           _line,
           _startStation.Id,
           _endStation.Id,
           Severity.Closed,
           Guid.NewGuid(),
           Guid.NewGuid(),
           DateTime.UtcNow);

        _writeNotificationRepository.AddNotificationsAsync(Arg.Any<IEnumerable<Notification>>())
            .Returns(Result.Success());

        IEnumerable<User> capturedUsers = null!;

        _notificationPublisher.PublishAsync(Arg.Do<IEnumerable<User>>(c => capturedUsers = c))
            .Returns(Task.CompletedTask);

        await _notifier.NotifyDisruptionAsync(disruption);

        capturedUsers.Count().Should().Be(2);

        capturedUsers.First().Id.Should().Be(affectedUsers.First().Id);
        capturedUsers.First().DisruptionId.Should().Be(disruption.Id);
        capturedUsers.First().Line.Should().Be(disruption.Line);
        capturedUsers.First().StartStation.Should().Be(affectedUsers.First().StartStation);
        capturedUsers.First().EndStation.Should().Be(affectedUsers.First().EndStation);
        capturedUsers.First().Severity.Should().Be(disruption.Severity);
        capturedUsers.First().PhoneNumber.Should().Be(userDetails.First().PhoneNumber);
        capturedUsers.First().PhoneOS.Should().Be(userDetails.First().PhoneOS);
        capturedUsers.First().EndTime.Should().Be(affectedUsers.First().EndTime);
        capturedUsers.First().AffectedStations.Should().BeEquivalentTo(affectedUsers.First().AffectedStations);

        capturedUsers.Last().Id.Should().Be(affectedUsers.Last().Id);
        capturedUsers.Last().DisruptionId.Should().Be(disruption.Id);
        capturedUsers.Last().Line.Should().Be(disruption.Line);
        capturedUsers.Last().StartStation.Should().Be(affectedUsers.Last().StartStation);
        capturedUsers.Last().EndStation.Should().Be(affectedUsers.Last().EndStation);
        capturedUsers.Last().Severity.Should().Be(disruption.Severity);
        capturedUsers.Last().PhoneNumber.Should().Be(userDetails.Last().PhoneNumber);
        capturedUsers.Last().PhoneOS.Should().Be(userDetails.Last().PhoneOS);
        capturedUsers.Last().EndTime.Should().Be(affectedUsers.Last().EndTime);
        capturedUsers.Last().AffectedStations.Should().BeEquivalentTo(affectedUsers.Last().AffectedStations);
    }

    [Fact]
    public async Task DisruptionNotifier_NotifyDisruptionAsync_NewUsers_NotifiedUsers_SameSeverity_Disruption_Notifys_New_Users_Only()
    {
        var severity = Severity.Severe;

        var disruption = new DisruptionDto(
            Guid.NewGuid(),
            _line,
            _startStation.Id,
            _endStation.Id,
            severity,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        var users = new List<User>
        {
            new(
                Guid.NewGuid(),
                disruption.Id,
                _line,
                _startStation,
                _endStation,
                severity,
                "+447123456789",
                PhoneOS.Android,
                _endTime,
                _affectedStations),
            new(
                Guid.NewGuid(),
                disruption.Id,
                _line,
                _startStation,
                _endStation,
                severity,
                 "+447234567890",
                 PhoneOS.Android,
                _endTime,
                _affectedStations),
        };

        _userNotifiedRepository.GetUsersByDisruptionIdAsync(Arg.Any<Guid>())
            .Returns(users);

        _userNotifiedRepository.SaveUsersAsync(Arg.Any<IEnumerable<User>>())
           .Returns(Result.Success());

        _userNotifiedRepository.DeleteByDisruptionIdAsync(Arg.Any<Guid>())
           .Returns(Task.CompletedTask);

        var affectedUsers = new List<AffectedUser>
        {
            new(Guid.NewGuid(), _startStation, _endStation, _affectedStations, _endTime),
            new(Guid.NewGuid(), _startStation, _endStation, _affectedStations, _endTime)
        };

        _waterlooClient.GetAffectedUsersAsync(
             Arg.Any<Guid>(),
             Arg.Any<Guid>(),
             Arg.Any<Guid>(),
             Arg.Any<Severity>(),
             Arg.Any<TimeOnly>(),
             Arg.Any<DayOfWeek>())
             .Returns(Result.Success<IEnumerable<AffectedUser>>(affectedUsers));

        var userDetails = new List<UserDetails>
        {
            new(affectedUsers.First().Id, "+447345678901", PhoneOS.Android),
            new(affectedUsers.Last().Id, "+447456789012", PhoneOS.IOS)
        };

        _stratfordClient.GetUserDetailsAsync(Arg.Any<IEnumerable<Guid>>())
           .Returns(Result.Success<IEnumerable<UserDetails>>(userDetails));


        _writeNotificationRepository.AddNotificationsAsync(Arg.Any<IEnumerable<Notification>>())
           .Returns(Result.Success());

        IEnumerable<User> capturedUsers = null!;

        _notificationPublisher.PublishAsync(Arg.Do<IEnumerable<User>>(c => capturedUsers = c))
            .Returns(Task.CompletedTask);

        await _notifier.NotifyDisruptionAsync(disruption);

        capturedUsers.Count().Should().Be(2);

        capturedUsers.First().Id.Should().Be(affectedUsers.First().Id);
        capturedUsers.First().DisruptionId.Should().Be(disruption.Id);
        capturedUsers.First().Line.Should().Be(disruption.Line);
        capturedUsers.First().StartStation.Should().Be(affectedUsers.First().StartStation);
        capturedUsers.First().EndStation.Should().Be(affectedUsers.First().EndStation);
        capturedUsers.First().Severity.Should().Be(disruption.Severity);
        capturedUsers.First().PhoneNumber.Should().Be(userDetails.First().PhoneNumber);
        capturedUsers.First().PhoneOS.Should().Be(userDetails.First().PhoneOS);
        capturedUsers.First().EndTime.Should().Be(affectedUsers.First().EndTime);
        capturedUsers.First().AffectedStations.Should().BeEquivalentTo(affectedUsers.First().AffectedStations);

        capturedUsers.Last().Id.Should().Be(affectedUsers.Last().Id);
        capturedUsers.Last().DisruptionId.Should().Be(disruption.Id);
        capturedUsers.Last().Line.Should().Be(disruption.Line);
        capturedUsers.Last().StartStation.Should().Be(affectedUsers.Last().StartStation);
        capturedUsers.Last().EndStation.Should().Be(affectedUsers.Last().EndStation);
        capturedUsers.Last().Severity.Should().Be(disruption.Severity);
        capturedUsers.Last().PhoneNumber.Should().Be(userDetails.Last().PhoneNumber);
        capturedUsers.Last().PhoneOS.Should().Be(userDetails.Last().PhoneOS);
        capturedUsers.Last().EndTime.Should().Be(affectedUsers.Last().EndTime);
        capturedUsers.Last().AffectedStations.Should().BeEquivalentTo(affectedUsers.Last().AffectedStations);
    }

    [Fact]
    public async Task DisruptionNotifier_NotifyDisruptionAsync_NewUsers_NotifiedUsers_DifferentSeverity_Disruption_Notifys_All()
    {
        var disruption = new DisruptionDto(
           Guid.NewGuid(),
           _line,
           _startStation.Id,
           _endStation.Id,
           Severity.Minor,
           Guid.NewGuid(),
           Guid.NewGuid(),
           DateTime.UtcNow);

        var users = new List<User>
        {
            new(
                Guid.NewGuid(),
                disruption.Id,
                _line,
                _startStation,
                _endStation,
                 Severity.Severe,
                "+447123456789",
                PhoneOS.Android,
                _endTime,
                _affectedStations),
            new(
                Guid.NewGuid(),
                disruption.Id,
                _line,
                _startStation,
                _endStation,
                 Severity.Severe,
                 "+447234567890",
                 PhoneOS.Android,
                _endTime,
                _affectedStations),
        };

        _userNotifiedRepository.GetUsersByDisruptionIdAsync(Arg.Any<Guid>())
           .Returns(users);

        _userNotifiedRepository.SaveUsersAsync(Arg.Any<IEnumerable<User>>())
          .Returns(Result.Success());

        _userNotifiedRepository.DeleteByDisruptionIdAsync(Arg.Any<Guid>())
           .Returns(Task.CompletedTask);

        var affectedUsers = new List<AffectedUser>
        {
            new(Guid.NewGuid(), _startStation, _endStation, _affectedStations, _endTime),
            new(Guid.NewGuid(), _startStation, _endStation, _affectedStations, _endTime)
        };

        _waterlooClient.GetAffectedUsersAsync(
           Arg.Any<Guid>(),
           Arg.Any<Guid>(),
           Arg.Any<Guid>(),
           Arg.Any<Severity>(),
           Arg.Any<TimeOnly>(),
           Arg.Any<DayOfWeek>())
           .Returns(Result.Success<IEnumerable<AffectedUser>>(affectedUsers));

        var userDetails = new List<UserDetails>
        {
            new(affectedUsers.First().Id, "+447345678901", PhoneOS.Android),
            new(affectedUsers.Last().Id, "+447456789012", PhoneOS.IOS)
        };

        _stratfordClient.GetUserDetailsAsync(Arg.Any<IEnumerable<Guid>>())
          .Returns(Result.Success<IEnumerable<UserDetails>>(userDetails));

        IEnumerable<User> capturedUser = null!;

        _notificationPublisher.PublishAsync(Arg.Do<IEnumerable<User>>(c => capturedUser = c))
            .Returns(Task.CompletedTask);

        await _notifier.NotifyDisruptionAsync(disruption);

        capturedUser.Count().Should().Be(2);
    }

    [Fact]
    public async Task DisruptionNotifier_NotifyDisruptionAsync_NewUser_NotifiedUser_Different_Severity_Disruption_Notify_Sends_New()
    {
        var disruption = new DisruptionDto(
            Guid.NewGuid(),
            _line,
            _startStation.Id,
            _endStation.Id,
            Severity.Minor,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        var users = new List<User>
        {
            new(
                Guid.NewGuid(),
                disruption.Id,
                _line,
                _startStation,
                _endStation,
                Severity.Severe,
                "+447123456789",
                PhoneOS.Android,
                _endTime,
                _affectedStations)
        };

        _userNotifiedRepository.GetUsersByDisruptionIdAsync(Arg.Any<Guid>())
          .Returns(users);

        _userNotifiedRepository.SaveUsersAsync(Arg.Any<IEnumerable<User>>())
         .Returns(Result.Success());

        _userNotifiedRepository.DeleteByDisruptionIdAsync(Arg.Any<Guid>())
           .Returns(Task.CompletedTask);

        var affectedUsers = new List<AffectedUser>
        {
            new(users.First().Id, _startStation, _endStation, _affectedStations, _endTime),
        };

        _waterlooClient.GetAffectedUsersAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Severity>(),
            Arg.Any<TimeOnly>(),
            Arg.Any<DayOfWeek>())
            .Returns(Result.Success<IEnumerable<AffectedUser>>(affectedUsers));

        var userDetails = new List<UserDetails>
        {
            new(affectedUsers.First().Id, "+447345678901", PhoneOS.Android),
        };

        _stratfordClient.GetUserDetailsAsync(Arg.Any<IEnumerable<Guid>>())
          .Returns(Result.Success<IEnumerable<UserDetails>>(userDetails));

        IEnumerable<User> capturedUsers = null!;

        _notificationPublisher.PublishAsync(Arg.Do<IEnumerable<User>>(c => capturedUsers = c))
            .Returns(Task.CompletedTask);

        await _notifier.NotifyDisruptionAsync(disruption);

        capturedUsers.Count().Should().Be(1);
        capturedUsers.First().Id.Should().Be(affectedUsers.First().Id);
    }

    [Fact]
    public async Task DisruptionNotifier_NotifyDisruptionAsync_Filters_NotifiedUsers_Correctly()
    {
        var disruption = new DisruptionDto(
            Guid.NewGuid(),
            _line,
            _startStation.Id,
            _endStation.Id,
            Severity.Severe,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        var users = new List<User>
        {
            new(
                Guid.NewGuid(),
                disruption.Id,
                _line,
                _startStation,
                _endStation,
                Severity.Minor,
                "+447123456789",
                PhoneOS.Android,
                _endTime,
                _affectedStations),
            new(
                Guid.NewGuid(),
                disruption.Id,
                _line,
                _startStation,
                _endStation,
                 Severity.Minor,
                 "+447234567890",
                 PhoneOS.Android,
                _endTime,
                _affectedStations),
        };

        _userNotifiedRepository.GetUsersByDisruptionIdAsync(Arg.Any<Guid>())
            .Returns(users);

        _userNotifiedRepository.SaveUsersAsync(Arg.Any<IEnumerable<User>>())
           .Returns(Result.Success());

        _userNotifiedRepository.DeleteByDisruptionIdAsync(Arg.Any<Guid>())
           .Returns(Task.CompletedTask);

        var affectedUsers = new List<AffectedUser>
        {
            new(users.First().Id, _startStation, _endStation, _affectedStations, _endTime),
            new(Guid.NewGuid(), _startStation, _endStation, _affectedStations, _endTime)
        };

        _waterlooClient.GetAffectedUsersAsync(
             Arg.Any<Guid>(),
             Arg.Any<Guid>(),
             Arg.Any<Guid>(),
             Arg.Any<Severity>(),
             Arg.Any<TimeOnly>(),
             Arg.Any<DayOfWeek>())
             .Returns(Result.Success<IEnumerable<AffectedUser>>(affectedUsers));

        var userDetails = new List<UserDetails>
        {
            new(affectedUsers.First().Id, "+447345678901", PhoneOS.Android),
            new(affectedUsers.Last().Id, "+447456789012", PhoneOS.IOS)
        };

        _stratfordClient.GetUserDetailsAsync(Arg.Any<IEnumerable<Guid>>())
           .Returns(Result.Success<IEnumerable<UserDetails>>(userDetails));


        _writeNotificationRepository.AddNotificationsAsync(Arg.Any<IEnumerable<Notification>>())
           .Returns(Result.Success());

        IEnumerable<User> capturedUsers = null!;

        _notificationPublisher.PublishAsync(Arg.Do<IEnumerable<User>>(c => capturedUsers = c))
            .Returns(Task.CompletedTask);

        await _notifier.NotifyDisruptionAsync(disruption);

        capturedUsers.Count().Should().Be(2);

        capturedUsers.ElementAt(0).Id.Should().Be(users.First().Id);
        capturedUsers.ElementAt(1).Id.Should().Be(affectedUsers.Last().Id);
    }
}
