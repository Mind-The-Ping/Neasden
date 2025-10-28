using CSharpFunctionalExtensions;
using Neasden.API.Model;

namespace Neasden.API.Client;

public interface IWaterlooClient
{
    public Task<Result<IEnumerable<Line>>> GetLinesById(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default);

    public Task<Result<IEnumerable<Station>>> GetStationsById(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default);
}
