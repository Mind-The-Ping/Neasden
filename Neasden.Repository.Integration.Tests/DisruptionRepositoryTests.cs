using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Database;
using Neasden.Models;
using Neasden.Repository.Repositories;

namespace Neasden.Repository.Integration.Tests;

public class DisruptionRepositoryTests
{
    private readonly NeasdenDbContext _neasdenDbContext;
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private readonly DisruptionRepository _disruptionRepository;

    public DisruptionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<NeasdenDbContext>()
             .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
             .Options;

        _neasdenDbContext = new NeasdenDbContext(options);

        _neasdenDbContext.Database.EnsureDeleted();
        _neasdenDbContext.Database.EnsureCreated();

        _disruptionRepository = new DisruptionRepository(_neasdenDbContext);
    }

    [Fact]
    public async Task DisruptionRepository_GetDisruptionByIdAsync_Successful()
    {
        var disruption = new Disruption
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };

        await _neasdenDbContext.Disruptions.AddAsync(disruption);
        await _neasdenDbContext.SaveChangesAsync();

        var result = await _disruptionRepository.GetDisruptionByIdAsync(disruption.Id);
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().BeEquivalentTo(disruption);
    }

    [Fact]
    public async Task DisruptionRepository_GetDisruptionByIdAsync_No_Matching_User_Fails()
    {
        var id = Guid.NewGuid();
       
        var result = await _disruptionRepository.GetDisruptionByIdAsync(id);
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be($"Could not find disruption {id} on the database.");
    }

    [Fact]
    public async Task DisruptionRepository_AddDisruptionEndTimeAsync_Successful()
    {
        var disruption = new Disruption
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };

        await _neasdenDbContext.Disruptions.AddAsync(disruption);
        await _neasdenDbContext.SaveChangesAsync();

        var disruptionEnd = new DisruptionEnd(disruption.Id, DateTime.UtcNow);
        var result = await _disruptionRepository.AddDisruptionEndTimesAsync([disruptionEnd]);

        result.IsSuccess.Should().BeTrue();

        var disruptionDb = await _neasdenDbContext.Disruptions
            .SingleOrDefaultAsync(x => x.Id == disruption.Id);

        disruptionDb.Should().NotBeNull();
        disruptionDb.Id.Should().Be(disruptionEnd.Id);
        disruptionDb.EndTime.Should().Be(disruptionEnd.EndTime);
    }

    [Fact]
    public async Task DisruptionRepository_AddDisruptionSeverityAsync_Successful()
    {
        var disruptionSeverity = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Minor
        };

        var result = await _disruptionRepository
            .AddDisruptionSeveritiesAsync([disruptionSeverity]);

        result.IsSuccess.Should().BeTrue();

        var disruptionSeveritySaved = await _neasdenDbContext.Severities
            .SingleOrDefaultAsync(x => x.Id == disruptionSeverity.Id);

        disruptionSeveritySaved.Should().NotBeNull();
        disruptionSeveritySaved.Should().BeEquivalentTo(disruptionSeverity);
    }

    [Fact]
    public async Task DisruptionRepository_GetDisruptionSeverityAsync_Successful()
    {
        var severity = new DisruptionSeverity
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            Severity = Severity.Minor,
            StartTime = DateTime.UtcNow
        };

        await _neasdenDbContext.Severities.AddAsync(severity);
        await _neasdenDbContext.SaveChangesAsync();

        var result = await _disruptionRepository.GetDisruptionSeverityByIdAsync(severity.Id);
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().BeEquivalentTo(severity);
    }

    [Fact]
    public async Task DisruptionRepository_GetDisruptionSeverityAsync_No_Severity_Disruption_Fails()
    {
        var id = Guid.NewGuid();
        var result = await _disruptionRepository.GetDisruptionSeverityByIdAsync(id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be($"Disruption severity {id} could not be found on the database.");
    }

    [Fact]
    public async Task DisruptionRepository_AddDescriptionsAsync_Successful()
    {
        var description = new DisruptionDescription
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            Description = "This is a test.",
            CreatedAt = DateTime.UtcNow
        };

        var result = await _disruptionRepository.AddDescriptionsAsync([description]);
        result.IsSuccess.Should().BeTrue();

        var record = await _neasdenDbContext.Descriptions.SingleOrDefaultAsync(d => d.Id == description.Id);
        record.Should().NotBeNull();
        record.Should().BeEquivalentTo(description);
    }

    [Fact]
    public async Task DisruptionRepository_GetDisruptionDescriptionByIdAsync_Successful()
    {
        var description = new DisruptionDescription
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            Description = "This is a test.",
            CreatedAt = DateTime.UtcNow
        };

        await _neasdenDbContext.Descriptions.AddAsync(description);
        await _neasdenDbContext.SaveChangesAsync();

        var result = await _disruptionRepository.GetDisruptionDescriptionByIdAsync(description.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(description);
    }
}
