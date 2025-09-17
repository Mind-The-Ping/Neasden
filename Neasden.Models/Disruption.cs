namespace Neasden.Models;

public class Disruption
{
    public Guid Id { get; set; }
    public Guid LineId { get; set; }
    public Guid StartStationId { get; set; }
    public Guid EndStationId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
