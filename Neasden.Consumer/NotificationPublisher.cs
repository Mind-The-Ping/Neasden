using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neasden.Library.Options;
using Neasden.Models;

namespace Neasden.Consumer;
public class NotificationPublisher : INotificationPublisher
{
    private readonly ServiceBusSender _notificationSender;
    private readonly ServiceBusSender _resolvedNotificationSender;
    private readonly ILogger<NotificationPublisher> _logger;
    private readonly TimeSpan _messageDelay;

    public NotificationPublisher(
          ILogger<NotificationPublisher> logger,
          IOptions<ServiceBusOptions> serviceBusOptions,
          IOptions<NotificationOptions> notificationOptions,
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

        _notificationSender = CreateSender(options.Notifications, "ServiceBus:Queues:Notifications");
        _resolvedNotificationSender = CreateSender(options.ResolvedNotifications, "ServiceBus:Queues:ResolvedNotifications");

        var notifyOptions = notificationOptions.Value ?? throw new ArgumentNullException(nameof(notificationOptions));
        _messageDelay = TimeSpan.FromSeconds(notifyOptions.DelayTime);
    }

    public async Task PublishAsync(IEnumerable<User> users)
    {
        foreach (var user in users)
        {
            var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(user))
            {
                ScheduledEnqueueTime = DateTimeOffset.UtcNow.Add(_messageDelay)
            };

            try {
                await _notificationSender.SendMessageAsync(message);
            }
            catch (Exception ex) {
                _logger.LogError($"Could not send notification message {user.Id}.", ex);
            }
        }
    }

    public async Task PublishResolvedAsync(IEnumerable<User> notifiedUsers)
    {
        foreach (var user in notifiedUsers)
        {
            var message = new ServiceBusMessage(BinaryData.FromObjectAsJson(user))
            {
                ScheduledEnqueueTime = DateTimeOffset.UtcNow.Add(_messageDelay)
            };

            try {
                await _resolvedNotificationSender.SendMessageAsync(message);
            }
            catch (Exception ex) {
                _logger.LogError($"Could not send resolved notification message for user {user.Id}.", ex);
            }
        }
    }
}
