using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Read;

namespace Neasden.Repository.Integration.Tests;
public class TestDbContextFactory : IDbContextFactory<ReadDbContext>
{
    private readonly DbContextOptions<ReadDbContext> _options;

    public TestDbContextFactory(DbContextOptions<ReadDbContext> options)
    {
        _options = options;
    }

    public ReadDbContext CreateDbContext()
    {
        return new ReadDbContext(_options);
    }
}
