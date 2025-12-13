namespace Neasden.Library.Options;
public class RedisOptions
{
    public required string Connection { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public int Port { get; set; }
}
