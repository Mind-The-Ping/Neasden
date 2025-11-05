using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;

namespace Neasden.Repository.Read;
public class ReadDisruptionRepository
{
    private readonly ILogger<ReadDisruptionRepository> _logger;
    private readonly IDbContextFactory<ReadDbContext> _contextFactory;

    public ReadDisruptionRepository(
        IDbContextFactory<ReadDbContext> contextFactory,
        ILogger<ReadDisruptionRepository> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Disruption>> GetDisruptionByIdAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        var result = await context.Disruptions
            .SingleOrDefaultAsync(x => x.Id == id);

        if (result == null)
        {
            var message = $"Disruption {id} does not exist on the database.";

            _logger.LogError(message);
            return Result.Failure<Disruption>(message);
        }

        return Result.Success(result);
    }

    public async Task<Result<DisruptionSeverity>> GetDisruptionSeverityByIdAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        var disruptionSeverity = await context.Severities
           .SingleOrDefaultAsync(x => x.Id == id);

        if (disruptionSeverity == null)
        {
            var message = $"Disruption severity {id} does not exist on this database.";

            _logger.LogError(message);
            return Result.Failure<DisruptionSeverity>(message);
        }

        return Result.Success(disruptionSeverity);
    }

    public async Task<Result<DisruptionDescription>> GetDisruptionDescriptionByIdAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        var result = await context.Descriptions
            .SingleOrDefaultAsync(x => x.Id == id);

        if (result is null)
        {
            var message = $"Disruption description {id} does not exist on this database.";

            _logger.LogError(message);
            return Result.Failure<DisruptionDescription>(message);
        }

        return Result.Success(result);
    }
}
