using CSharpFunctionalExtensions;
using Neasden.Repository.Redis;
using Neasden.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Neasden.Consumer.Repositories;
public class NotificationConsumerRepo
{
    private readonly NotificationRepository _notificationRepository;
    private readonly ILogger<NotificationConsumerRepo> _logger;

    public NotificationConsumerRepo(
        NotificationRepository notificationRepository,
        ILogger<NotificationConsumerRepo> logger)
    {
        _notificationRepository = notificationRepository ??
            throw new ArgumentNullException(nameof(notificationRepository));

        _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> AddNotificationAsync(BinaryData body)
    {
        Notification? message;

        try
        {
            var json = body.ToArray();
            message = JsonSerializer.Deserialize<Notification>(json);
        }
        catch 
        {
            var errorMessage = "Notification message could not be deserialized.";

            _logger.LogError(errorMessage);
            return Result.Failure(errorMessage);
        }

        var result = await _notificationRepository.SaveNotificationAsync(message!);
        return result;
    }
}
