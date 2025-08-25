using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Neasden.Repository.Database;
using System.Security.Claims;
using System.Text;


namespace Neasden.API.Integration.Tests;
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";
    public NeasdenDbContext DbContext { get; private set; } = null!;

    public IConfiguration Configuration { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<NeasdenDbContext>));

            if (descriptor != null) {
                services.Remove(descriptor);
            }

            services.AddDbContext<NeasdenDbContext>(options => {
                options.UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345");
            });

            var sp = services.BuildServiceProvider();

            Configuration = sp.GetRequiredService<IConfiguration>();

            var db = sp.GetRequiredService<NeasdenDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            DbContext = db;
        });
    }

    public string GenerateTestJwt(Guid userId) =>
        CreateToken(userId, Configuration);

    private static string CreateToken(Guid userId, IConfiguration configuration)
    {
        var secretKey = configuration["Jwt:Secret"]!;
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity
            ([
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
             ]),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = credentials,
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"]
        };

        var handler = new JsonWebTokenHandler();

        return handler.CreateToken(tokenDescriptor);
    }
}
