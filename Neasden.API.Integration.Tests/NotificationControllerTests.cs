using FluentAssertions;
using Neasden.API.Dto;
using Neasden.Repository.Database;
using Neasden.Repository.Models;
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

    [Fact]
    public async Task NotificationController_Disruption_Successful()
    {
        var disruption = new Disruption
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            Description = "Something not human clambered out of the bottom of the Northen line, please stay away.",
            StartTime = DateTime.UtcNow
        };

        await _dbContext.Disruptions.AddAsync(disruption);
        await _dbContext.SaveChangesAsync();

        var response = await _client.GetAsync($"api/notification/disruption?id={disruption.Id}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Disruption>();

        result.Should().NotBeNull();
        result.Id.Should().Be(disruption.Id);
        result.LineId.Should().Be(disruption.LineId);
        result.StartStationId.Should().Be(disruption.StartStationId);
        result.EndStationId.Should().Be(disruption.EndStationId);
        result.Description.Should().Be(disruption.Description);
        result.StartTime.Should().BeCloseTo(disruption.StartTime, precision: TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task NotificationController_GetNotificationById_Successful()
    {
        var severity = new DisruptionSeverity
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Severe
        };

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            DisruptionId = severity.DisruptionId,
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            SeverityId = severity.Id,
            NotificationSentBy = NotificationSentBy.Push,
            SentTime = DateTime.UtcNow,
        };

        await _dbContext.Notifications.AddAsync(notification);
        await _dbContext.Severitys.AddAsync(severity);
        await _dbContext.SaveChangesAsync();

        var response = await _client.GetAsync($"api/notification/getById?id={notification.Id}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<NotificationReturn>();

        result.Should().NotBeNull();
        result.LineId.Should().Be(notification.LineId);
        result.DisruptionId.Should().Be(notification.DisruptionId);
        result.StartStationId.Should().Be(notification.StartStationId);
        result.EndStationId.Should().Be(notification.EndStationId);
        result.Severity.Should().Be(severity.Severity);
        result.NotificationSentBy.Should().Be(notification.NotificationSentBy);
        result.SentTime.Should().BeCloseTo(notification.SentTime, precision: TimeSpan.FromMilliseconds(10));
    }

    [Fact]
    public async Task NotificationController_GetNotificationsByUserId_Successful()
    {

    }
}
