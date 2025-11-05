using Microsoft.EntityFrameworkCore;
using Neasden.Models;

namespace Neasden.Repository.Read;
public class ReadDbContext : DbContext
{
    public DbSet<Disruption> Disruptions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<DisruptionSeverity> Severities { get; set; }
    public DbSet<DisruptionDescription> Descriptions { get; set; }
    public ReadDbContext(DbContextOptions<ReadDbContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
