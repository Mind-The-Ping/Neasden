using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;

namespace Neasden.Repository.Read;
public class ReadNotificationRepository
{
    private readonly ILogger<ReadNotificationRepository> _logger;
    private readonly IDbContextFactory<ReadDbContext> _contextFactory;

    public ReadNotificationRepository(
        IDbContextFactory<ReadDbContext> contextFactory,
        ILogger<ReadNotificationRepository> logger)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<Notification>> GetNotificationByIdAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        var result = await context.Notifications
            .SingleOrDefaultAsync(x => x.Id == id);

        if (result == null)
        {
            var message = $"Notification {id} does not exist on this database.";

            _logger.LogError(message);
            return Result.Failure<Notification>(message);
        }

        return Result.Success(result);
    }

    public async Task<Result<PaginatedResult<Notification>>> GetNotificationIdsByUserIdAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var context = _contextFactory.CreateDbContext();
        var query = context.Notifications
           .Where(x => x.UserId == userId)
           .OrderByDescending(x => x.SentTime);

        var totalCount = await query.CountAsync();

        if (totalCount == 0)
        {
            return Result.Success(new PaginatedResult<Notification>(
               [],
               page,
               pageSize,
               0));
        }

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new PaginatedResult<Notification>(items, page, pageSize, totalCount);
        return Result.Success(result);
    }

    public async Task<Result<PaginatedResult<Notification>>> GetNotificationIdsByUserIdLatestAsync(Guid userId, DateTime lastChecked)
    {
        await using var context = _contextFactory.CreateDbContext();
        var query = context.Notifications
           .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync();

        var newItemsQuery = query
           .Where(x => x.SentTime > lastChecked)
           .OrderByDescending(x => x.SentTime);

        var newItems = await newItemsQuery.ToListAsync();

        if (newItems.Count == 0)
        {
            return Result.Success(new PaginatedResult<Notification>(
                [],
                1,
                0,
                totalCount
            ));
        }
        var result = new PaginatedResult<Notification>(newItems, 1, newItems.Count, totalCount);
        return Result.Success(result);
    }
}
