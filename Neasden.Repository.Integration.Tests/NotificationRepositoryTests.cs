using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Database;
using Neasden.Models;
using Neasden.Repository.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Neasden.Repository.Integration.Tests;
public class NotificationRepositoryTests
{
    private readonly NeasdenDbContext _neasdenDbContext;
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private readonly NotificationRepository _notificationRepository;

    public NotificationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<NeasdenDbContext>()
            .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
            .Options;

        _neasdenDbContext = new NeasdenDbContext(options);

        _neasdenDbContext.Database.EnsureDeleted();
        _neasdenDbContext.Database.EnsureCreated();

        var logger = Substitute.For<ILogger<NotificationRepository>>();

        _notificationRepository = new NotificationRepository(_neasdenDbContext, logger);
    }

    [Fact]
    public async Task NotificationRepository_CreateNotificationAsync_Successful()
    {
        var notification = new Notification()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            SeverityId = Guid.NewGuid(),
            DescriptionId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            NotificationSentBy = NotificationSentBy.Push,
            SentTime = DateTime.UtcNow,
            AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        var result = await _notificationRepository.AddNotificationsAsync([notification]);

        result.IsSuccess.Should().BeTrue();

        var notificationSaved = await _neasdenDbContext.Notifications.SingleOrDefaultAsync(x => x.Id == notification.Id);

        notificationSaved.Should().NotBeNull();
        notificationSaved.Should().Be(notification);
    }

    [Fact]
    public async Task NotificationRepository_GetNotificationByIdAsync_Successful()
    {
        var notification = new Notification
        {
           Id = Guid.NewGuid(),
           UserId = Guid.NewGuid(),
           LineId = Guid.NewGuid(),
           DisruptionId = Guid.NewGuid(),
           SeverityId = Guid.NewGuid(),
           DescriptionId = Guid.NewGuid(),
           StartStationId = Guid.NewGuid(),
           EndStationId = Guid.NewGuid(),
           NotificationSentBy = NotificationSentBy.Sms,
           SentTime = DateTime.UtcNow,
           AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        await _neasdenDbContext.AddAsync(notification);
        await _neasdenDbContext.SaveChangesAsync();

        var result = await _notificationRepository.GetNotificationByIdAsync(notification.Id);
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().BeEquivalentTo(notification);
    }

    [Fact]
    public async Task NotificationRepository_GetDisruptionByIdAsync_No_Matching_Notification_Fails()
    {
        var id = Guid.NewGuid();

        var result = await _notificationRepository.GetNotificationByIdAsync(id);
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be($"Notification {id} does not exist on this database.");
    }

    [Fact]
    public async Task NotificationRepository_GetNotificationByUserIdAsync_Successful()
    {
        var notification1 = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            SeverityId = Guid.NewGuid(),
            DescriptionId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            NotificationSentBy = NotificationSentBy.Sms,
            SentTime = DateTime.UtcNow,
            AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        var notification2 = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            SeverityId = Guid.NewGuid(),
            DescriptionId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            NotificationSentBy = NotificationSentBy.Sms,
            SentTime = DateTime.UtcNow,
            AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        await _neasdenDbContext.Notifications.AddAsync(notification1);
        await _neasdenDbContext.Notifications.AddAsync(notification2);
        await _neasdenDbContext.SaveChangesAsync();

        var result = await _notificationRepository.GetNotificationsByUserId(notification1.UserId);
        result.IsSuccess.Should().BeTrue();

        result.Value.Count().Should().Be(1);
        result.Value.First().Should().BeEquivalentTo(notification1);
    }

    [Fact]
    public async Task NotificationRepository_GetNotificationByUserIdAsync_No_Matching_User_Fails()
    {
        var userId = Guid.NewGuid();

        var result = await _notificationRepository.GetNotificationsByUserId(userId);
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be($"Notifications for user {userId} do not exist on the database.");
    }
}
