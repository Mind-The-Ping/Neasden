using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Read;

namespace Neasden.Repository.Integration.Tests.Read;
public class TestReadDbContextFactory : IDbContextFactory<ReadDbContext>
{
    private readonly DbContextOptions<ReadDbContext> _options;

    public TestReadDbContextFactory(DbContextOptions<ReadDbContext> options)
    {
        _options = options;
    }

    public ReadDbContext CreateDbContext()
    {
        return new ReadDbContext(_options);
    }
}
