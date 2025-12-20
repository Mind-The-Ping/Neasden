using CSharpFunctionalExtensions;
using Neasden.Models;

namespace Neasden.Repository.NotificationCount;

public interface INotificationCountRepository
{
    Task<int> GetUserNotificationCountAsync(Guid userId);
    Task<Result> AddToCountAsync(UnReadNotification unReadNotification);
    Task<Result> RemoveFromCountAsync(Guid notificationId);
}
