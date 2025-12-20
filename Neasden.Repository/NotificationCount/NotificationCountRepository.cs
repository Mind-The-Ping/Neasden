using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Neasden.Models;

namespace Neasden.Repository.NotificationCount;

public class NotificationCountRepository : INotificationCountRepository
{
    private readonly ILogger<NotificationCountRepository> _logger;
    private readonly IMongoCollection<UnReadNotification> _unReadNotificationsCollection;

    public NotificationCountRepository(
        IOptions<DatabaseOptions> databaseOptions,
        IMongoDatabase mongoDatabase,
        ILogger<NotificationCountRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(databaseOptions);

        var options = databaseOptions.Value ??
            throw new ArgumentNullException(nameof(databaseOptions));

        var database = mongoDatabase ??
          throw new ArgumentNullException(nameof(mongoDatabase));

        _unReadNotificationsCollection =
            database.GetCollection<UnReadNotification>(options.Collection);
    }

    public async Task<Result> AddToCountAsync(UnReadNotification unReadNotification)
    {
        try
        {
            await _unReadNotificationsCollection.InsertOneAsync(unReadNotification);
            return Result.Success();
        }
        catch(Exception ex)
        {
            var message = $"Could not save unread notification : {unReadNotification.NotificationId}.";

            _logger.LogError(ex, message);
            return Result.Failure(message);
        }
    }

    public async Task<int> GetUserNotificationCountAsync(Guid userId) =>
         (int) await _unReadNotificationsCollection
        .CountDocumentsAsync(n => n.UserId == userId);
   
       

    public async Task<Result> RemoveFromCountAsync(Guid notificationId)
    {
        try
        {
            var result = await _unReadNotificationsCollection
            .DeleteOneAsync(n => n.NotificationId == notificationId);

            return Result.Success();
        }
        catch (Exception ex) 
        {
            var message = $"Could not delete unread notification: {notificationId}.";
            _logger.LogError(ex, message);
            return Result.Failure(message);
        }
    }
}
