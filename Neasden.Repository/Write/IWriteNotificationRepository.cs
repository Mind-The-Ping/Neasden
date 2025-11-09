using CSharpFunctionalExtensions;
using Neasden.Models;

namespace Neasden.Repository.Write;
public interface IWriteNotificationRepository
{
    Task<Result> AddNotificationsAsync(IEnumerable<Notification> notifications);
}
