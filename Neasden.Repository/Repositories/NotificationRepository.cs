using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Database;
using Neasden.Models;

namespace Neasden.Repository.Repositories;
public class NotificationRepository
{
    private readonly NeasdenDbContext _neasdenDbContext;

    public NotificationRepository(NeasdenDbContext neasdenDbContext)
    {
        _neasdenDbContext = neasdenDbContext;
    }

    public async Task<Result> AddNotificationsAsync(IEnumerable<Notification> notifications)
    {
        try
        {
            await _neasdenDbContext.Notifications.AddRangeAsync(notifications);
            await _neasdenDbContext.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure("Could not save notifications to database.");
        }
    }

    public async Task<Result<Notification>> GetNotificationByIdAsync(Guid id)
    {
        var result = await _neasdenDbContext.Notifications
            .SingleOrDefaultAsync(x => x.Id == id);

        if (result == null) {
            return Result.Failure<Notification>($"Could not find notification {id} on the database.");
        }
        
        return Result.Success(result);
    }

    public async Task<Result<IEnumerable<Notification>>> GetNotificationsByUserId(Guid userId)
    {
        var result = await _neasdenDbContext.Notifications
            .Where(x => x.UserId == userId)
            .ToListAsync();

        if(result.Count == 0) {
            return Result.Failure<IEnumerable<Notification>>($"Could not find notifications for user {userId} on the database.");
        }

        return Result.Success<IEnumerable<Notification>>(result);
    }
}
