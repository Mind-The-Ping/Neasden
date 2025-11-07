using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;

namespace Neasden.Repository.Write;
public class WriteDisruptionRepository
{
    private readonly WriteDbContext _context;
    private readonly ILogger<WriteDisruptionRepository> _logger;

    public WriteDisruptionRepository(
        WriteDbContext context,
        ILogger<WriteDisruptionRepository> logger)
    {
        _context = context ??
            throw new ArgumentNullException(nameof(context));

        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> AddDisruptionsAsync(IEnumerable<Disruption> disruptions)
    {
        try
        {
            var newIds = disruptions.Select(d => d.Id).ToList();

            var existingIds = await _context.Disruptions
                .Where(d => newIds.Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync();

            var newDisruptions = disruptions
                .Where(d => !existingIds.Contains(d.Id))
                .ToList();

            if (newDisruptions.Count != 0)
            {
                await _context.Disruptions.AddRangeAsync(newDisruptions);
                await _context.SaveChangesAsync();
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

    public async Task<Result> AddDisruptionEndTimesAsync(IEnumerable<DisruptionEnd> disruptionEnds)
    {
        var endTimesDict = disruptionEnds.ToDictionary(d => d.Id, d => d.EndTime);
        var ids = endTimesDict.Keys.ToList();

        var disruptionsToUpdate = await _context.Disruptions
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
            await _context.SaveChangesAsync();
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
            await _context.Severities.AddRangeAsync(disruptionSeverities);
            await _context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            var message = "Could not save disruption severities to database.";

            _logger.LogError(ex, message);
            return Result.Failure(message);
        }
    }

    public async Task<Result> AddDescriptionsAsync(IEnumerable<DisruptionDescription> descriptions)
    {
        try
        {
            var existingIds = await _context.Descriptions
                .Where(d => descriptions.Select(x => x.Id).Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync();

            var newOnes = descriptions
                .Where(d => !existingIds.Contains(d.Id))
                .ToList();

            if (newOnes.Count > 0)
            {
                await _context.Descriptions.AddRangeAsync(newOnes);
                await _context.SaveChangesAsync();
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            var message = "Could not save disruption descriptions to database.";

            _logger.LogError(ex, message);
            return Result.Failure(message);
        }
    }
}
