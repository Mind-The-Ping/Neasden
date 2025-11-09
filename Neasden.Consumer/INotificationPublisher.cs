using Neasden.Models;

namespace Neasden.Consumer;
public interface INotificationPublisher
{
    Task PublishAsync(IEnumerable<User> notifications);
    Task PublishResolvedAsync(IEnumerable<User> notifiedUsers);
}
