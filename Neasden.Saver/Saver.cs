namespace Neasden.Saver;

using CSharpFunctionalExtensions;
using PostgresDisruptionRepository = Repository.Repositories.DisruptionRepository;
using PostgresNotificationRepository = Repository.Repositories.NotificationRepository;
using RedisDisruptionRepository = Repository.Redis.DisruptionRepository;
using RedisNotificationRepository = Repository.Redis.NotificationRepository;

public class Saver
{
    private readonly RedisNotificationRepository _redisNotification;
    private readonly RedisDisruptionRepository _redisDisruption;
    private readonly PostgresNotificationRepository _postgresNotification;
    private readonly PostgresDisruptionRepository _postgresDisruption;

    public Saver(
        RedisNotificationRepository redisNotification, 
        RedisDisruptionRepository redisDisruption, 
        PostgresNotificationRepository postgresNotification, 
        PostgresDisruptionRepository postgresDisruption)
    {
        _redisNotification = redisNotification;
        _redisDisruption = redisDisruption;
        _postgresNotification = postgresNotification;
        _postgresDisruption = postgresDisruption;
    }

    public async Task<long> DisruptionCountAsync() =>
        await _redisDisruption.GetDisruptionCountAsync();

    public async Task<long> DisruptionEndCountAsync() =>
        await _redisDisruption.GetDisruptionEndCountAsync();

    public async Task<long> DisruptionSeverityCountAsnc() =>
        await _redisDisruption.GetDisruptionSeverityCountAsync();

    public async Task<long> NotificationCountAsync() =>
        await _redisNotification.GetNotificationCountAsync();

    public Task<Result> DrainDisruptionsAsync() =>
        DrainAsync(_redisDisruption.GetDisruptionsAsync,
                   _postgresDisruption.AddDisruptionsAsync,
                   _redisDisruption.DeleteDisruptionsAsync);

    public Task<Result> DrainDisruptionSeveritiesAsync() =>
        DrainAsync(_redisDisruption.GetDisruptionSeveritiesAsync,
                   _postgresDisruption.AddDisruptionSeveritiesAsync,
                   _redisDisruption.DeleteDisruptionSeveritiesAsync);

    public Task<Result> DrainDisruptionEndsAsync() =>
        DrainAsync(_redisDisruption.GetDisruptionEndsAsync,
                   _postgresDisruption.AddDisruptionEndTimesAsync,
                   _redisDisruption.DeleteDisruptionEndsAsync);

    public Task<Result> DrainNotificationsAsync() =>
        DrainAsync(_redisNotification.GetNotificationsAsync,
                   _postgresNotification.AddNotificationsAsync,
                   _redisNotification.DeleteNotificationsAsync);

    private static async Task<Result> DrainAsync<T>(
    Func<Task<Result<IEnumerable<T>>>> getFromRedis,
    Func<IEnumerable<T>, Task<Result>> saveToPostgres,
    Func<Task<Result>> deleteFromRedis)
    {
        var items = await getFromRedis();
        if (items.IsFailure) return Result.Failure(items.Error);

        var saveResult = await saveToPostgres(items.Value);
        if (saveResult.IsFailure) return Result.Failure(saveResult.Error);

        var deleteResult = await deleteFromRedis();
        if (deleteResult.IsFailure) return Result.Failure(deleteResult.Error);

        return Result.Success();
    }
}
