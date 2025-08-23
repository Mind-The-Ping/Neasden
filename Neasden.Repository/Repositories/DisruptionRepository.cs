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
        DateTime dateTime)
    {
        if(string.IsNullOrWhiteSpace(description)) {
            return Result.Failure($"Description is empty for disruption {id}");
        }

        var disruption = new Disruption
        {
            Id = id,
            LineId = lineId,
            StartStationId = startStationId,
            EndStationId = endStationId,
            Description = description,
            DateTime = dateTime
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
}
