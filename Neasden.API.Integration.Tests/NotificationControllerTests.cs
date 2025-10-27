using FluentAssertions;
using Neasden.API.Dto;
using Neasden.Repository.Database;
using Neasden.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Neasden.API.Integration.Tests;

public class NotificationControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly HttpClient _client;
    private readonly HttpClient _unauthorizedClient;
    private readonly NeasdenDbContext _dbContext;

    public NotificationControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        var token = factory.GenerateTestJwt(_id);
        _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        _unauthorizedClient = factory.CreateClient();

        _dbContext = factory.DbContext;
    }
    
    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Database.EnsureCreated();
    }
}
