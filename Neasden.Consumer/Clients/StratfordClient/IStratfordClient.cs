using CSharpFunctionalExtensions;
using Neasden.Consumer.Models;

namespace Neasden.Consumer.Clients.StratfordClient;
public interface IStratfordClient
{
    public Task<Result<IEnumerable<UserDetails>>> GetUserDetailsAsync(IEnumerable<Guid> ids);
}
