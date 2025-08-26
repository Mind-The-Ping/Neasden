using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Database;
using Neasden.Repository.Models;
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
    public async Task DisruptionRepository_CreateDisruptionAsync_Successful()
    {
        var id = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var startStationId = Guid.NewGuid();
        var endStationId = Guid.NewGuid();
        var description = "Something horrific happened on the District line please bare with us as we clean up the mess.";
        var startTime = DateTime.UtcNow;

        var result = await _disruptionRepository.AddDisruptionAsync(
                                id,
                                lineId,
                                startStationId,
                                endStationId,
                                description,
                                startTime);

        
        result.IsSuccess.Should().BeTrue();

        var disruption = _neasdenDbContext.Disruptions.SingleOrDefault(x => x.Id == id);
        disruption.Should().NotBeNull();

        disruption.Id.Should().Be(id);
        disruption.LineId.Should().Be(lineId);
        disruption.StartStationId.Should().Be(startStationId);
        disruption.EndStationId.Should().Be(endStationId);
        disruption.Description.Should().Be(description);
        disruption.StartTime.Should().Be(startTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task DisruptionRepository_CreateDisruptionAsync_EmptyDescription_Fails(string description)
    {
        var id = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var startStationId = Guid.NewGuid();
        var endStationId = Guid.NewGuid();
        var dateTime = DateTime.UtcNow;

        var result = await _disruptionRepository.AddDisruptionAsync(
                               id,
                               lineId,
                               startStationId,
                               endStationId,
                               description,
                               dateTime);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be($"Description is empty for disruption {id}.");
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
            Description = "Something not human clambered out of the bottom of the Northen line, please stay away.",
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
            Description = "Something not human clambered out of the bottom of the Northen line, please stay away.",
            StartTime = DateTime.UtcNow
        };

        await _neasdenDbContext.Disruptions.AddAsync(disruption);
        await _neasdenDbContext.SaveChangesAsync();

        var endTime = DateTime.UtcNow.AddMinutes(30);
        var result = await _disruptionRepository.AddDisruptionEndTimeAsync(disruption.Id, endTime);

        result.IsSuccess.Should().BeTrue();

        var disruptionDb = await _neasdenDbContext.Disruptions
            .SingleOrDefaultAsync(x => x.Id == disruption.Id);

        disruptionDb.Should().NotBeNull();
        disruptionDb.Id.Should().Be(disruption.Id);
        disruptionDb.EndTime.Should().Be(endTime);
    }

    [Fact]
    public async Task DisruptionRepository_AddDisruptionEndTimeAsync_No_Disruption_Fails()
    {
        var id = Guid.NewGuid();
        var result = await _disruptionRepository.AddDisruptionEndTimeAsync(id, DateTime.UtcNow.AddMinutes(30));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be($"Disruption {id} could not be found on the database.");
    }

    [Fact]
    public async Task DisruptionRepository_AddDisruptionSeverityAsync_Successful()
    {
        var id = Guid.NewGuid();
        var disruptionId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        var severity = Severity.Minor;

        var result = await _disruptionRepository
            .AddDisruptionSeverityAsync(id, disruptionId, startTime, severity);

        result.IsSuccess.Should().BeTrue();

        var disruptionSeverity = await _neasdenDbContext.Severitys
            .SingleOrDefaultAsync(x => x.Id == id);

        disruptionSeverity.Should().NotBeNull();
        disruptionSeverity.Id.Should().Be(id);
        disruptionSeverity.DisruptionId.Should().Be(disruptionId);
        disruptionSeverity.StartTime.Should().Be(startTime);
        disruptionSeverity.Severity.Should().Be(severity);
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

        await _neasdenDbContext.Severitys.AddAsync(severity);
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
}
