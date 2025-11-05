using Microsoft.EntityFrameworkCore;
using Neasden.Models;

namespace Neasden.Repository.Write;
public class WriteDbContext : DbContext
{
    public DbSet<Disruption> Disruptions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<DisruptionSeverity> Severities { get; set; }
    public DbSet<DisruptionDescription> Descriptions { get; set; }
    public WriteDbContext(DbContextOptions<WriteDbContext> options)
        : base(options) { }
}
