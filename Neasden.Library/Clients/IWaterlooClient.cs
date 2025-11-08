using CSharpFunctionalExtensions;
using Neasden.Models;

namespace Neasden.Library.Clients;

public interface IWaterlooClient
{
    public Task<Result<IEnumerable<Line>>> GetLinesById(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default);

    public Task<Result<IEnumerable<Station>>> GetStationsById(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default);

    public Task<Result<IEnumerable<AffectedUser>>> GetAffectedUsersAsync(
       Guid line,
       Guid startStation,
       Guid endStation,
       Severity severity,
       TimeOnly time,
       DayOfWeek queryDay,
       CancellationToken cancellationToken = default);
}
