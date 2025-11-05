using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;
using Neasden.Repository.Read;
using Neasden.Repository.Write;
using NSubstitute;

namespace Neasden.Repository.Integration.Tests.Read;
public class ReadDisruptionRepositoryTests
{
    private readonly WriteDbContext _writeContext;
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private readonly ReadDisruptionRepository _repository;

    public ReadDisruptionRepositoryTests()
    {
        var readOptions = new DbContextOptionsBuilder<ReadDbContext>()
            .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
            .Options;

        var _contextFactory = new TestDbContextFactory(readOptions);

        using (var context = _contextFactory.CreateDbContext())
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        var logger = Substitute.For<ILogger<ReadDisruptionRepository>>();

        _repository = new ReadDisruptionRepository(_contextFactory, logger);

        var writeOptions = new DbContextOptionsBuilder<WriteDbContext>()
           .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
           .Options;

        _writeContext = new WriteDbContext(writeOptions);
    }

    [Fact]
    public async Task ReadDisruptionRepository_GetDisruptionByIdAsync_Successful()
    {
        var disruption = new Disruption
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };

        await _writeContext.Disruptions.AddAsync(disruption);
        await _writeContext.SaveChangesAsync();

        var result = await _repository.GetDisruptionByIdAsync(disruption.Id);
        result.IsSuccess.Should().BeTrue();

        result.Value.Id.Should().Be(disruption.Id);
        result.Value.LineId.Should().Be(disruption.LineId);
        result.Value.StartStationId.Should().Be(disruption.StartStationId);
        result.Value.EndStationId.Should().Be(disruption.EndStationId);
        result.Value.StartTime.Should().BeCloseTo(disruption.StartTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ReadDisruptionRepository_GetDisruptionByIdAsync_No_Matching_User_Fails()
    {
        var id = Guid.NewGuid();

        var result = await _repository.GetDisruptionByIdAsync(id);
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be($"Disruption {id} does not exist on the database.");
    }

    [Fact]
    public async Task ReadDisruptionRepository_GetDisruptionSeverityAsync_Successful()
    {
        var severity = new DisruptionSeverity
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            Severity = Severity.Minor,
            StartTime = DateTime.UtcNow
        };

        await _writeContext.Severities.AddAsync(severity);
        await _writeContext.SaveChangesAsync();

        var result = await _repository.GetDisruptionSeverityByIdAsync(severity.Id);
        result.IsSuccess.Should().BeTrue();

        result.Value.Id.Should().Be(severity.Id);
        result.Value.DisruptionId.Should().Be(severity.DisruptionId);
        result.Value.Severity.Should().Be(severity.Severity);
        result.Value.StartTime.Should().BeCloseTo(severity.StartTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ReadDisruptionRepository_GetDisruptionSeverityAsync_No_Severity_Disruption_Fails()
    {
        var id = Guid.NewGuid();
        var result = await _repository.GetDisruptionSeverityByIdAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be($"Disruption severity {id} does not exist on this database.");
    }

    [Fact]
    public async Task ReadDisruptionRepository_GetDisruptionDescriptionByIdAsync_Successful()
    {
        var description = new DisruptionDescription
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            Description = "This is a test.",
            CreatedAt = DateTime.UtcNow
        };

        await _writeContext.Descriptions.AddAsync(description);
        await _writeContext.SaveChangesAsync();

        var result = await _repository.GetDisruptionDescriptionByIdAsync(description.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value.Id.Should().Be(description.Id);
        result.Value.DisruptionId.Should().Be(description.DisruptionId);
        result.Value.Description.Should().Be(description.Description);
        result.Value.CreatedAt.Should().BeCloseTo(description.CreatedAt, TimeSpan.FromSeconds(1));
    }
}
