namespace Neasden.Models;
public class DisruptionDescription
{
    public Guid Id { get; set; }
    public Guid DisruptionId { get; set; }
    public required string Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
