using FluentAssertions;
using Neasden.Repository.Options;
using Neasden.Repository.Redis.Models;
using Testcontainers.Redis;

namespace Neasden.Repository.Redis.Integration.Tests;

public class DisruptionRepositoryTests : IAsyncLifetime
{
    private readonly RedisContainer _redisContainer;

    public DisruptionRepositoryTests()
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
    public async Task DisruptionRepository_Save_Load_Single_Disruption()
    {
        var disruption = new Disruption(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "This is a test nerd.",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(5));

        var repository = CreateRepository();

        var saveResults = await repository.SaveDisruptionAsync(disruption);
        var loadResults = await repository.GetDisruptionsAsync();

        saveResults.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(1);
        loadResults.Value.First().Should().Be(disruption);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Load_Multiple_Disruptions()
    {
        var disruption1 = new Disruption(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "This is a test nerd.",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(5));

        var disruption2 = new Disruption(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "This is a test dweeb.",
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(5));

        var repository = CreateRepository();

        var saveResults1 = await repository.SaveDisruptionAsync(disruption1);
        var saveResults2 = await repository.SaveDisruptionAsync(disruption2);
        var loadResults = await repository.GetDisruptionsAsync();

        saveResults1.IsSuccess.Should().BeTrue();
        saveResults2.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(2);
        loadResults.Value.First().Should().Be(disruption1);
        loadResults.Value.Last().Should().Be(disruption2);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Load_Single_DisruptionSeverity()
    {
        var disruptinSeverity = new DisruptionSeverity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            Severity.Suspended);

        var repository = CreateRepository();

        var saveResults = await repository.SaveDisruptionSeverityAsync(disruptinSeverity);
        var loadResults = await repository.GetDisruptionSeveritiesAsync();

        saveResults.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(1);
        loadResults.Value.First().Should().Be(disruptinSeverity);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Load_Multiple_DisruptionSeveritys()
    {
        var disruptinSeverity1 = new DisruptionSeverity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            Severity.Suspended);

        var disruptinSeverity2 = new DisruptionSeverity(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            Severity.Suspended);

        var repository = CreateRepository();

        var saveResults1 = await repository.SaveDisruptionSeverityAsync(disruptinSeverity1);
        var saveResults2 = await repository.SaveDisruptionSeverityAsync(disruptinSeverity2);
        var loadResults = await repository.GetDisruptionSeveritiesAsync();

        saveResults1.IsSuccess.Should().BeTrue();
        saveResults2.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(2);
        loadResults.Value.First().Should().Be(disruptinSeverity1);
        loadResults.Value.Last().Should().Be(disruptinSeverity2);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Load_Single_DisruptionEnd()
    {
        var disruptionEnd = new DisruptionEnd(Guid.NewGuid(), DateTime.UtcNow);

        var repository = CreateRepository();

        var saveResults = await repository.SaveDisruptionEndAsync(disruptionEnd);
        var loadResults = await repository.GetDisruptionEndsAsync();

        saveResults.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(1);
        loadResults.Value.First().Should().Be(disruptionEnd);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Load_Multiple_DisruptionEnd()
    {
        var disruptionEnd1 = new DisruptionEnd(Guid.NewGuid(), DateTime.UtcNow);
        var disruptionEnd2 = new DisruptionEnd(Guid.NewGuid(), DateTime.UtcNow);

        var repository = CreateRepository();

        var saveResults1 = await repository.SaveDisruptionEndAsync(disruptionEnd1);
        var saveResults2 = await repository.SaveDisruptionEndAsync(disruptionEnd2);
        var loadResults = await repository.GetDisruptionEndsAsync();

        saveResults1.IsSuccess.Should().BeTrue();
        saveResults2.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(2);
        loadResults.Value.First().Should().Be(disruptionEnd1);
        loadResults.Value.Last().Should().Be(disruptionEnd2);
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
