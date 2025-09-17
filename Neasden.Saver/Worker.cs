using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Neasden.Saver.Options;
using System.Diagnostics;

namespace Neasden.Saver;

public class Worker : BackgroundService
{
    private readonly SaverOptions _options;
    private readonly TimeSpan _maxInterval;
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(
        ILogger<Worker> logger,
        IOptions<SaverOptions> options,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _maxInterval = TimeSpan.FromSeconds(_options.MaxInterval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastDrain = Stopwatch.StartNew();

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information)) {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            using var scope = _scopeFactory.CreateScope();
            var saver = scope.ServiceProvider.GetRequiredService<Saver>();

            var disruptionCount = await saver.DisruptionCountAsync();
            var severityCount = await saver.DisruptionSeverityCountAsnc();
            var endCount = await saver.DisruptionEndCountAsync();
            var notificationCount = await saver.NotificationCountAsync();
            var descriptionCount = await saver.DisruptionDescriptionCountAsync();

            if (disruptionCount >= _options.MaxRecords || lastDrain.Elapsed >= _maxInterval)
                await DrainWithLogging(saver.DrainDisruptionsAsync, "Disruptions");

            if (severityCount >= _options.MaxRecords || lastDrain.Elapsed >= _maxInterval)
                await DrainWithLogging(saver.DrainDisruptionSeveritiesAsync, "Disruption Severities");

            if (endCount >= _options.MaxRecords || lastDrain.Elapsed >= _maxInterval)
                await DrainWithLogging(saver.DrainDisruptionEndsAsync, "Disruption Ends");

            if (notificationCount >= _options.MaxRecords || lastDrain.Elapsed >= _maxInterval)
                await DrainWithLogging(saver.DrainNotificationsAsync, "Notifications");

            if (descriptionCount >= _options.MaxRecords || lastDrain.Elapsed >= _maxInterval)
                await DrainWithLogging(saver.DrainDisruptionDescriptionsAsync, "Disruption Descriptions");

            if (disruptionCount >= _options.MaxRecords || 
                severityCount >= _options.MaxRecords || 
                endCount >= _options.MaxRecords || 
                notificationCount >= _options.MaxRecords||
                lastDrain.Elapsed >= _maxInterval)
            {
                lastDrain.Restart();
            }

            await Task.Delay(500, stoppingToken);
        }
    }

    private async Task DrainWithLogging(Func<Task<Result>> drainFunc, string name)
    {
        var result = await drainFunc();
        if (result.IsSuccess) {
            _logger.LogInformation("{name} drained successfully.", name);
        }
        else {
            _logger.LogError("Failed to drain {name}: {error}", name, result.Error);
        }
    }
}
