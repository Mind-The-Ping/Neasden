using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;
using Neasden.Repository.Write;
using NSubstitute;

namespace Neasden.Repository.Integration.Tests.Write;
public class WriteNotificationRepositoryTests
{
    private readonly WriteDbContext _context;
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private readonly WriteNotificationRepository _repository;

    public WriteNotificationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WriteDbContext>()
            .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
            .Options;

        _context = new WriteDbContext(options);

        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        var logger = Substitute.For<ILogger<WriteNotificationRepository>>();

        _repository = new WriteNotificationRepository(_context, logger);
    }

    [Fact]
    public async Task WriteNotificationRepository_CreateNotificationAsync_Successful()
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
            SentTime = DateTime.UtcNow,
            AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        var result = await _repository.AddNotificationsAsync([notification]);

        result.IsSuccess.Should().BeTrue();

        var notificationSaved = await _context.Notifications.SingleOrDefaultAsync(x => x.Id == notification.Id);

        notificationSaved.Should().NotBeNull();
        notificationSaved.Should().Be(notification);
    }

    [Fact]
    public async Task WriteNotificationRepository_RemoveNotificationsByUserIdAsync_Successful()
    {
        var notification1 = new Notification()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            SeverityId = Guid.NewGuid(),
            DescriptionId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            SentTime = DateTime.UtcNow,
            AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        var notification2 = new Notification()
        {
            Id = Guid.NewGuid(),
            UserId = notification1.UserId,
            LineId = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            SeverityId = Guid.NewGuid(),
            DescriptionId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            SentTime = DateTime.UtcNow,
            AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        var notification3 = new Notification()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            SeverityId = Guid.NewGuid(),
            DescriptionId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            SentTime = DateTime.UtcNow,
            AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        await _context.Notifications.AddRangeAsync([
            notification1, 
            notification2, 
            notification3]);

        await _context.SaveChangesAsync();

        var deletedAt = DateTime.UtcNow;

        var result = await _repository.RemoveNotificationsByUserIdAsync(notification1.UserId, deletedAt);
        result.IsSuccess.Should().BeTrue();

        var deletedUsers = await _context.Notifications.
            Where(x => x.UserId == notification1.UserId)
            .AsNoTracking()
            .ToListAsync();

        deletedUsers.Should().HaveCount(2);

        var deletedDict = deletedUsers.ToDictionary(x => x.Id);

        var n1 = deletedDict[notification1.Id];
        var n2 = deletedDict[notification2.Id];

        n1.Id.Should().Be(notification1.Id);
        n1.UserId.Should().Be(notification1.UserId);
        n1.LineId.Should().Be(notification1.LineId);
        n1.DisruptionId.Should().Be(notification1.DisruptionId);
        n1.SeverityId.Should().Be(notification1.SeverityId);
        n1.DescriptionId.Should().Be(notification1.DescriptionId);
        n1.StartStationId.Should().Be(notification1.StartStationId);
        n1.EndStationId.Should().Be(notification1.EndStationId);
        n1.SentTime.Should().BeCloseTo(notification1.SentTime, TimeSpan.FromSeconds(30));
        n1.AffectedStationIds.Should().BeEquivalentTo(notification1.AffectedStationIds);
        n1.DeletedAt.Should().BeCloseTo(deletedAt, TimeSpan.FromSeconds(30));

        n2.Id.Should().Be(notification2.Id);
        n2.UserId.Should().Be(notification2.UserId);
        n2.LineId.Should().Be(notification2.LineId);
        n2.DisruptionId.Should().Be(notification2.DisruptionId);
        n2.SeverityId.Should().Be(notification2.SeverityId);
        n2.DescriptionId.Should().Be(notification2.DescriptionId);
        n2.StartStationId.Should().Be(notification2.StartStationId);
        n2.EndStationId.Should().Be(notification2.EndStationId);
        n2.SentTime.Should().BeCloseTo(notification2.SentTime, TimeSpan.FromSeconds(30));
        n2.AffectedStationIds.Should().BeEquivalentTo(notification2.AffectedStationIds);
        n2.DeletedAt.Should().BeCloseTo(deletedAt, TimeSpan.FromSeconds(30));

        var leftAloneUser = _context.Notifications.FirstOrDefault(x => x.UserId == notification3.UserId);
        leftAloneUser.Should().NotBeNull();
        leftAloneUser.DeletedAt.Should().BeNull();

    }
}
