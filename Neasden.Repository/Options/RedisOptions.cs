namespace Neasden.Repository.Options;

public class RedisOptions
{
    public required string DisruptionKey { get; set; }
    public required string NotificationKey { get; set; }
    public required string ConnectionString { get; set; }
}
