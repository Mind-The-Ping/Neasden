using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Models;

namespace Neasden.Repository.Database;
public class NeasdenDbContext : DbContext
{
    public DbSet<Disruption> Disruptions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<DisruptionSeverity> Severitys { get; set; }
    public NeasdenDbContext(DbContextOptions<NeasdenDbContext> options) 
        : base(options) { }
}