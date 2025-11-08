using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;
using Neasden.Repository.Write;
using NSubstitute;

namespace Neasden.Repository.Integration.Tests.Write;

public class WriteDisruptionRepositoryTests
{
    private readonly IDbContextFactory<WriteDbContext> _contextFactory;
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private readonly WriteDisruptionRepository _repository;

    public WriteDisruptionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WriteDbContext>()
             .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
             .Options;

        _contextFactory = new TestWriteDbContextFactory(options);


        using (var context = _contextFactory.CreateDbContext())
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        var logger = Substitute.For<ILogger<WriteDisruptionRepository>>();

        _repository = new WriteDisruptionRepository(logger, _contextFactory);
    }

    [Fact]
    public async Task WriteDisruptionRepository_AddDisruptionAsync_Successful()
    {
        var disruption = new Disruption
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };

        var result = await _repository
            .AddDisruptionAsync(disruption);

        await using var verifyContext = _contextFactory.CreateDbContext();
        var disruptionDb = await verifyContext.Disruptions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == disruption.Id);

        disruptionDb.Should().NotBeNull();
        disruptionDb.Id.Should().Be(disruption.Id);
        disruptionDb.LineId.Should().Be(disruption.LineId);
        disruptionDb.StartStationId.Should().Be(disruption.StartStationId);
        disruptionDb.EndStationId.Should().Be(disruption.EndStationId);
        disruptionDb.StartTime.Should().BeCloseTo(disruption.StartTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WriteDisruptionRepository_AddDisruptionAsync_Already_Added_Successful()
    {
        var disruption = new Disruption
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow
        };

        await using (var setupContext = _contextFactory.CreateDbContext())
        {
            await setupContext.Disruptions.AddAsync(disruption);
            await setupContext.SaveChangesAsync();
        }

        var result = await _repository
            .AddDisruptionAsync(disruption);

        result.IsSuccess.Should().BeTrue();
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

        await using (var setupContext = _contextFactory.CreateDbContext())
        {
            await setupContext.Disruptions.AddAsync(disruption);
            await setupContext.SaveChangesAsync();
        }

        var disruptionEnd = new DisruptionEnd(disruption.Id, DateTime.UtcNow);
        var result = await _repository.AddDisruptionEndTimeAsync(disruptionEnd);

        result.IsSuccess.Should().BeTrue();

        await using var verifyContext = _contextFactory.CreateDbContext();
        var disruptionDb = await verifyContext.Disruptions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == disruption.Id);

        disruptionDb.Should().NotBeNull();
        disruptionDb.Id.Should().Be(disruptionEnd.Id);
        disruptionDb.EndTime.Should().BeCloseTo(disruptionEnd.EndTime, TimeSpan.FromSeconds(1));
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
            .AddDisruptionSeverityAsync(disruptionSeverity);

        result.IsSuccess.Should().BeTrue();

        await using var context = _contextFactory.CreateDbContext();

        var disruptionSeveritySaved = await context.Severities
            .SingleOrDefaultAsync(x => x.Id == disruptionSeverity.Id);

        disruptionSeveritySaved.Should().NotBeNull();
        disruptionSeveritySaved.Id.Should().Be(disruptionSeverity.Id);
        disruptionSeveritySaved.StartTime.Should().BeCloseTo(disruptionSeverity.StartTime, TimeSpan.FromSeconds(1));
        disruptionSeveritySaved.Severity.Should().Be(disruptionSeverity.Severity);
    }

    [Fact]
    public async Task WriteDisruptionRepository_AddDisruptionSeverityAsync_Already_Added_Successful()
    {
        var disruptionSeverity = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Minor
        };

        await using (var setupContext = _contextFactory.CreateDbContext())
        {
            await setupContext.Severities.AddAsync(disruptionSeverity);
            await setupContext.SaveChangesAsync();
        }

        var result = await _repository
            .AddDisruptionSeverityAsync(disruptionSeverity);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task WriteDisruptionRepository_AddDescriptionAsync_Successful()
    {
        var description = new DisruptionDescription
        {
            Id = Guid.NewGuid(),
            Description = "This is a test.",
            CreatedAt = DateTime.UtcNow
        };

        var result = await _repository.AddDescriptionAsync(description);
        result.IsSuccess.Should().BeTrue();

        await using var context = _contextFactory.CreateDbContext();

        var record = await context.Descriptions.SingleOrDefaultAsync(d => d.Id == description.Id);
        record.Should().NotBeNull();
        record.Id.Should().Be(description.Id);
        record.Description.Should().Be(description.Description);
        record.CreatedAt.Should().BeCloseTo(description.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WriteDisruptionRepository_AddDescriptionAsync_Already_Added_Successful()
    {
        var description = new DisruptionDescription
        {
            Id = Guid.NewGuid(),
            Description = "This is a test.",
            CreatedAt = DateTime.UtcNow
        };

        await using (var setupContext = _contextFactory.CreateDbContext())
        {
            await setupContext.Descriptions.AddAsync(description);
            await setupContext.SaveChangesAsync();
        }

        var result = await _repository
            .AddDescriptionAsync(description);

        result.IsSuccess.Should().BeTrue();
    }
}
