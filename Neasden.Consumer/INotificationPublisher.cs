using Neasden.Models;

namespace Neasden.Consumer;
public interface INotificationPublisher
{
    Task PublishAsync(IEnumerable<Notification> notifications);
    Task PublishResolvedAsync(IEnumerable<User> notifiedUsers);
}
