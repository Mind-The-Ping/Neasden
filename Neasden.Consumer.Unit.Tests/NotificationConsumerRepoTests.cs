using FluentAssertions;
using Neasden.Consumer.Repositories;
using Neasden.Repository.Redis;
using Neasden.Repository.Redis.Options;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Neasden.Consumer.Unit.Tests;
public class NotificationConsumerRepoTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;

    public NotificationConsumerRepoTests()
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
    public async Task NotificationConsumerRepo_AddNotificationAsync_Wrong_Fails()
    {
        var options = Microsoft.Extensions.Options.Options.Create(
           new RedisOptions()
           {
               ConnectionString = _redisContainer.GetConnectionString(),
               DisruptionKey = "disruptions",
               DisruptionSeverityKey = "disruptionSeveritys",
               DisruptionEndKey = "disruptionEnds",
               NotificationKey = "notifications",
               DescriptionKey = "descriptions"
           });

        var multiplexer = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());

        var repository = new NotificationRepository(options, multiplexer);
        var repo = new NotificationConsumerRepo(repository);

        var body = new BinaryData([]);
        var result = await repo.AddNotificationAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Notification message could not be deserialized.");
    }
}
