using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;
using Neasden.Repository.Write;
using NSubstitute;

namespace Neasden.Repository.Integration.Tests.Write;

public class WriteDisruptionSeverityHistoryTests
{
    private readonly IDbContextFactory<WriteDbContext> _contextFactory;
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private readonly WriteDisruptionSeverityHistory _repository;

    public WriteDisruptionSeverityHistoryTests()
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

        var logger = Substitute.For<ILogger<WriteDisruptionSeverityHistory>>();

        _repository = new WriteDisruptionSeverityHistory(logger, _contextFactory);
    }

    [Fact]
    public async Task WriteDisruptionSeverityHistory_AddDisruptionSeverityHistoryAsync_Successful()
    {
        var severityHistory = new DisruptionSeverityHistory
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            CurrentSeverity = Severity.Severe,
            PreviousSeverity = Severity.Minor,
            CreatedAt = DateTime.UtcNow,
        };

        var result = await _repository
            .AddDisruptionSeverityHistoryAsync(severityHistory);

        await using var verifyContext = _contextFactory.CreateDbContext();
        var severityHistoryDb = await verifyContext.SeverityHistories
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == severityHistory.Id);

        severityHistoryDb.Should().NotBeNull();
        severityHistoryDb.Id.Should().Be(severityHistory.Id);
        severityHistoryDb.DisruptionId.Should().Be(severityHistory.DisruptionId);
        severityHistoryDb.CurrentSeverity.Should().Be(severityHistory.CurrentSeverity);
        severityHistoryDb.PreviousSeverity.Should().Be(severityHistory.PreviousSeverity);
        severityHistoryDb.CreatedAt.Should().BeCloseTo(severityHistory.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task WriteDisruptionSeverityHistory_AddDisruptionSeverityHistoryAsync_Already_Added_Successful()
    {
        var severityHistory = new DisruptionSeverityHistory
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            CurrentSeverity = Severity.Severe,
            PreviousSeverity = Severity.Minor,
            CreatedAt = DateTime.UtcNow,
        };

        await using var setupContext = _contextFactory.CreateDbContext();
        await setupContext.SeverityHistories.AddAsync(severityHistory);
        await setupContext.SaveChangesAsync();

        var result = await _repository
           .AddDisruptionSeverityHistoryAsync(severityHistory);

        result.IsSuccess.Should().BeTrue();
    }
}
