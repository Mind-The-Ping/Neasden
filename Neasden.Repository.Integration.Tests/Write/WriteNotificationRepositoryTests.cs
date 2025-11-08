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
}
