using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Database;
using Neasden.Repository.Models;

namespace Neasden.Repository.Repositories;
public class DisruptionRepository
{
    private readonly NeasdenDbContext _neasdenDbContext;

    public DisruptionRepository(NeasdenDbContext neasdenDbContext)
    {
        _neasdenDbContext = neasdenDbContext;
    }

    public async Task<Result> AddDisruptionAsync(
        Guid id,
        Guid lineId,
        Guid startStationId,
        Guid endStationId,
        string description,
        DateTime startTime)
    {
        if(string.IsNullOrWhiteSpace(description)) {
            return Result.Failure($"Description is empty for disruption {id}.");
        }

        var disruption = new Disruption
        {
            Id = id,
            LineId = lineId,
            StartStationId = startStationId,
            EndStationId = endStationId,
            Description = description,
            StartTime = startTime
        };

        try
        {
            await _neasdenDbContext.AddAsync(disruption);
            await _neasdenDbContext.SaveChangesAsync();
        }
        catch(Exception ex) {
            return Result.Failure($"Database could not save disruption {id}.");
        }

        return Result.Success();
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

    public async Task<Result> AddDisruptionEndTimeAsync(Guid id, DateTime endTime)
    {
        var disruption = await _neasdenDbContext.Disruptions
            .SingleOrDefaultAsync(x => x.Id == id);

        if (disruption == null) {
            return Result.Failure($"Disruption {id} could not be found on the database.");
        }

        try
        {
            disruption.EndTime = endTime;
            await _neasdenDbContext.SaveChangesAsync();
        }
        catch (Exception ex) {
            return Result.Failure($"Database could not save disruption end time for {id}.");
        }


        return Result.Success();
    }

    public async Task<Result> AddDisruptionSeverityAsync(
       Guid id,
       Guid disruptionId,
       DateTime startTime,
       Severity severity)
    {
        var disruptionSeverity = new DisruptionSeverity
        {
            Id = id,
            DisruptionId = disruptionId,
            StartTime = startTime,
            Severity = severity
        };

        try
        {
            await _neasdenDbContext.Severitys.AddAsync(disruptionSeverity);
            await _neasdenDbContext.SaveChangesAsync();
        }
        catch (Exception ex) {
            return Result.Failure($"Database could not save disruption severity {id}.");
        }

        return Result.Success();
    }

    public async Task<Result<DisruptionSeverity>> GetDisruptionSeverityByIdAsync(Guid id)
    {
        var disruptionSeverity = await _neasdenDbContext.Severitys
           .SingleOrDefaultAsync(x => x.Id == id);

        if (disruptionSeverity == null) {
            return Result.Failure<DisruptionSeverity>($"Disruption severity {id} could not be found on the database.");
        }

        return Result.Success(disruptionSeverity);
    }
}
