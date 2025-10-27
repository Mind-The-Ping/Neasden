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
    public async Task NotificationRepository_GetNotificationByUserIdAsync_Defaults_Successful()
    {
        var userId = Guid.NewGuid();
        var notifications = GenerateNotifications(userId, 20);

        await _neasdenDbContext.Notifications.AddRangeAsync(notifications);
        await _neasdenDbContext.SaveChangesAsync();

        var result = await _notificationRepository.GetNotificationIdsByUserId(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalPages.Should().Be(1);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.TotalCount.Should().Be(20);
        result.Value.Items.Should().BeEquivalentTo(notifications);
    }

    [Fact]
    public async Task NotificationRepository_GetNotificationByUserIdAsync_Middle_Page()
    {
        var userId = Guid.NewGuid();
        var notifications = GenerateNotifications(userId, 100);

        await _neasdenDbContext.Notifications.AddRangeAsync(notifications);
        await _neasdenDbContext.SaveChangesAsync();

        var result = await _notificationRepository.GetNotificationIdsByUserId(userId, 3, 25);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(3);
        result.Value.PageSize.Should().Be(25);
        result.Value.TotalPages.Should().Be(4);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.TotalCount.Should().Be(100);

        var expected = notifications
            .OrderByDescending(x => x.SentTime)
            .Skip((result.Value.Page - 1) * result.Value.PageSize)
            .Take(result.Value.PageSize)
            .ToList();

        result.Value.Items.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task NotificationRepository_GetNotificationByUserIdAsync_No_Notifications_Fails()
    {
        var userId = Guid.NewGuid();
        var notifications = GenerateNotifications(userId, 20);

        var result = await _notificationRepository.GetNotificationIdsByUserId(userId, 3, 25);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be($"Notification ids for user {userId} do not exist on the database.");
    }

    private List<Notification> GenerateNotifications(Guid userId, int number)
    {
        var notifications = new List<Notification>(number);
        var random = new Random();

        for (int i = 0; i < number; i++)
        {
            var notification = new Notification()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LineId = Guid.NewGuid(),
                DisruptionId = Guid.NewGuid(),
                StartStationId = Guid.NewGuid(),
                EndStationId = Guid.NewGuid(),
                SeverityId = Guid.NewGuid(),
                DescriptionId = Guid.NewGuid(),
                NotificationSentBy = (NotificationSentBy)random.Next(Enum.GetValues(typeof(NotificationSentBy)).Length),
                SentTime = DateTime.UtcNow.AddMinutes(-random.Next(0, 60 * 24 * 30)),
                AffectedStationIds = [.. Enumerable.Range(0, random.Next(1, 5)).Select(_ => Guid.NewGuid())]
            };

            notifications.Add(notification);
        }

        return notifications;
    }
}
