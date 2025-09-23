using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Database;
using Neasden.Models;
using Microsoft.Extensions.Logging;

namespace Neasden.Repository.Repositories;
public class DisruptionRepository
{
    private readonly NeasdenDbContext _neasdenDbContext;
    private readonly ILogger<DisruptionRepository> _logger;

    public DisruptionRepository(
        NeasdenDbContext neasdenDbContext,
        ILogger<DisruptionRepository> logger)
    {
        _neasdenDbContext = neasdenDbContext ?? 
            throw new ArgumentNullException(nameof(neasdenDbContext));

        _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> AddDisruptionsAsync(IEnumerable<Disruption> disruptions)
    {
        try
        {
            var newIds = disruptions.Select(d => d.Id).ToList();

            var existingIds = await _neasdenDbContext.Disruptions
                .Where(d => newIds.Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync();

            var newDisruptions = disruptions
                .Where(d => !existingIds.Contains(d.Id))
                .ToList();

            if(newDisruptions.Count != 0)
            {
                await _neasdenDbContext.Disruptions.AddRangeAsync(newDisruptions);
                await _neasdenDbContext.SaveChangesAsync();
            }

            return Result.Success();
        }
        catch (Exception ex) 
        {
            var message = "Could not save disruptions to database.";

            _logger.LogError(ex, message);
            return Result.Failure(message);
        }
    }

    public async Task<Result<Disruption>> GetDisruptionByIdAsync(Guid id)
    {
        var result = await _neasdenDbContext.Disruptions
            .SingleOrDefaultAsync(x => x.Id == id);

        if(result == null) 
        {
            var message = $"Disruption {id} does not exist on the database.";

            _logger.LogError(message);
            return Result.Failure<Disruption>(message);
        }

        return Result.Success(result);
    }

    public async Task<Result> AddDisruptionEndTimesAsync(IEnumerable<DisruptionEnd> disruptionEnds)
    {
        var endTimesDict = disruptionEnds.ToDictionary(d => d.Id, d => d.EndTime);
        var ids = endTimesDict.Keys.ToList();

        var disruptionsToUpdate = await _neasdenDbContext.Disruptions
              .Where(d => ids.Contains(d.Id))
              .ToListAsync();

        if (disruptionsToUpdate.Count == 0) 
        {
            var message = "No matching disruptions found in the database for disruption ends.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        foreach (var disruption in disruptionsToUpdate) {
            disruption.EndTime = endTimesDict[disruption.Id];
        }

        try
        {
            await _neasdenDbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex) 
        {
            var message = "Database could not save disruption end times.";

            _logger.LogError(ex, message);
            return Result.Failure(message);
        }
    }

    public async Task<Result> AddDisruptionSeveritiesAsync(IEnumerable<DisruptionSeverity> disruptionSeverities)
    {
        try
        {
            await _neasdenDbContext.Severities.AddRangeAsync(disruptionSeverities);
            await _neasdenDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex) 
        {
            var message = "Could not save disruption severities to database.";

            _logger.LogError(ex, message);
            return Result.Failure(message);
        }
    }

    public async Task<Result<DisruptionSeverity>> GetDisruptionSeverityByIdAsync(Guid id)
    {
        var disruptionSeverity = await _neasdenDbContext.Severities
           .SingleOrDefaultAsync(x => x.Id == id);

        if (disruptionSeverity == null) 
        {
            var message = $"Disruption severity {id} does not exist on this database.";

            _logger.LogError(message);
            return Result.Failure<DisruptionSeverity>(message);
        }

        return Result.Success(disruptionSeverity);
    }

    public async Task<Result> AddDescriptionsAsync(IEnumerable<DisruptionDescription> descriptions)
    {
        try
        {
            await _neasdenDbContext.Descriptions.AddRangeAsync(descriptions);
            await _neasdenDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            var message = "Could not save disruption descriptions to database.";

            _logger.LogError(ex, message);
            return Result.Failure(message);
        }
    }

    public async Task<Result<DisruptionDescription>> GetDisruptionDescriptionByIdAsync(Guid id)
    {
        var result = await _neasdenDbContext.Descriptions
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
