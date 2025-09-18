using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
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
    private readonly DisruptionConsumerRepo _disruptionRepo;
    private readonly NotificationConsumerRepo _notificationRepo;

    public Consumer(
        ILogger<Consumer> logger,
        DisruptionConsumerRepo disruptionRepo,
        NotificationConsumerRepo notificationRepo)
    {
        _logger = logger;
        _disruptionRepo = disruptionRepo;
        _notificationRepo = notificationRepo;
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

        await messageActions.CompleteMessageAsync(message);
    }

    [Function("DisruptionDescriptionConsumer")]
    public async Task DisruptionDescriptionHandler(
      [ServiceBusTrigger("%QueueDisruptionDescriptions%", Connection = "ServiceBusConnection")]
      ServiceBusReceivedMessage message,
      ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        await _disruptionRepo.AddDisruptionDescriptionAsync(message.Body);

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

        await messageActions.CompleteMessageAsync(message);
    }
}