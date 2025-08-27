using CSharpFunctionalExtensions;
using Neasden.Repository.Models;
using Neasden.Repository.Repositories;
using System.Text.Json;

namespace Neasden.Consumer.Repositories;
public class NotificationConsumerRepo
{
    private readonly NotificationRepository _notificationRepository;

    public NotificationConsumerRepo(NotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result> AddNotificationAsync(BinaryData body)
    {
        Notification? message;

        try
        {
            var json = body.ToArray();
            message = JsonSerializer.Deserialize<Notification>(json);
        }
        catch {
            return Result.Failure("Notification message could not be deserialized.");
        }

        var result = await _notificationRepository.CreateNotificationAsync(
            message!.Id,
            message.UserId,
            message.LineId,
            message.DisruptionId,
            message.SeverityId,
            message.StartStationId,
            message.EndStationId,
            message.NotificationSentBy,
            message.SentTime);

        return result;
    }
}
