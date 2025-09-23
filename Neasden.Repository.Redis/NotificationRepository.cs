using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Neasden.Models;
using StackExchange.Redis;
using System.Text.Json;
using Neasden.Repository.Redis.Options;
using Microsoft.Extensions.Logging;

namespace Neasden.Repository.Redis;
public class NotificationRepository
{
    private readonly IDatabase _database;
    private readonly string _notificationKey;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(
        IOptions<RedisOptions> options,
        ConnectionMultiplexer redis,
        ILogger<NotificationRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(redis, nameof(redis));

        _database = redis.GetDatabase();
        _notificationKey = options.Value.NotificationKey;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> SaveNotificationAsync(Notification notification)
    {
        var json = JsonSerializer.Serialize(notification);

        if (string.IsNullOrWhiteSpace(json)) 
        {
            var message = $"Could not serialize notification {notification.Id}.";
            return Result.Failure(message);
        }

        var result = await _database.ListRightPushAsync(_notificationKey, json);

        if(result <= 0)
        {
            var message = $"Could not save notification {notification.Id} to Redis.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        return Result.Success();
    }

    public async Task<Result<IEnumerable<Notification>>> GetNotificationsAsync()
    {
        var values = await _database.ListRangeAsync(_notificationKey, 0, -1);

        if (values.Length == 0) 
        {
            var message = "No notifications found in Redis.";

            _logger.LogError(message);
            return Result.Failure<IEnumerable<Notification>>(message);
        }

        var notifications = values
        .Select(v => JsonSerializer.Deserialize<Notification>(v!))
        .Where(d => d != null)
        .ToList()!;

        return Result.Success<IEnumerable<Notification>>(notifications!);
    }

    public async Task<Result> DeleteNotificationsAsync()
    {
        var deleted = await _database.KeyDeleteAsync(_notificationKey);

        if(!deleted)
        {
            var message = "No notifications found to delete.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        return Result.Success();
    }

    public async Task<long> GetNotificationCountAsync() =>
        await _database.ListLengthAsync(_notificationKey);
}
