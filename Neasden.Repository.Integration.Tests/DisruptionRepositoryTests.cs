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
             .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdonUser;Password=password12345")
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
        var dateTime = DateTime.UtcNow;

        var result = await _disruptionRepository.AddDisruptionAsync(
                                id,
                                lineId,
                                startStationId,
                                endStationId,
                                description,
                                dateTime);

        
        result.IsSuccess.Should().BeTrue();

        var disruption = _neasdenDbContext.Disruptions.SingleOrDefault(x => x.Id == id);
        disruption.Should().NotBeNull();

        disruption.Id.Should().Be(id);
        disruption.LineId.Should().Be(lineId);
        disruption.StartStationId.Should().Be(startStationId);
        disruption.EndStationId.Should().Be(endStationId);
        disruption.Description.Should().Be(description);
        disruption.DateTime.Should().Be(dateTime);
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
        result.Error.Should().Be($"Description is empty for disruption {id}");
    }

    [Fact]
    public async Task DisruptionRepository_GetDisruptionAsync_Successful()
    {
        var disruption = new Disruption
        {
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            Description = "Something not human clambered out of the bottom of the Northen line, please stay away.",
            DateTime = DateTime.UtcNow
        };

        await _neasdenDbContext.Disruptions.AddAsync(disruption);
        await _neasdenDbContext.SaveChangesAsync();

        var result = await _disruptionRepository.GetDisruptionAsync(disruption.Id);
        result.IsSuccess.Should().BeTrue();

        result.Value.Should().BeEquivalentTo(disruption);
    }

    [Fact]
    public async Task DisruptionRepository_GetDisruptionAsync_No_Matching_User_Fails()
    {
        var id = Guid.NewGuid();
       
        var result = await _disruptionRepository.GetDisruptionAsync(id);
        result.IsFailure.Should().BeTrue();

        result.Error.Should().Be($"Could not find disruption {id} on the database.");
    }
}
