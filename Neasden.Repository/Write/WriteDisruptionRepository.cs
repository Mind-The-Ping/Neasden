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

     
        if (await context.Disruptions.AnyAsync(x => x.Id == disruption.Id)) {
            return Result.Success();
        }

        try
        {
            await context.Disruptions.AddAsync(disruption);
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex) when (IsUniqueViolation(ex)) {
            return Result.Success();
        }
        catch (Exception ex) {
            return LogAndReturnFailure(ex, $"disruption {disruption.Id}");
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
        catch (Exception ex) {
            return LogAndReturnFailure(ex, $"disruption end time for {disruption.Id}");
        }
    }

    public async Task<Result> AddDisruptionSeverityAsync(DisruptionSeverity disruptionSeverity)
    {
        await using var context = _contextFactory.CreateDbContext();

        if (await context.Severities.AnyAsync(x => x.Id == disruptionSeverity.Id)) {
            return Result.Success();
        }

        try
        {
            await context.Severities.AddAsync(disruptionSeverity);
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex) when (IsUniqueViolation(ex)) {
            return Result.Success();
        }
        catch (Exception ex) {
            return LogAndReturnFailure(ex, $"disruption severity {disruptionSeverity.Id}");
        }
    }

    public async Task<Result> AddDescriptionAsync(DisruptionDescription description)
    {
        await using var context = _contextFactory.CreateDbContext();

        if (await context.Descriptions.AnyAsync(x => x.Id == description.Id)) {
            return Result.Success();
        }

        description.CreatedAt = DateTime.UtcNow;

        try
        {
            context.Descriptions.Add(description);
            await context.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            if (IsDuplicateKeyViolation(ex)) {
                return Result.Success();
            }

            return LogAndReturnFailure(ex, $"disruption description {description.Id}");
        }
    }

    private static bool IsDuplicateKeyViolation(Exception ex)
    {
        while (ex != null)
        {
            if (ex is PostgresException pg && pg.SqlState == "23505") {
                return true;
            }
            ex = ex.InnerException;
        }
        return false;
    }

    private Result LogAndReturnFailure(Exception ex, string entityInfo)
    {
        var message = $"Could not save {entityInfo}. error: {ex.Message}";
        _logger.LogError(ex, message);
        return Result.Failure(message);
    }
}
