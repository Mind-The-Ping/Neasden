using CSharpFunctionalExtensions;
using Neasden.API.Model;

namespace Neasden.API.Client;

public interface IWaterlooClient
{
    public Task<Result<Line>> GetLineById(
        Guid id, 
        CancellationToken cancellationToken = default);

    public Task<Result<Station>> GetStationById(
        Guid id, 
        CancellationToken cancellationToken = default);
}
