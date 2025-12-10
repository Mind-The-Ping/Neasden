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
       AffectedJourney affectedJourney,
       CancellationToken cancellationToken = default);
}
