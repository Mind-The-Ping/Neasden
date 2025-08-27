namespace Neasden.Consumer.Settings;
public class ServiceBusSettings
{
    public required string ConnectionString { get; set; }
    public required QueuesSettings Queues { get; set; }
}

public class QueuesSettings
{
    public required string Disruptions { get; set; }
    public required string DisruptionSeverity { get; set; }
    public required string DisruptionEndTimes { get; set; }
    public required string Notifications { get; set; }
}