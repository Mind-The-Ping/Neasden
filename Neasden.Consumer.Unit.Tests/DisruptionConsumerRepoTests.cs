using FluentAssertions;
using Neasden.Consumer.Repositories;
using Neasden.Repository.Options;
using Neasden.Repository.Redis;
using Testcontainers.Redis;

namespace Neasden.Consumer.Unit.Tests;

public class DisruptionConsumerRepoTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;

    public DisruptionConsumerRepoTests()
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
    public async Task DisruptionConsumerRepo_AddDisruptionAsync_Wrong_Fails()
    {
        var repository = CreateRepository();
        var repo = new DisruptionConsumerRepo(repository);

        var body = new BinaryData([]);
        var result = await repo.AddDisruptionAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Disruption message could not be deserialized.");
    }

    [Fact]
    public async Task DisruptionConsumerRepo_UpdateDisruptionSeverityAsync_Wrong_Fails()
    {
        var repository = CreateRepository();
        var repo = new DisruptionConsumerRepo(repository);

        var body = new BinaryData([]);
        var result = await repo.UpdateDisruptionSeverityAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Disruption severity message could not be deserialized.");
    }

    [Fact]
    public async Task DisruptionConsumerRepo_AddDisruptionEndTimeAsync_Wrong_Fails()
    {
        var repository = CreateRepository();
        var repo = new DisruptionConsumerRepo(repository);

        var body = new BinaryData([]);
        var result = await repo.AddDisruptionEndTimeAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Disruption end time message could not be deserialized.");
    }

    private DisruptionRepository CreateRepository()
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

        return new DisruptionRepository(options);
    }
}
