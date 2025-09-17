using Microsoft.EntityFrameworkCore;
using Neasden.Models;

namespace Neasden.Repository.Database;
public class NeasdenDbContext : DbContext
{
    public DbSet<Disruption> Disruptions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<DisruptionSeverity> Severities { get; set; }
    public DbSet<DisruptionDescription> Descriptions { get; set; }
    public NeasdenDbContext(DbContextOptions<NeasdenDbContext> options) 
        : base(options) { }
}