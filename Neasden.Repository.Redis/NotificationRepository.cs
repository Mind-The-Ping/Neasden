using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Neasden.Repository.Options;
using Neasden.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Neasden.Repository.Redis;
public class NotificationRepository
{
    private readonly IDatabase _database;
    private readonly string _notificationKey;

    public NotificationRepository(IOptions<RedisOptions> options)
    {
        var redisOptions = options.Value ??
         throw new ArgumentNullException(nameof(options));

        var redis = ConnectionMultiplexer.Connect(redisOptions.ConnectionString);

        _database = redis.GetDatabase();
        _notificationKey = options.Value.NotificationKey;
    }

    public async Task<Result> SaveNotificationAsync(Notification notification)
    {
        var json = JsonSerializer.Serialize(notification);

        if (string.IsNullOrWhiteSpace(json)) {
            return Result.Failure($"Could not serialize notification {notification.Id}.");
        }

        var result = await _database.ListRightPushAsync(_notificationKey, json);

        return result > 0
            ? Result.Success()
            : Result.Failure($"Could not save notification {notification.Id} to Redis.");
    }

    public async Task<Result<IEnumerable<Notification>>> GetNotificationsAsync()
    {
        var values = await _database.ListRangeAsync(_notificationKey, 0, -1);

        if (values.Length == 0) {
            return Result.Failure<IEnumerable<Notification>>("No notifications found in Redis.");
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

        return deleted
           ? Result.Success()
           : Result.Failure("No notifications found to delete.");
    }

    public async Task<long> GetNotificationCountAsync() =>
        await _database.ListLengthAsync(_notificationKey);
}
