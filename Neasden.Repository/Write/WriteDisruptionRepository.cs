using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;
using Npgsql;

namespace Neasden.Repository.Write;
public class WriteDisruptionRepository
{
    private readonly ILogger<WriteDisruptionRepository> _logger;
    private readonly IDbContextFactory<WriteDbContext> _contextFactory;

    public WriteDisruptionRepository(
        ILogger<WriteDisruptionRepository> logger,
                IDbContextFactory<WriteDbContext> contextFactory)
    {
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
        _contextFactory = contextFactory ??
            throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<Result> AddDisruptionAsync(Disruption disruption)
    {
        await using var context = _contextFactory.CreateDbContext();

        if (context.Disruptions.Contains(disruption)) {
            return Result.Success();
        }

        try
        {
            await context.Disruptions.AddAsync(disruption);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex) 
        {
            var message = $"Could not save disruption {disruption.Id}. error: {ex.Message}";

            _logger.LogError(message);
            return Result.Failure(message);
        }
    }

    public async Task<Result> AddDisruptionEndTimeAsync(DisruptionEnd disruptionEnd)
    {
        await using var context = _contextFactory.CreateDbContext();

        var disruption = await context.Disruptions
           .SingleOrDefaultAsync(x => x.Id == disruptionEnd.Id);

        if (disruption == null)
        {
            var message = $"Disruption {disruptionEnd.Id} does not exist on the database to save end.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        try
        {
            disruption.EndTime = disruptionEnd.EndTime;
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            var message = $"Could not save disruption end time for {disruption.Id}. error: {ex.Message}";

            _logger.LogError(message);
            return Result.Failure(message);
        }
    }

    public async Task<Result> AddDisruptionSeverityAsync(DisruptionSeverity disruptionSeverity)
    {
        await using var context = _contextFactory.CreateDbContext();

        if (context.Severities.Contains(disruptionSeverity)) {
            return Result.Success();
        }

        try
        {
            await context.Severities.AddAsync(disruptionSeverity);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            var message = $"Could not save disruption severity {disruptionSeverity.Id}. error: {ex.Message}";

            _logger.LogError(message);
            return Result.Failure(message);
        }
    }

    public async Task<Result> AddDescriptionAsync(DisruptionDescription description)
    {
        await using var context = _contextFactory.CreateDbContext();

        description.CreatedAt = DateTime.UtcNow;

        try
        {
            context.Descriptions.Add(description);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (DbUpdateException ex)
        when (ex.InnerException is PostgresException pg &&
              pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Result.Success();
        }
        catch (Exception ex)
        {
            var message = $"Could not save disruption description {description.Id}. error: {ex.Message}";
            _logger.LogError(message);
            return Result.Failure(message);
        }
    }
}
