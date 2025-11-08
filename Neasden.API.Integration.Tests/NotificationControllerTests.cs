using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Neasden.Library.Clients;
using Neasden.Models;
using Neasden.Repository.Write;
using NSubstitute;
using System.Net;
using System.Net.Http.Headers;

namespace Neasden.API.Integration.Tests;

public class NotificationControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly HttpClient _client;
    private readonly HttpClient _unauthorizedClient;
    private readonly WriteDbContext _writeContext;
    private readonly IWaterlooClient _waterlooClient;

    public NotificationControllerTests(CustomWebApplicationFactory factory)
    {
        _waterlooClient = Substitute.For<IWaterlooClient>();

        var customizedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IWaterlooClient));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(_waterlooClient);
            });
        });

        _client = customizedFactory.CreateClient();
        var token = factory.GenerateTestJwt(_id);
        _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        _unauthorizedClient = factory.CreateClient();

        var options = new DbContextOptionsBuilder<WriteDbContext>()
            .UseNpgsql($"Host=localhost;Port=5434;Database={factory.databaseName};Username=neasdenUser;Password=password12345")
            .Options;

        _writeContext = new WriteDbContext(options);
    }

    [Fact]
    public async Task NotificationController_GetNotificationById_Successful()
    {
        var disruption = new Disruption()
        {
            Id = Guid.NewGuid(),
            LineId = Guid.Parse("2f0c75a5-8149-49b7-9cc6-32e4a5246d7f"),
            StartStationId = Guid.Parse("b02ebcb8-83a4-48e1-85d8-6e3fb21fa058"),
            EndStationId = Guid.Parse("9c8c4a97-c895-4c03-bba7-0f54a3b11bb3"),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(5),
        };

        var severity = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            StartTime = disruption.StartTime,
            Severity = Severity.Severe
        };

        var description = new DisruptionDescription()
        {
            Id = Guid.NewGuid(),
            Description = "Something bad happened I guess ??"
        };

        var notification = new Notification()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LineId = Guid.Parse("2f0c75a5-8149-49b7-9cc6-32e4a5246d7f"),
            StartStationId = Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"),
            EndStationId = Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"),
            AffectedStationIds = [
                Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"),
                Guid.Parse("5c15a8f5-a21d-4567-97a4-3cbc095d2298"),
                Guid.Parse("28cee11a-267d-4170-9cdc-2e7ef7b6ca40"),
                Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"),
                ],
            DisruptionId = disruption.Id,
            DescriptionId = description.Id,
            SentTime = disruption.StartTime.AddMinutes(5),
            SeverityId = severity.Id,
            NotificationSentBy = NotificationSentBy.Push
        };

        await _writeContext.Disruptions.AddAsync(disruption);
        await _writeContext.Severities.AddAsync(severity);
        await _writeContext.Descriptions.AddAsync(description);
        await _writeContext.Notifications.AddAsync(notification);
        await _writeContext.SaveChangesAsync();

        var line = new Line(Guid.Parse("2f0c75a5-8149-49b7-9cc6-32e4a5246d7f"), "Jubilee");

        _waterlooClient
            .GetLinesById(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result.Success<IEnumerable<Line>>([line])
            ));

        var getStationResult1 = new List<Station>() { new(Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"), "North Greenwich") };
        var getStationResult2 = new List<Station>() { new(Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"), "Bermondsey") };
        var getStationResult3 = new List<Station>()
        {
            new(Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"), "North Greenwich"),
            new(Guid.Parse("5c15a8f5-a21d-4567-97a4-3cbc095d2298"), "Canary Wharf"),
            new(Guid.Parse("28cee11a-267d-4170-9cdc-2e7ef7b6ca40"), "Canada Water"),
            new(Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"), "Bermondsey")
        };
        var getStationResult4 = new List<Station>() { new(Guid.Parse("b02ebcb8-83a4-48e1-85d8-6e3fb21fa058"), "Stratford") };
        var getStationResult5 = new List<Station>() { new(Guid.Parse("9c8c4a97-c895-4c03-bba7-0f54a3b11bb3"), "London Bridge") };

        _waterlooClient
            .GetStationsById(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(getStationResult1, getStationResult2, getStationResult3, getStationResult4, getStationResult5);

        var response = await _client.GetAsync($"api/notification/getById?id={notification.Id}");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task NotificationController_GetNotificationById_Unauthorized_User_Successful()
    {
        var response = await _unauthorizedClient.GetAsync($"api/notification/getById?id={Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NotificationController_GetNotificationsByUserId_Successful()
    {
        var disruption = new Disruption()
        {
            Id = Guid.NewGuid(),
            LineId = Guid.Parse("2f0c75a5-8149-49b7-9cc6-32e4a5246d7f"),
            StartStationId = Guid.Parse("b02ebcb8-83a4-48e1-85d8-6e3fb21fa058"),
            EndStationId = Guid.Parse("9c8c4a97-c895-4c03-bba7-0f54a3b11bb3"),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(5),
        };

        var severity = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            StartTime = disruption.StartTime,
            Severity = Severity.Severe
        };

        var description = new DisruptionDescription()
        {
            Id = Guid.NewGuid(),
            Description = "Something bad happened I guess ??"
        };

        var notification = new Notification()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LineId = Guid.Parse("2f0c75a5-8149-49b7-9cc6-32e4a5246d7f"),
            StartStationId = Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"),
            EndStationId = Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"),
            AffectedStationIds = [
                Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"),
                Guid.Parse("5c15a8f5-a21d-4567-97a4-3cbc095d2298"),
                Guid.Parse("28cee11a-267d-4170-9cdc-2e7ef7b6ca40"),
                Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"),
                ],
            DisruptionId = disruption.Id,
            DescriptionId = description.Id,
            SentTime = disruption.StartTime.AddMinutes(5),
            SeverityId = severity.Id,
            NotificationSentBy = NotificationSentBy.Push
        };

        await _writeContext.Disruptions.AddAsync(disruption);
        await _writeContext.Severities.AddAsync(severity);
        await _writeContext.Descriptions.AddAsync(description);
        await _writeContext.Notifications.AddAsync(notification);
        await _writeContext.SaveChangesAsync();

        var line = new Line(Guid.Parse("2f0c75a5-8149-49b7-9cc6-32e4a5246d7f"), "Jubilee");

        _waterlooClient
            .GetLinesById(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result.Success<IEnumerable<Line>>([line])
            ));

        var getStationResult1 = new List<Station>() { new(Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"), "North Greenwich") };
        var getStationResult2 = new List<Station>() { new(Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"), "Bermondsey") };
        var getStationResult3 = new List<Station>()
        {
            new(Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"), "North Greenwich"),
            new(Guid.Parse("5c15a8f5-a21d-4567-97a4-3cbc095d2298"), "Canary Wharf"),
            new(Guid.Parse("28cee11a-267d-4170-9cdc-2e7ef7b6ca40"), "Canada Water"),
            new(Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"), "Bermondsey")
        };
        var getStationResult4 = new List<Station>() { new(Guid.Parse("b02ebcb8-83a4-48e1-85d8-6e3fb21fa058"), "Stratford") };
        var getStationResult5 = new List<Station>() { new(Guid.Parse("9c8c4a97-c895-4c03-bba7-0f54a3b11bb3"), "London Bridge") };

        _waterlooClient
            .GetStationsById(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(getStationResult1, getStationResult2, getStationResult3, getStationResult4, getStationResult5);

        var response = await _client.GetAsync($"api/notification/getByUserId?page=1&pageSize=20");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task NotificationController_GetNotificationsByUserId_Unauthorized_User_Successful()
    {
        var response = await _unauthorizedClient.GetAsync($"api/notification/getByUserId?page=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NotificationController_GetNotificationsByUserIdLatest_Successful()
    {
        var disruption = new Disruption()
        {
            Id = Guid.NewGuid(),
            LineId = Guid.Parse("2f0c75a5-8149-49b7-9cc6-32e4a5246d7f"),
            StartStationId = Guid.Parse("b02ebcb8-83a4-48e1-85d8-6e3fb21fa058"),
            EndStationId = Guid.Parse("9c8c4a97-c895-4c03-bba7-0f54a3b11bb3"),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(5),
        };

        var severity = new DisruptionSeverity()
        {
            Id = Guid.NewGuid(),
            StartTime = disruption.StartTime,
            Severity = Severity.Severe
        };

        var description = new DisruptionDescription()
        {
            Id = Guid.NewGuid(),
            Description = "Something bad happened I guess ??"
        };

        var notification = new Notification()
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            LineId = Guid.Parse("2f0c75a5-8149-49b7-9cc6-32e4a5246d7f"),
            StartStationId = Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"),
            EndStationId = Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"),
            AffectedStationIds = [
                Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"),
                Guid.Parse("5c15a8f5-a21d-4567-97a4-3cbc095d2298"),
                Guid.Parse("28cee11a-267d-4170-9cdc-2e7ef7b6ca40"),
                Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"),
                ],
            DisruptionId = disruption.Id,
            DescriptionId = description.Id,
            SentTime = disruption.StartTime.AddMinutes(5),
            SeverityId = severity.Id,
            NotificationSentBy = NotificationSentBy.Push
        };

        await _writeContext.Disruptions.AddAsync(disruption);
        await _writeContext.Severities.AddAsync(severity);
        await _writeContext.Descriptions.AddAsync(description);
        await _writeContext.Notifications.AddAsync(notification);
        await _writeContext.SaveChangesAsync();

        var line = new Line(Guid.Parse("2f0c75a5-8149-49b7-9cc6-32e4a5246d7f"), "Jubilee");

        _waterlooClient
            .GetLinesById(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Result.Success<IEnumerable<Line>>([line])
            ));

        var getStationResult1 = new List<Station>() { new(Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"), "North Greenwich") };
        var getStationResult2 = new List<Station>() { new(Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"), "Bermondsey") };
        var getStationResult3 = new List<Station>()
        {
            new(Guid.Parse("6252902f-7fd2-45a8-a6d5-1f377e88b9be"), "North Greenwich"),
            new(Guid.Parse("5c15a8f5-a21d-4567-97a4-3cbc095d2298"), "Canary Wharf"),
            new(Guid.Parse("28cee11a-267d-4170-9cdc-2e7ef7b6ca40"), "Canada Water"),
            new(Guid.Parse("6842d9a0-acc8-4843-a3d3-2e06d03fdcd1"), "Bermondsey")
        };
        var getStationResult4 = new List<Station>() { new(Guid.Parse("b02ebcb8-83a4-48e1-85d8-6e3fb21fa058"), "Stratford") };
        var getStationResult5 = new List<Station>() { new(Guid.Parse("9c8c4a97-c895-4c03-bba7-0f54a3b11bb3"), "London Bridge") };

        _waterlooClient
            .GetStationsById(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(getStationResult1, getStationResult2, getStationResult3, getStationResult4, getStationResult5);

        var response = await _client.GetAsync($"api/notification/getByUserIdLatest?lastChecked={DateTime.UtcNow.AddMinutes(-10)}");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task NotificationController_GetNotificationsByUserIdLatest_Unauthorized_User_Successful()
    {
        var response = await _unauthorizedClient.GetAsync($"api/notification/getByUserIdLatest?lastChecked={DateTime.UtcNow.AddMinutes(-10)}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _writeContext.Database.EnsureDeleted();
        _writeContext.Database.EnsureCreated();
    }
}
