using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Database;
using Neasden.Repository.Models;

namespace Neasden.Repository.Repositories;
public class NotificationRepository
{
    private readonly NeasdenDbContext _neasdenDbContext;

    public NotificationRepository(NeasdenDbContext neasdenDbContext)
    {
        _neasdenDbContext = neasdenDbContext;
    }

    public async Task<Result<Guid>> CreateNotificationAsync(
        Guid id,
        Guid userId,
        Guid lineId,
        Guid disruptionId,
        Guid severityId,
        Guid startStationId,
        Guid endStationId,
        NotificationSentBy notificationSentBy,
        DateTime sentTime)
    {
        var notification = new Notification
        {
            Id = id,
            UserId = userId,
            LineId = lineId,
            DisruptionId = disruptionId,
            SeverityId = severityId,
            StartStationId = startStationId,
            EndStationId = endStationId,
            NotificationSentBy = notificationSentBy,
            SentTime = sentTime
        };

        try
        {
            await _neasdenDbContext.Notifications.AddAsync(notification);
            await _neasdenDbContext.SaveChangesAsync();
        }
        catch (Exception ex) {
            return Result.Failure<Guid>($"Database could not save notification for {userId}.");
        }

        return Result.Success(notification.Id);
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
