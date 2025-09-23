using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Database;
using Neasden.Models;
using Microsoft.Extensions.Logging;

namespace Neasden.Repository.Repositories;
public class NotificationRepository
{
    private readonly NeasdenDbContext _neasdenDbContext;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(
        NeasdenDbContext neasdenDbContext,
        ILogger<NotificationRepository> logger)
    {
        _neasdenDbContext = neasdenDbContext ?? 
            throw new ArgumentNullException(nameof(neasdenDbContext));

        _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> AddNotificationsAsync(IEnumerable<Notification> notifications)
    {
        try
        {
            await _neasdenDbContext.Notifications.AddRangeAsync(notifications);
            await _neasdenDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex) 
        {
            var message = "Could not save notifications to database.";

            _logger.LogError(ex, message);
            return Result.Failure(message);
        }
    }

    public async Task<Result<Notification>> GetNotificationByIdAsync(Guid id)
    {
        var result = await _neasdenDbContext.Notifications
            .SingleOrDefaultAsync(x => x.Id == id);

        if (result == null) 
        {
            var message = $"Notification {id} does not exist on this database.";

            _logger.LogError(message);
            return Result.Failure<Notification>(message);
        }
        
        return Result.Success(result);
    }

    public async Task<Result<IEnumerable<Notification>>> GetNotificationsByUserId(Guid userId)
    {
        var result = await _neasdenDbContext.Notifications
            .Where(x => x.UserId == userId)
            .ToListAsync();

        if(result.Count == 0) 
        {
            var message = $"Notifications for user {userId} do not exist on the database.";

            _logger.LogError(message);
            return Result.Failure<IEnumerable<Notification>>(message);
        }

        return Result.Success<IEnumerable<Notification>>(result);
    }
}
