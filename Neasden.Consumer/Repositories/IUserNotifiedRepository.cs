using CSharpFunctionalExtensions;
using Neasden.Models;

namespace Neasden.Consumer.Repositories;
public interface IUserNotifiedRepository
{
    Task<Result> SaveJourneysAsync(IEnumerable<Journey> journeys);
    Task<IEnumerable<Journey>> GetJourneysByDisruptionIdAsync(Guid disruptionId);
    Task DeleteByDisruptionIdAsync(Guid disruptionId);
    Task DeleteJourneysAsync(IEnumerable<Journey> journeys);
}
