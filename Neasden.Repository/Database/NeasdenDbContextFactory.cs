using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Neasden.Repository.Database;
public class NeasdenDbContextFactory : IDesignTimeDbContextFactory<NeasdenDbContext>
{
    public NeasdenDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
          .SetBasePath(Directory.GetCurrentDirectory())
          .AddJsonFile("appsettings.json")
          .Build();

        var optionsBuilder = new DbContextOptionsBuilder<NeasdenDbContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        return new NeasdenDbContext(optionsBuilder.Options);
    }
}
