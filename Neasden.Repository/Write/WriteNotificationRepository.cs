using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Neasden.Models;

namespace Neasden.Repository.Write;
public class WriteNotificationRepository : IWriteNotificationRepository
{
    private readonly WriteDbContext _context;
    private readonly ILogger<WriteNotificationRepository> _logger;

    public WriteNotificationRepository(
        WriteDbContext context,
        ILogger<WriteNotificationRepository> logger)
    {
        _context = context ??
           throw new ArgumentNullException(nameof(context));

        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> AddNotificationsAsync(IEnumerable<Notification> notifications)
    {
        try
        {
            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            var message = "Could not save notifications to database.";

            _logger.LogError(ex, message);
            return Result.Failure(message);
        }
    }
}
