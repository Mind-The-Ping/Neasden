namespace Neasden.Consumer.Settings;
public class ServiceBusSettings
{
    public required string ConnectionString { get; set; }
    public required QueuesSettings Queues { get; set; }

    public required TopicsSettings Topics { get; set; }
}

public class QueuesSettings
{
    public required string Disruptions { get; set; }
    public required string DisruptionSeverity { get; set; }
    public required string Notifications { get; set; }
}

public class TopicsSettings
{
    public required TopicSubscription DisruptionEnds { get; set; }
}

public class TopicSubscription
{
    public required string Name { get; set; }
    public required string Subscription { get; set; }
}