using Castle.Core.Logging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;
using Neasden.Repository.Database;
using Neasden.Repository.Redis.Options;
using NSubstitute;
using StackExchange.Redis;
using PostgresDisruptionRepository = Neasden.Repository.Repositories.DisruptionRepository;
using PostgresNotificationRepository = Neasden.Repository.Repositories.NotificationRepository;
using RedisDisruptionRepository = Neasden.Repository.Redis.DisruptionRepository;
using RedisNotificationRepository = Neasden.Repository.Redis.NotificationRepository;

namespace Neasden.Saver.Integration.Tests;

public class SaverTests
{
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private readonly Saver _saver;

    private readonly RedisNotificationRepository _redisNotification;
    private readonly RedisDisruptionRepository _redisDisruption;

    private readonly PostgresNotificationRepository _postgresNotification;
    private readonly PostgresDisruptionRepository _postgresDisruption;

    public SaverTests()
    {
        var options = new DbContextOptionsBuilder<NeasdenDbContext>()
            .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
            .Options;

        var neasdenDbContext = new NeasdenDbContext(options);

        neasdenDbContext.Database.EnsureDeleted();
        neasdenDbContext.Database.EnsureCreated();

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

        _postgresDisruption = new PostgresDisruptionRepository(neasdenDbContext, postgresDisruptionLogger);
        _postgresNotification = new PostgresNotificationRepository(neasdenDbContext, postgresNotificationLogger);

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

        var record = await _postgresDisruption.GetDisruptionByIdAsync(disruption.Id);

        record.IsSuccess.Should().BeTrue();
        record.Value.Should().BeEquivalentTo(disruption);
    }

    [Fact]
    public async Task Saver_DrainDisruptionSeveritiesAsync_Successful()
    {
        var disruptionSeverity = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Suspended,
        };

        _ = await _redisDisruption.SaveDisruptionSeverityAsync(disruptionSeverity);

        var result = await _saver.DrainDisruptionSeveritiesAsync();
        var count = await _redisDisruption.GetDisruptionCountAsync();

        result.IsSuccess.Should().BeTrue();
        count.Should().Be(0);

        var record = await _postgresDisruption.GetDisruptionSeverityByIdAsync(disruptionSeverity.Id);

        record.IsSuccess.Should().BeTrue();
        record.Value.Should().BeEquivalentTo(disruptionSeverity);
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

        var record = await _postgresDisruption.GetDisruptionByIdAsync(disruption.Id);
        record.IsSuccess.Should().BeTrue();
        record.Value.EndTime.Should().Be(endTime);
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

        var record = await _postgresNotification.GetNotificationByIdAsync(notification.Id);
        record.IsSuccess.Should().BeTrue();
        record.Value.Should().BeEquivalentTo(notification);
    }

    [Fact]
    public async Task Saver_DrainDescriptionsAsync_Successful()
    {
        var description = new DisruptionDescription
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            Description = "Knives Out, Fangs out ??.",
            CreatedAt = DateTime.UtcNow
        };

        _ = await _redisDisruption.SaveDisruptionDescriptionAsync(description);

        var result = await _saver.DrainDisruptionDescriptionsAsync();
        var count = await _saver.DisruptionDescriptionCountAsync();

        result.IsSuccess.Should().BeTrue();
        count.Should().Be(0);

        var record = await _postgresDisruption.GetDisruptionDescriptionByIdAsync(description.Id);
        record.IsSuccess.Should().BeTrue();
        record.Value.Should().BeEquivalentTo(description);
    }
}
