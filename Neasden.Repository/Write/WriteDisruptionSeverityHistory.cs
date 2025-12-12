using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Models;

namespace Neasden.Repository.Write;

public class WriteDisruptionSeverityHistory
{
    private readonly ILogger<WriteDisruptionSeverityHistory> _logger;
    private readonly IDbContextFactory<WriteDbContext> _contextFactory;

    public WriteDisruptionSeverityHistory(
        ILogger<WriteDisruptionSeverityHistory> logger,
                IDbContextFactory<WriteDbContext> contextFactory)
    {
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
        _contextFactory = contextFactory ??
            throw new ArgumentNullException(nameof(contextFactory));
    }

    public async Task<Result> AddDisruptionSeverityHistoryAsync(DisruptionSeverityHistory disruptionSeverityHistory)
    {
        await using var context = _contextFactory.CreateDbContext();
        if (context.SeverityHistories.Contains(disruptionSeverityHistory)) {
            return Result.Success();
        }

        try
        {
            await context.SeverityHistories.AddAsync(disruptionSeverityHistory);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            var message = $"Could not save disruption severity history {disruptionSeverityHistory.Id}. error: {ex.Message}";

            _logger.LogError(message);
            return Result.Failure(message);
        }
    }
}
