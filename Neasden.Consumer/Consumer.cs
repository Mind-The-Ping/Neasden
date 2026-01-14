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
    private readonly WriteDisruptionSeverityHistory _writeDisruptionSeverityHistory;
    private readonly IWriteNotificationRepository _writeNotificationRepository;

    public Consumer(
        ILogger<Consumer> logger,
        DisruptionNotifier notifer,
        WriteDisruptionRepository writeDisruptionRepository,
        IWriteNotificationRepository writeNotificationRepository,
        WriteDisruptionSeverityHistory writeDisruptionSeverityHistory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notifer = notifer ?? throw new ArgumentNullException( nameof(notifer));

        _writeDisruptionRepository = writeDisruptionRepository ?? 
            throw new ArgumentNullException(nameof(writeDisruptionRepository));

        _writeNotificationRepository = writeNotificationRepository ?? 
            throw new ArgumentNullException(nameof(writeNotificationRepository));

        _writeDisruptionSeverityHistory = writeDisruptionSeverityHistory ??
            throw new ArgumentNullException(nameof(writeDisruptionSeverityHistory));
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
            var lineDisruptionsDto = JsonSerializer.Deserialize<LineDisruptionsDto>(json);

            foreach (var lineDisruption in lineDisruptionsDto!.DisruptionDtos)
            {
                var disruption = new Disruption()
                {
                    Id = lineDisruption!.Id,
                    LineId = lineDisruption.Line.Id,
                    StartStationId = lineDisruption.StartStationId,
                    EndStationId = lineDisruption.EndStationId,
                    StartTime = lineDisruption.StartTime
                };
                await _writeDisruptionRepository.AddDisruptionAsync(disruption);
            }

            await _notifer.NotifyDisruptionAsync(lineDisruptionsDto!);
            
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


    [Function("QueueDisruptionSeverityHistoryConsumer")]
    public async Task QueueDisruptionSeverityHistoryHandler(
        [ServiceBusTrigger("%QueueDisruptionSeverityHistory%", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        try
        {
            var json = message.Body.ToArray();
            var disruptionSeverityHistory = JsonSerializer.Deserialize<DisruptionSeverityHistory>(json);
            await _writeDisruptionSeverityHistory.AddDisruptionSeverityHistoryAsync(disruptionSeverityHistory!);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Could not deserialize disruption severity history.");
        }

        await messageActions.CompleteMessageAsync(message);
    }

    [Function("DisruptionsEndConsumer")]
    public async Task DisruptionEndsHandler(
      [ServiceBusTrigger("%QueueDisruptionEnds%", Connection = "ServiceBusConnection")]
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