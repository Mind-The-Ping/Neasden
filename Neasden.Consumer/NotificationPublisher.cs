using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neasden.Library.Options;
using Neasden.Models;

namespace Neasden.Consumer;
public class NotificationPublisher
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<NotificationPublisher> _logger;

    public NotificationPublisher(
          ILogger<NotificationPublisher> logger,
          IOptions<ServiceBusOptions> serviceBusOptions,
          ServiceBusClient serviceBusClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var options = serviceBusOptions.Value ?? throw new ArgumentNullException(nameof(serviceBusOptions));

        ServiceBusSender CreateSender(string name, string propertyPath)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException($"ServiceBus entity '{propertyPath}' is not configured.");
            return serviceBusClient.CreateSender(name);
        }

        _sender = CreateSender(options.Notifications, "ServiceBus:Queues:Notifications");
    }

    public async Task PublishAsync(IEnumerable<Notification> notifications)
    {
        foreach (var notification in notifications)
        {
            var message = BinaryData.FromObjectAsJson(notification);

            try {
                await _sender.SendMessageAsync(new ServiceBusMessage(message));
            }
            catch (Exception ex) {
                _logger.LogError($"Could not send notification message {notification.Id}.", ex);
            }
        }
    }
}
