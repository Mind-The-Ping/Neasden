namespace Neasden.Repository.Redis.Options;

public class RedisOptions
{
    public required string DisruptionKey { get; set; }
    public required string DisruptionSeverityKey { get; set; }
    public required string DisruptionEndKey { get; set; }
    public required string NotificationKey { get; set; }
    public required string DescriptionKey { get; set; }
    public required string ConnectionString { get; set; }
}
