using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;
using Neasden.Repository.Read;
using Neasden.Repository.Write;
using NSubstitute;

namespace Neasden.Repository.Integration.Tests.Read;
public class ReadNotificationRepositoryTests
{
    private readonly WriteDbContext _writeContext;
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private ReadNotificationRepository _repository;

    public ReadNotificationRepositoryTests()
    {
        var readOptions = new DbContextOptionsBuilder<ReadDbContext>()
            .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
            .Options;

        var _contextFactory = new TestDbContextFactory(readOptions);

        using (var context = _contextFactory.CreateDbContext())
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        var logger = Substitute.For<ILogger<ReadNotificationRepository>>();

        _repository = new ReadNotificationRepository(_contextFactory, logger);

        var writeOptions = new DbContextOptionsBuilder<WriteDbContext>()
           .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
           .Options;

        _writeContext = new WriteDbContext(writeOptions);
    }

    [Fact]
    public async Task ReadNotificationRepository_GetNotificationByIdAsync_Successful()
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

        await _writeContext.AddAsync(notification);
        await _writeContext.SaveChangesAsync();

        var result = await _repository.GetNotificationByIdAsync(notification.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(notification.Id);
        result.Value.UserId.Should().Be(notification.UserId);
        result.Value.LineId.Should().Be(notification.LineId);
        result.Value.DisruptionId.Should().Be(notification.DisruptionId);
        result.Value.SeverityId.Should().Be(notification.SeverityId);
        result.Value.DescriptionId.Should().Be(notification.DescriptionId);
        result.Value.StartStationId.Should().Be(notification.StartStationId);
        result.Value.EndStationId.Should().Be(notification.EndStationId);
        result.Value.NotificationSentBy.Should().Be(notification.NotificationSentBy);
        result.Value.SentTime.Should().BeCloseTo(notification.SentTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ReadNotificationRepository_GetDisruptionByIdAsync_No_Matching_Notification_Fails()
    {
        var id = Guid.NewGuid();

        var result = await _repository.GetNotificationByIdAsync(id);
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be($"Notification {id} does not exist on this database.");
    }

    [Fact]
    public async Task ReadNotificationRepository_GetNotificationByUserIdAsync_Defaults_Successful()
    {
        var userId = Guid.NewGuid();
        var notifications = GenerateNotifications(userId, 20);

        await _writeContext.Notifications.AddRangeAsync(notifications);
        await _writeContext.SaveChangesAsync();

        var result = await _repository.GetNotificationIdsByUserIdAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalPages.Should().Be(1);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.TotalCount.Should().Be(20);
        result.Value.Items.Should().BeEquivalentTo(notifications, options =>
            options.Excluding(n => n.SentTime));
    }

    [Fact]
    public async Task ReadNotificationRepository_GetNotificationByUserIdAsync_Middle_Page()
    {
        var userId = Guid.NewGuid();
        var notifications = GenerateNotifications(userId, 100);

        await _writeContext.Notifications.AddRangeAsync(notifications);
        await _writeContext.SaveChangesAsync();

        var result = await _repository.GetNotificationIdsByUserIdAsync(userId, 3, 25);

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

        result.Value.Items.Should().BeEquivalentTo(expected, options =>
           options.Excluding(n => n.SentTime));
    }

    [Fact]
    public async Task ReadNotificationRepository_GetNotificationByUserIdAsync_No_Notifications_Successful()
    {
        var userId = Guid.NewGuid();
        var notifications = GenerateNotifications(userId, 20);

        var result = await _repository.GetNotificationIdsByUserIdAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalPages.Should().Be(0);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ReadNotificationRepository_GetNotificationIdsByUserIdLatestAsync_Successful()
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

        await _writeContext.AddAsync(notification);
        await _writeContext.SaveChangesAsync();

        var hourEarlier = DateTime.UtcNow.AddHours(-1);
        var result = await _repository.GetNotificationIdsByUserIdLatestAsync(notification.UserId, hourEarlier);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.First().Should().BeEquivalentTo(notification, options =>
         options.Excluding(n => n.SentTime));
    }

    [Fact]
    public async Task ReadNotificationRepository_GetNotificationIdsByUserIdLatestAsync_GetCorrectNotifcation_Successful()
    {
        var userId = Guid.NewGuid();

        var notification1 = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LineId = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            SeverityId = Guid.NewGuid(),
            DescriptionId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            NotificationSentBy = NotificationSentBy.Sms,
            SentTime = DateTime.UtcNow.AddHours(-2),
            AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        var notification2 = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LineId = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            SeverityId = Guid.NewGuid(),
            DescriptionId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            NotificationSentBy = NotificationSentBy.Sms,
            SentTime = DateTime.UtcNow.AddHours(1),
            AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        await _writeContext.AddRangeAsync([notification1, notification2]);
        await _writeContext.SaveChangesAsync();

        var hourEarlier = DateTime.UtcNow.AddHours(-1);

        var result = await _repository.GetNotificationIdsByUserIdLatestAsync(userId, hourEarlier);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.First().Should().BeEquivalentTo(notification2, options =>
          options.Excluding(n => n.SentTime));
    }

    [Fact]
    public async Task NotificationRepository_GetNotificationIdsByUserIdLatestAsync_No_Notifications_Successful()
    {
        var userId = Guid.NewGuid();
        var notifications = GenerateNotifications(userId, 20);

        var result = await _repository.GetNotificationIdsByUserIdLatestAsync(userId, DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.TotalPages.Should().Be(0);
        result.Value.TotalCount.Should().Be(0);
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
