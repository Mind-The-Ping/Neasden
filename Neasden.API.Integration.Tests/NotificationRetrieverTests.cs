using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neasden.Library.Clients;
using Neasden.Models;
using Neasden.Repository.Integration.Tests.Read;
using Neasden.Repository.Read;
using Neasden.Repository.Write;
using NSubstitute;

namespace Neasden.API.Integration.Tests;


public class NotificationRetrieverTests
{
    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    private readonly WriteDbContext _writeContext;
    private readonly IWaterlooClient _waterlooClient;

    private readonly NotificationRetriever _notificationRetriever;

    public NotificationRetrieverTests()
    {
        var readOptions = new DbContextOptionsBuilder<ReadDbContext>()
            .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
            .Options;

        var _contextFactory = new TestReadDbContextFactory(readOptions);

        using (var context = _contextFactory.CreateDbContext())
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }

        var notificationRepoLogger = Substitute.For<ILogger<ReadNotificationRepository>>();
        var notificationRepository = new ReadNotificationRepository(_contextFactory, notificationRepoLogger);

        var disruptionRepoLogger = Substitute.For<ILogger<ReadDisruptionRepository>>();
        var disruptionRepository = new ReadDisruptionRepository(_contextFactory, disruptionRepoLogger);

        _waterlooClient = Substitute.For<IWaterlooClient>();

        var notificationReLogger = Substitute.For<ILogger<NotificationRetriever>>();

        _notificationRetriever = new NotificationRetriever(
            _waterlooClient,
            notificationReLogger,
            disruptionRepository,
            notificationRepository);

        var writeOptions = new DbContextOptionsBuilder<WriteDbContext>()
          .UseNpgsql($"Host=localhost;Port=5434;Database={_databaseName};Username=neasdenUser;Password=password12345")
          .Options;

        _writeContext = new WriteDbContext(writeOptions);
    }

    [Fact]
    public async Task NotificationRetriever_GetNotificiationAsnyc_Successful()
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

        var result = await _notificationRetriever.GetNotificiationAsnyc(notification.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Line.Should().Be(line);
        result.Value.JourneyStartStation.Should().Be(getStationResult1.First());
        result.Value.JourneyEndStation.Should().Be(getStationResult2.First());
        result.Value.DisruptionStartStation.Should().Be(getStationResult4.First());
        result.Value.DisruptionEndStation.Should().Be(getStationResult5.First());
        result.Value.AffectedStations.Should().BeEquivalentTo(getStationResult3);
        result.Value.Severity.Should().Be(severity.Severity);
        result.Value.SentDate.Should().BeCloseTo(notification.SentTime, TimeSpan.FromSeconds(1));
        result.Value.DisruptionStart.Should().BeCloseTo(disruption.StartTime, TimeSpan.FromSeconds(1));
        result.Value.DisruptionEnd.Should().BeCloseTo(disruption.EndTime, TimeSpan.FromSeconds(1));
        result.Value.DisruptionDescription.Should().Be(description.Description);
    }

    [Fact]
    public async Task NotificationRetriever_GetNotificiationsByUserIdAsnyc_Successful()
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

        var result = await _notificationRetriever.GetNotificationsByUserIdAsync(notification.UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalPages.Should().Be(1);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.First().Line.Should().Be(line);
        result.Value.Items.First().JourneyStartStation.Should().Be(getStationResult1.First());
        result.Value.Items.First().JourneyEndStation.Should().Be(getStationResult2.First());
        result.Value.Items.First().DisruptionStartStation.Should().Be(getStationResult4.First());
        result.Value.Items.First().DisruptionEndStation.Should().Be(getStationResult5.First());
        result.Value.Items.First().AffectedStations.Should().BeEquivalentTo(getStationResult3);
        result.Value.Items.First().Severity.Should().Be(severity.Severity);
        result.Value.Items.First().SentDate.Should().BeCloseTo(notification.SentTime, TimeSpan.FromSeconds(1));
        result.Value.Items.First().DisruptionStart.Should().BeCloseTo(disruption.StartTime, TimeSpan.FromSeconds(1));
        result.Value.Items.First().DisruptionEnd.Should().BeCloseTo(disruption.EndTime, TimeSpan.FromSeconds(1));
        result.Value.Items.First().DisruptionDescription.Should().Be(description.Description);
    }

    [Fact]
    public async Task NotificationRetriever_GetNotificiationsByUserIdAsnyc_None_Successful()
    {
        var result = await _notificationRetriever.GetNotificationsByUserIdAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalPages.Should().Be(0);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task NotificationRetriever_GetNotificiationsByUserIdLatestAsync_Successful()
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

        var result = await _notificationRetriever.GetNotificationsByUserLatestIdAsync(notification.UserId, DateTime.UtcNow.AddMinutes(-10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.First().Line.Should().Be(line);
        result.Value.Items.First().JourneyStartStation.Should().Be(getStationResult1.First());
        result.Value.Items.First().JourneyEndStation.Should().Be(getStationResult2.First());
        result.Value.Items.First().DisruptionStartStation.Should().Be(getStationResult4.First());
        result.Value.Items.First().DisruptionEndStation.Should().Be(getStationResult5.First());
        result.Value.Items.First().AffectedStations.Should().BeEquivalentTo(getStationResult3);
        result.Value.Items.First().Severity.Should().Be(severity.Severity);
        result.Value.Items.First().SentDate.Should().BeCloseTo(notification.SentTime, TimeSpan.FromSeconds(1));
        result.Value.Items.First().DisruptionStart.Should().BeCloseTo(disruption.StartTime, TimeSpan.FromSeconds(1));
        result.Value.Items.First().DisruptionEnd.Should().BeCloseTo(disruption.EndTime, TimeSpan.FromSeconds(1));
        result.Value.Items.First().DisruptionDescription.Should().Be(description.Description);
    }
}
