using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;
using Neasden.Repository.Redis.Options;
using NSubstitute;
using StackExchange.Redis;
using Neasden.Repository.Write;
using PostgresDisruptionRepository = Neasden.Repository.Write.WriteDisruptionRepository;
using PostgresNotificationRepository = Neasden.Repository.Write.WriteNotificationRepository;
using RedisDisruptionRepository = Neasden.Repository.Redis.DisruptionRepository;
using RedisNotificationRepository = Neasden.Repository.Redis.NotificationRepository;

namespace Neasden.Saver.Integration.Tests;

public class SaverTests
{
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private readonly WriteDbContext _context;
    private readonly Saver _saver;

    private readonly RedisNotificationRepository _redisNotification;
    private readonly RedisDisruptionRepository _redisDisruption;

    private readonly PostgresNotificationRepository _postgresNotification;
    private readonly PostgresDisruptionRepository _postgresDisruption;

    public SaverTests()
    {
        var options = new DbContextOptionsBuilder<WriteDbContext>()
            .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
            .Options;

        _context = new WriteDbContext(options);

        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        var redisOptions = new RedisOptions()
        {
            ConnectionString = "localhost:6382",
            DisruptionKey = "disruptions",
            DisruptionSeverityKey = "disruptionSeverities",
            DisruptionEndKey = "disruptionEnds",
            NotificationKey = "notifications",
            DescriptionKey = "descriptions"
        };

        var iRedisOptions = Microsoft.Extensions.Options.Options.Create(redisOptions);

        var multiplexer = ConnectionMultiplexer.Connect("localhost:6382");

        var redisDisruptionLogger = Substitute.For<ILogger<RedisDisruptionRepository>>();
        var redisNotificationLogger = Substitute.For<ILogger<RedisNotificationRepository>>();

        _redisDisruption = new RedisDisruptionRepository(iRedisOptions, multiplexer, redisDisruptionLogger);
        _redisNotification = new RedisNotificationRepository(iRedisOptions, multiplexer, redisNotificationLogger);

        var postgresDisruptionLogger = Substitute.For<ILogger<PostgresDisruptionRepository>>();
        var postgresNotificationLogger = Substitute.For<ILogger<PostgresNotificationRepository>>();

        _postgresDisruption = new PostgresDisruptionRepository(_context, postgresDisruptionLogger);
        _postgresNotification = new PostgresNotificationRepository(_context, postgresNotificationLogger);

        _saver = new Saver(
            _redisNotification,
            _redisDisruption,
            _postgresNotification,
            _postgresDisruption);
    }

    [Fact]
    public async Task Saver_DrainDisruptionsAsync_Successful()
    {
        var disruption = new Disruption()
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(5)
        };

        _ = await _redisDisruption.SaveDisruptionAsync(disruption);
        
        var result = await _saver.DrainDisruptionsAsync();
        var count = await _redisDisruption.GetDisruptionCountAsync();

        result.IsSuccess.Should().BeTrue();
        count.Should().Be(0); 

        var record = await _context.Disruptions.FirstOrDefaultAsync(x => x.Id == disruption.Id);
        record.Should().BeEquivalentTo(disruption);
    }

    [Fact]
    public async Task Saver_DrainDisruptionSeveritiesAsync_Successful()
    {
        var disruptionSeverity = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Suspended,
        };

        _ = await _redisDisruption.SaveDisruptionSeverityAsync(disruptionSeverity);

        var result = await _saver.DrainDisruptionSeveritiesAsync();
        var count = await _redisDisruption.GetDisruptionCountAsync();

        result.IsSuccess.Should().BeTrue();
        count.Should().Be(0);

        var record = await _context.Severities.FirstOrDefaultAsync(x => x.Id == disruptionSeverity.Id);
        record.Should().BeEquivalentTo(disruptionSeverity);
    }

    [Fact]
    public async Task Saver_DrainDisruptionEndsAsync_Successful()
    {
        var disruption = new Disruption()
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
        };

        _ = await _redisDisruption.SaveDisruptionAsync(disruption);
        _ = await _saver.DrainDisruptionsAsync();

        var endTime = DateTime.UtcNow.AddHours(2);
        var disruptionEnd = new DisruptionEnd(disruption.Id, endTime);

        _ = await _redisDisruption.SaveDisruptionEndAsync(disruptionEnd);

        var result = await _saver.DrainDisruptionEndsAsync();
        var count = await _redisDisruption.GetDisruptionEndCountAsync();

        result.IsSuccess.Should().BeTrue();
        count.Should().Be(0);

        var record = await _context.Disruptions.FirstOrDefaultAsync(x => x.Id == disruption.Id);
        record.EndTime.Should().Be(endTime);
    }

    [Fact]
    public async Task Saver_DrainNotifcationAsync_Successful()
    {
        var notification = new Notification()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            SeverityId = Guid.NewGuid(),
            DescriptionId = Guid.NewGuid(),
            NotificationSentBy = NotificationSentBy.Sms,
            SentTime = DateTime.UtcNow,
            AffectedStationIds = [Guid.NewGuid(), Guid.NewGuid()]
        };

        _ = await _redisNotification.SaveNotificationAsync(notification);

        var result = await _saver.DrainNotificationsAsync();
        var count = await _redisNotification.GetNotificationCountAsync();

        result.IsSuccess.Should().BeTrue();
        count.Should().Be(0);

        var record = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == notification.Id);
        record.Should().BeEquivalentTo(notification);
    }

    [Fact]
    public async Task Saver_DrainDescriptionsAsync_Successful()
    {
        var description = new DisruptionDescription
        {
            Id = Guid.NewGuid(),
            Description = "Knives Out, Fangs out ??.",
            CreatedAt = DateTime.UtcNow
        };

        _ = await _redisDisruption.SaveDisruptionDescriptionAsync(description);

        var result = await _saver.DrainDisruptionDescriptionsAsync();
        var count = await _saver.DisruptionDescriptionCountAsync();

        result.IsSuccess.Should().BeTrue();
        count.Should().Be(0);

        var record = await _context.Descriptions.FirstOrDefaultAsync(x => x.Id == description.Id);
        record.Should().BeEquivalentTo(description);
    }
}
