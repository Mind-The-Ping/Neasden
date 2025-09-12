using FluentAssertions;
using Neasden.Repository.Options;
using Neasden.Models;
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

        var disruption = new Disruption()
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            Description = "This is a test nerd.",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(5)
        };

        var repository = CreateRepository();

        var saveResults = await repository.SaveDisruptionAsync(disruption);
        var loadResults = await repository.GetDisruptionsAsync();

        saveResults.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(1);
        loadResults.Value.First().Should().BeEquivalentTo(disruption);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Load_Multiple_Disruptions()
    {
        var disruption1 = new Disruption()
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            Description = "This is a test nerd.",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(5)
        };

        var disruption2 = new Disruption()
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            Description = "This is a test dweeb.",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(5)
        };

        var repository = CreateRepository();

        var saveResults1 = await repository.SaveDisruptionAsync(disruption1);
        var saveResults2 = await repository.SaveDisruptionAsync(disruption2);
        var loadResults = await repository.GetDisruptionsAsync();

        saveResults1.IsSuccess.Should().BeTrue();
        saveResults2.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(2);
        loadResults.Value.First().Should().BeEquivalentTo(disruption1);
        loadResults.Value.Last().Should().BeEquivalentTo(disruption2);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Load_Single_When_Same_Sent_Disruption()
    {
        var disruption = new Disruption()
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            Description = "This is a test nerd.",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(5)
        };

        var repository = CreateRepository();

        var saveResults1 = await repository.SaveDisruptionAsync(disruption);
        var saveResults2 = await repository.SaveDisruptionAsync(disruption);
        var loadResults = await repository.GetDisruptionsAsync();

        saveResults1.IsSuccess.Should().BeTrue();
        saveResults2.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(1);
        loadResults.Value.First().Should().BeEquivalentTo(disruption);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Delete_Disruption()
    {
        var disruption = new Disruption()
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            Description = "This is a test nerd.",
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(5)
        };

        var repository = CreateRepository();

        _ = await repository.SaveDisruptionAsync(disruption);
        var deleteResults = await repository.DeleteDisruptionsAsync();
        var loadResults = await repository.GetDisruptionsAsync();

        deleteResults.IsSuccess.Should().BeTrue();
        loadResults.IsFailure.Should().BeTrue();
        loadResults.Error.Should().Be("No disruptions found in Redis.");
    }

    [Fact]
    public async Task DisruptionRepository_Save_Load_Single_DisruptionSeverity()
    {
        var disruptionSeverity = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Suspended,
        };

        var repository = CreateRepository();

        var saveResults = await repository.SaveDisruptionSeverityAsync(disruptionSeverity);
        var loadResults = await repository.GetDisruptionSeveritiesAsync();

        saveResults.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(1);
        loadResults.Value.First().Should().BeEquivalentTo(disruptionSeverity);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Load_Multiple_DisruptionSeveritys()
    {
        var disruptionSeverity1 = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Suspended,
        };

        var disruptionSeverity2 = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Suspended,
        };

        var repository = CreateRepository();

        var saveResults1 = await repository.SaveDisruptionSeverityAsync(disruptionSeverity1);
        var saveResults2 = await repository.SaveDisruptionSeverityAsync(disruptionSeverity2);
        var loadResults = await repository.GetDisruptionSeveritiesAsync();

        saveResults1.IsSuccess.Should().BeTrue();
        saveResults2.IsSuccess.Should().BeTrue();
        loadResults.IsSuccess.Should().BeTrue();

        loadResults.Value.Count().Should().Be(2);
        loadResults.Value.First().Should().BeEquivalentTo(disruptionSeverity1);
        loadResults.Value.Last().Should().BeEquivalentTo(disruptionSeverity2);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Delete_DisruptionSeverity()
    {
        var disruptionSeverity = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Suspended,
        };

        var repository = CreateRepository();

        _ = await repository.SaveDisruptionSeverityAsync(disruptionSeverity);
        var deleteResults = await repository.DeleteDisruptionSeveritiesAsync();
        var loadResults = await repository.GetDisruptionSeveritiesAsync();

        deleteResults.IsSuccess.Should().BeTrue();
        loadResults.IsFailure.Should().BeTrue();
        loadResults.Error.Should().Be("No disruption severities found in Redis.");
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
        loadResults.Value.First().Should().BeEquivalentTo(disruptionEnd);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Load_Multiple_DisruptionEnds()
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
        loadResults.Value.First().Should().BeEquivalentTo(disruptionEnd1);
        loadResults.Value.Last().Should().BeEquivalentTo(disruptionEnd2);
    }

    [Fact]
    public async Task DisruptionRepository_Save_Delete_DisruptionEnd()
    {
        var disruptionEnd = new DisruptionEnd(Guid.NewGuid(), DateTime.UtcNow);

        var repository = CreateRepository();

        _ = await repository.SaveDisruptionEndAsync(disruptionEnd);
        var deleteResults = await repository.DeleteDisruptionEndsAsync();
        var loadResults = await repository.GetDisruptionEndsAsync();

        deleteResults.IsSuccess.Should().BeTrue();
        loadResults.IsFailure.Should().BeTrue();
        loadResults.Error.Should().Be("No disruption ends found in Redis.");
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
