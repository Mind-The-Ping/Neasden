using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neasden.Consumer.Options;
using Neasden.Consumer.Repositories;

namespace Neasden.Consumer;

public enum SaveType
{
    Disruption,
    DisruptionSeverity,
    DisruptionEnd,
    Notification
}

public class Consumer
{
    private readonly ILogger<Consumer> _logger;
    private readonly ServiceBusSender _saveSender;
    private readonly DisruptionConsumerRepo _disruptionRepo;
    private readonly NotificationConsumerRepo _notificationRepo;

    public Consumer(
        ILogger<Consumer> logger,
        ServiceBusClient serviceBusClient,
        IOptions<ServiceBusOptions> options,
        DisruptionConsumerRepo disruptionRepo,
        NotificationConsumerRepo notificationRepo)
    {
        _logger = logger;
        _disruptionRepo = disruptionRepo;
        _notificationRepo = notificationRepo;
        _saveSender = serviceBusClient.CreateSender(options.Value.SaveNeasden);
    }

    [Function("DisruptionsConsumer")]
    public async Task DisruptionHandler(
        [ServiceBusTrigger("%QueueDisruptions%", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
         ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        await _disruptionRepo.AddDisruptionAsync(message.Body);

        await _saveSender.SendMessageAsync(new ServiceBusMessage(SaveType.Disruption.ToString()));

        await messageActions.CompleteMessageAsync(message);
    }

    [Function("DisruptionsSeverityConsumer")]
    public async Task DisruptionSeverityHandler(
        [ServiceBusTrigger("%QueueDisruptionsSeverity%", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        await _disruptionRepo.UpdateDisruptionSeverityAsync(message.Body);

        await _saveSender.SendMessageAsync(new ServiceBusMessage(SaveType.DisruptionSeverity.ToString()));

        await messageActions.CompleteMessageAsync(message);
    }

    [Function("DisruptionsEndConsumer")]
    public async Task DisruptionEndsHandler(
      [ServiceBusTrigger("%TopicsDisruptionEndsName%", "%TopicsDisruptionEndsSubscription%", Connection = "ServiceBusConnection")]
      ServiceBusReceivedMessage message,
      ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        await _disruptionRepo.AddDisruptionEndTimeAsync(message.Body);

        await _saveSender.SendMessageAsync(new ServiceBusMessage(SaveType.DisruptionEnd.ToString()));

        await messageActions.CompleteMessageAsync(message);
    }

    [Function("NotificationConsumer")]
    public async Task NotificationHandler(
      [ServiceBusTrigger("%QueueNotifications%", Connection = "ServiceBusConnection")]
      ServiceBusReceivedMessage message,
      ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        await _notificationRepo.AddNotificationAsync(message.Body);

        await _saveSender.SendMessageAsync(new ServiceBusMessage(SaveType.Notification.ToString()));

        await messageActions.CompleteMessageAsync(message);
    }
}