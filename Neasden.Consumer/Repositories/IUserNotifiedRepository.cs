using CSharpFunctionalExtensions;
using Neasden.Models;

namespace Neasden.Consumer.Repositories;
public interface IUserNotifiedRepository
{
    Task<Result> SaveUsersAsync(IEnumerable<User> users);
    Task<IEnumerable<User>> GetUsersByDisruptionIdAsync(Guid disruptionId);
    Task DeleteByDisruptionIdAsync(Guid disruptionId);
    Task DeleteUsersAsync(Guid disruptionId, IEnumerable<User> users);
}
