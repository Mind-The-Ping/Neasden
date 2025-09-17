using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Database;
using Neasden.Models;

namespace Neasden.Repository.Repositories;
public class DisruptionRepository
{
    private readonly NeasdenDbContext _neasdenDbContext;

    public DisruptionRepository(NeasdenDbContext neasdenDbContext)
    {
        _neasdenDbContext = neasdenDbContext;
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
        catch (Exception ex) {
            return Result.Failure("Could not save disruptions to database.");
        }
    }

    public async Task<Result<Disruption>> GetDisruptionByIdAsync(Guid id)
    {
        var result = await _neasdenDbContext.Disruptions
            .SingleOrDefaultAsync(x => x.Id == id);

        if(result == null) {
            return Result.Failure<Disruption>($"Could not find disruption {id} on the database.");
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

        if (disruptionsToUpdate.Count == 0) {
            return Result.Failure("No matching disruptions found in the database.");
        }

        foreach (var disruption in disruptionsToUpdate) {
            disruption.EndTime = endTimesDict[disruption.Id];
        }

        try
        {
            await _neasdenDbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure("Database could not save disruption end times.");
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
        catch (Exception ex) {
            return Result.Failure("Could not save disruption severitys to database.");
        }
    }

    public async Task<Result<DisruptionSeverity>> GetDisruptionSeverityByIdAsync(Guid id)
    {
        var disruptionSeverity = await _neasdenDbContext.Severities
           .SingleOrDefaultAsync(x => x.Id == id);

        if (disruptionSeverity == null) {
            return Result.Failure<DisruptionSeverity>($"Disruption severity {id} could not be found on the database.");
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
            return Result.Failure("Could not save descriptions to database.");
        }
    }

    public async Task<Result<DisruptionDescription>> GetDisruptionDescriptionByIdAsync(Guid id)
    {
        var result = await _neasdenDbContext.Descriptions
            .SingleOrDefaultAsync(x => x.Id == id);

        if (result is null)
        {
            return Result.Failure<DisruptionDescription>($"Could not find description {id} on the database.");
        }

        return Result.Success(result);
    }
}
