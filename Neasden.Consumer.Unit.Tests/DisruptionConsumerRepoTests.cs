using FluentAssertions;
using Microsoft.Extensions.Logging;
using Neasden.Consumer.Repositories;
using Neasden.Repository.Redis;
using Neasden.Repository.Redis.Options;
using NSubstitute;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Neasden.Consumer.Unit.Tests;

public class DisruptionConsumerRepoTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;
    private readonly ILogger<DisruptionConsumerRepo> _logger;

    public DisruptionConsumerRepoTests()
    {
        _redisContainer = new RedisBuilder()
         .WithImage("redis:7.2")
         .WithCleanUp(true)
         .Build();

        _logger = Substitute.For<ILogger<DisruptionConsumerRepo>>();
    }

    public async Task InitializeAsync() =>
        await _redisContainer.StartAsync();

    public async Task DisposeAsync() =>
        await _redisContainer.DisposeAsync();

    [Fact]
    public async Task DisruptionConsumerRepo_AddDisruptionAsync_Wrong_Fails()
    {
        var repository = CreateRepository();
        var repo = new DisruptionConsumerRepo(repository, _logger);

        var body = new BinaryData([]);
        var result = await repo.AddDisruptionAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Disruption message could not be deserialized.");
    }

    [Fact]
    public async Task DisruptionConsumerRepo_UpdateDisruptionSeverityAsync_Wrong_Fails()
    {
        var repository = CreateRepository();
        var repo = new DisruptionConsumerRepo(repository, _logger);

        var body = new BinaryData([]);
        var result = await repo.UpdateDisruptionSeverityAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Disruption severity message could not be deserialized.");
    }

    [Fact]
    public async Task DisruptionConsumerRepo_AddDisruptionEndTimeAsync_Wrong_Fails()
    {
        var repository = CreateRepository();
        var repo = new DisruptionConsumerRepo(repository, _logger);

        var body = new BinaryData([]);
        var result = await repo.AddDisruptionEndTimeAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Disruption end time message could not be deserialized.");
    }

    [Fact]
    public async Task DisruptionConsumerRepo_AddDisruptionDescriptionAsync_Wrong_Fails()
    {
        var repository = CreateRepository();
        var repo = new DisruptionConsumerRepo(repository, _logger);

        var body = new BinaryData([]);
        var result = await repo.AddDisruptionDescriptionAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Disruption description message could not be deserialized.");
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
              NotificationKey = "notifications",
              DescriptionKey = "descriptions"
          });

        var multiplexer = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        var logger = Substitute.For<ILogger<DisruptionRepository>>();

        return new DisruptionRepository(options, multiplexer, logger);
    }
}
