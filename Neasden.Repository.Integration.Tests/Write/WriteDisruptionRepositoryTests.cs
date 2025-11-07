using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;
using Neasden.Repository.Write;
using NSubstitute;

namespace Neasden.Repository.Integration.Tests.Write;

public class WriteDisruptionRepositoryTests
{
    private readonly WriteDbContext _context;
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private readonly WriteDisruptionRepository _repository;

    public WriteDisruptionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WriteDbContext>()
             .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
             .Options;

        _context = new WriteDbContext(options);

        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        var logger = Substitute.For<ILogger<WriteDisruptionRepository>>();

        _repository = new WriteDisruptionRepository(_context, logger);
    }

    [Fact]
    public async Task WriteDisruptionRepository_AddDisruptionEndTimeAsync_Successful()
    {
        var disruption = new Disruption
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };

        await _context.Disruptions.AddAsync(disruption);
        await _context.SaveChangesAsync();

        var disruptionEnd = new DisruptionEnd(disruption.Id, DateTime.UtcNow);
        var result = await _repository.AddDisruptionEndTimesAsync([disruptionEnd]);

        result.IsSuccess.Should().BeTrue();

        var disruptionDb = await _context.Disruptions
            .SingleOrDefaultAsync(x => x.Id == disruption.Id);

        disruptionDb.Should().NotBeNull();
        disruptionDb.Id.Should().Be(disruptionEnd.Id);
        disruptionDb.EndTime.Should().Be(disruptionEnd.EndTime);
    }

    [Fact]
    public async Task WriteDisruptionRepository_AddDisruptionSeverityAsync_Successful()
    {
        var disruptionSeverity = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Minor
        };

        var result = await _repository
            .AddDisruptionSeveritiesAsync([disruptionSeverity]);

        result.IsSuccess.Should().BeTrue();

        var disruptionSeveritySaved = await _context.Severities
            .SingleOrDefaultAsync(x => x.Id == disruptionSeverity.Id);

        disruptionSeveritySaved.Should().NotBeNull();
        disruptionSeveritySaved.Should().BeEquivalentTo(disruptionSeverity);
    }

    [Fact]
    public async Task WriteDisruptionRepository_AddDescriptionsAsync_Successful()
    {
        var description = new DisruptionDescription
        {
            Id = Guid.NewGuid(),
            Description = "This is a test.",
            CreatedAt = DateTime.UtcNow
        };

        var result = await _repository.AddDescriptionsAsync([description]);
        result.IsSuccess.Should().BeTrue();

        var record = await _context.Descriptions.SingleOrDefaultAsync(d => d.Id == description.Id);
        record.Should().NotBeNull();
        record.Should().BeEquivalentTo(description);
    }

    [Fact]
    public async Task WriteDisruptionRepository_AddDescriptionsAsync_Only_New_Successful()
    {
        var description1 = new DisruptionDescription
        {
            Id = Guid.NewGuid(),
            Description = "This is a test.",
            CreatedAt = DateTime.UtcNow
        };

        await _context.Descriptions.AddAsync(description1);
        await _context.SaveChangesAsync();

        var description2 = new DisruptionDescription
        {
            Id = Guid.NewGuid(),
            Description = "This is a test 2.",
            CreatedAt = DateTime.UtcNow
        };

        var result = await _repository.AddDescriptionsAsync([description1, description2]);
        result.IsSuccess.Should().BeTrue();

        var records = _context.Descriptions.ToList();
        records.Count.Should().Be(2);
        records.Should().BeEquivalentTo([description1, description2]);
    }
}
