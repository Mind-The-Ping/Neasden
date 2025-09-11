using FluentAssertions;
using Neasden.Repository.Options;
using Neasden.Repository.Redis.Models;
using Testcontainers.Redis;

namespace Neasden.Repository.Redis.Integration.Tests;
public class NotificationRepositoryTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;

    public NotificationRepositoryTests()
    {
        _redisContainer = new RedisBuilder()
          .WithImage("redis:7.2")
          .WithCleanUp(true)
          .Build();
    }

    public async Task InitializeAsync() =>
        await _redisContainer.StartAsync();

    public async Task DisposeAsync() =>
        await _redisContainer.DisposeAsync();

    [Fact]
    public async Task NotificationRepository_Save_Load_Single_Notification()
    {
        var notification = new Notification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationSentBy.Sms,
            DateTime.UtcNow);

        var repository = CreateRepository();

        var savedResults = await repository.SaveNotificationAsync(notification);
        var loadResults = await repository.GetNotificationsAsync();

        savedResults.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.First().Should().Be(notification);
    }

    [Fact]
    public async Task NotificationRepository_Save_Load_Multiple_Notifications()
    {
        var notification1 = new Notification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationSentBy.Sms,
            DateTime.UtcNow);

        var notification2 = new Notification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationSentBy.Sms,
            DateTime.UtcNow);

        var repository = CreateRepository();

        var savedResults1 = await repository.SaveNotificationAsync(notification1);
        var savedResults2 = await repository.SaveNotificationAsync(notification2);
        var loadResults = await repository.GetNotificationsAsync();

        savedResults1.IsSuccess.Should().BeTrue();
        savedResults2.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.First().Should().Be(notification1);
        loadResults.Value.Last().Should().Be(notification2);
    }

    [Fact]
    public async Task NotificationRepository_Save_Delete_Notification()
    {
        var notification = new Notification(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificationSentBy.Sms,
            DateTime.UtcNow);

        var repository = CreateRepository();

        _ = await repository.SaveNotificationAsync(notification);
        var deleteResults = await repository.DeleteNotificationsAsync();
        var loadResults = await repository.GetNotificationsAsync();

        deleteResults.IsSuccess.Should().BeTrue();
        loadResults.IsFailure.Should().BeTrue();
        loadResults.Error.Should().Be("No notifications found in Redis.");
    }

    private NotificationRepository CreateRepository()
    {
        var options = Microsoft.Extensions.Options.Options.Create(
          new RedisOptions()
          {
              ConnectionString = _redisContainer.GetConnectionString(),
              DisruptionKey = "disruptions",
              DisruptionSeverityKey = "disruptionSeveritys",
              DisruptionEndKey = "disruptionEnds",
              NotificationKey = "notifications"
          });

        return new NotificationRepository(options);
    }
}
