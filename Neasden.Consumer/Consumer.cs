using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Neasden.Consumer.Dto;
using Neasden.Models;
using Neasden.Repository.Write;
using System.Text.Json;

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
    private readonly DisruptionNotifier _notifer;
    private readonly WriteDisruptionRepository _writeDisruptionRepository;
    private readonly IWriteNotificationRepository _writeNotificationRepository;

    public Consumer(
        ILogger<Consumer> logger,
        DisruptionNotifier notifer,
        WriteDisruptionRepository writeDisruptionRepository,
        IWriteNotificationRepository writeNotificationRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notifer = notifer ?? throw new ArgumentNullException( nameof(notifer));

        _writeDisruptionRepository = writeDisruptionRepository ?? 
            throw new ArgumentNullException(nameof(writeDisruptionRepository));

        _writeNotificationRepository = writeNotificationRepository ?? 
            throw new ArgumentNullException(nameof(writeNotificationRepository));
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


        try
        {
            var json = message.Body.ToArray();
            var disruptionDto = JsonSerializer.Deserialize<DisruptionDto>(json);
            var disruption = new Disruption()
            {
                Id = disruptionDto!.Id,
                LineId = disruptionDto.Line.Id,
                StartStationId = disruptionDto.StartStationId,
                EndStationId = disruptionDto.EndStationId,
                StartTime = disruptionDto.StartTime
            };

            await _writeDisruptionRepository.AddDisruptionAsync(disruption);
            await _notifer.NotifyDisruptionAsync(disruptionDto!);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Could not deserialize disruption.");
        }

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

        try
        {
            var json = message.Body.ToArray();
            var disruptionSeverity = JsonSerializer.Deserialize<DisruptionSeverity>(json);
            await _writeDisruptionRepository.AddDisruptionSeverityAsync(disruptionSeverity!);
        }
        catch(Exception ex) {
            _logger.LogError(ex, "Could not deserialize disruption severity.");
        }

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

        try
        {
            var json = message.Body.ToArray();
            var disruptionEnd = JsonSerializer.Deserialize<DisruptionEnd>(json);
            await _writeDisruptionRepository.AddDisruptionEndTimeAsync(disruptionEnd!);
            await _notifer.NotifyDisruptionEndAsync(disruptionEnd!);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Could not deserialize disruption end.");
        }

        await messageActions.CompleteMessageAsync(message);
    }

    [Function("DeletedUserConsumer")]
    public async Task DeletedUserHandler(
     [ServiceBusTrigger("%TopicDeletedUser%", "%TopicDeletedUserSubscription%", Connection = "ServiceBusConnection")]
      ServiceBusReceivedMessage message,
     ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        try
        {
            var json = message.Body.ToArray();
            var userDeleted = JsonSerializer.Deserialize<UserDeleted>(json);

            var result = await _writeNotificationRepository.RemoveNotificationsByUserIdAsync(
                userDeleted!.UserId, 
                userDeleted.DeletedAt);

            if (result.IsFailure) {
                _logger.LogError(result.Error);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Could not deserialize deleted user.");
        }

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

        try
        {
            var json = message.Body.ToArray();
            var disruptionDescription = JsonSerializer.Deserialize<DisruptionDescription>(json);
            await _writeDisruptionRepository.AddDescriptionAsync(disruptionDescription!);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Could not deserialize disruption end.");
        }

        await messageActions.CompleteMessageAsync(message);
    }
}