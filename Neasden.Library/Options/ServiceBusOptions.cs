namespace Neasden.Library.Options;
public class ServiceBusOptions
{
    public required string Notifications { get; set; }
    public required string ConnectionString { get; set; }
    public required string ResolvedNotifications { get; set; }
}
