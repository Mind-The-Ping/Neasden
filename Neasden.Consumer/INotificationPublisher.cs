using Neasden.Models;

namespace Neasden.Consumer;
public interface INotificationPublisher
{
    Task PublishAsync(IEnumerable<Journey> notifications);
    Task PublishResolvedAsync(IEnumerable<Journey> notifiedUsers);
}
