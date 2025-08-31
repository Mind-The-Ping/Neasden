using Azure.Messaging.ServiceBus;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Neasden.Consumer.Repositories;
using Neasden.Consumer.Settings;

namespace Neasden.Consumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ServiceBusClient _client;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<ServiceBusProcessor> _processors = [];
    private readonly ServiceBusSettings _serviceBusSettings;

    public Worker(
        ILogger<Worker> logger, 
        ServiceBusClient client,
        IServiceProvider serviceProvider,
        IOptions<ServiceBusSettings> serviceBusSettings)
    {
        _client = client;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _serviceBusSettings=serviceBusSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        }

        await CreateQueueProcessor(_serviceBusSettings.Queues.Disruptions, stoppingToken);
        await CreateQueueProcessor(_serviceBusSettings.Queues.DisruptionSeverity, stoppingToken);
        await CreateQueueProcessor(_serviceBusSettings.Queues.Notifications, stoppingToken);

        await CreateTopicProcessor(
            _serviceBusSettings.Topics.DisruptionEnds.Name,
            _serviceBusSettings.Topics.DisruptionEnds.Subscription,
            stoppingToken
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task MessageHandler(ProcessMessageEventArgs args, string queueName)
    {
        using var scope = _serviceProvider.CreateScope();
        var disRepo = scope.ServiceProvider.GetRequiredService<DisruptionConsumerRepo>();
        var notifRepo = scope.ServiceProvider.GetRequiredService<NotificationConsumerRepo>();

        var result = queueName switch
        {
            var q when q == _serviceBusSettings.Queues.Disruptions => await disRepo.AddDisruptionAsync(args.Message.Body),
            var q when q == _serviceBusSettings.Queues.DisruptionSeverity => await disRepo.UpdateDisruptionSeverityAsync(args.Message.Body),
            var q when q == _serviceBusSettings.Queues.Notifications => await notifRepo.AddNotificationAsync(args.Message.Body),
            _ => Result.Failure($"A message was sent in the incorrect format {args.Message}"),
        };

    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus processing error");
        return Task.CompletedTask;
    }

    private async Task CreateQueueProcessor(string queueName, CancellationToken stoppingToken)
    {
        var processor = _client.CreateProcessor(queueName);

        processor = _client.CreateProcessor(queueName);
        processor.ProcessMessageAsync += args => MessageHandler(args, queueName);
        processor.ProcessErrorAsync += ErrorHandler;

        _processors.Add(processor);

        await processor.StartProcessingAsync(stoppingToken);
    }

    private async Task CreateTopicProcessor(string topicName, string subscriptionName, CancellationToken stoppingToken)
    {
        var processor = _client.CreateProcessor(topicName, subscriptionName);
        processor.ProcessMessageAsync += async args =>
        {
            using var scope = _serviceProvider.CreateScope();
            var disRepo = scope.ServiceProvider.GetRequiredService<DisruptionConsumerRepo>();

            await disRepo.AddDisruptionEndTimeAsync(args.Message.Body);
            await args.CompleteMessageAsync(args.Message);
        };

        processor.ProcessErrorAsync += ErrorHandler;

        _processors.Add(processor);

        await processor.StartProcessingAsync(stoppingToken);
    }
}
