using Microsoft.EntityFrameworkCore;
using Neasden.Repository.Write;

namespace Neasden.Repository.Integration.Tests.Write;
public class TestWriteDbContextFactory : IDbContextFactory<WriteDbContext>
{
    private readonly DbContextOptions<WriteDbContext> _options;

    public TestWriteDbContextFactory(DbContextOptions<WriteDbContext> options)
    {
        _options = options;
    }

    public WriteDbContext CreateDbContext()
    {
        return new WriteDbContext(_options);
    }
}
