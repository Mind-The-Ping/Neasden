using FluentAssertions;
using Neasden.Consumer.Repositories;
using Neasden.Models;
using StackExchange.Redis;
using System.Text.Json;
using Testcontainers.Redis;

namespace Neasden.Consumer.Integration.Tests;

public class UserNotifiedRepositoryTests : IAsyncLifetime
{

    private readonly RedisContainer _redisContainer;

    public UserNotifiedRepositoryTests()
    {
        _redisContainer = new RedisBuilder()
         .WithImage("redis:7.2")
         .WithCleanUp(true)
         .Build();
    }

    public async Task InitializeAsync() =>
       await _redisContainer.StartAsync();


    public async Task DisposeAsync() =>
        await _redisContainer.DisposeAsync();

    [Fact]
    public async Task UserNotifiedRepository_SaveJourneysAsync_Successfully()
    {
        var redis = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        var database = redis.GetDatabase();

        var userNotifiedRepository = new UserNotifiedRepository(redis);

        var line = new Line(Guid.Parse("c7f7c41a-03d2-4a79-9e8e-b55b1b5a056e"), "Central");
        var startStation = new Station(Guid.Parse("44e87f5b-015d-42f8-a250-232e226de45b"), "Chancery Lane");
        var endStation = new Station(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End");
        var affectedStations = new List<Station>()
        {
           new(Guid.Parse("44e87f5b-015d-42f8-a250-232e226de45b"), "Chancery Lane"),
           new(Guid.Parse("299580df-c896-486f-898d-c51f4a0bd0d2"), "St. Pauls"),
           new(Guid.Parse("aaedc653-e766-4d6b-87e2-4c87322971ef"), "Bank"),
           new(Guid.Parse("db101bcd-350e-4485-b875-7ac2c8c1b6cc"), "Liverpool Street"),
           new(Guid.Parse("b7a5ae67-882b-4509-8df9-4bae2ef1dd2a"), "Bethnal Green"),
           new(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End")
        };

        var journey = new Journey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            line,
            startStation,
            endStation,
            Severity.Minor,
            "+441234567890",
            PhoneOS.Android,
            TimeOnly.FromDateTime(DateTime.UtcNow.AddMinutes(30)),
            affectedStations);

        var journeys = new List<Journey>
        {
            journey
        };

        var result = await userNotifiedRepository.SaveJourneysAsync(journeys);
        result.IsSuccess.Should().BeTrue();

        var values = await database.SetMembersAsync($"notified_index:{journey.DisruptionId}");
        values.Count().Should().Be(1);

        var recordResult = await database.StringGetAsync((RedisKey)values.First().ToString());
        var journeyRecord = JsonSerializer.Deserialize<Journey>(recordResult!);

        journeyRecord.JourneyId.Should().Be(journey.JourneyId);
        journeyRecord.UserId.Should().Be(journey.UserId);
        journeyRecord.DisruptionId.Should().Be(journey.DisruptionId);
        journeyRecord.Line.Should().Be(journey.Line);
        journeyRecord.StartStation.Should().Be(journey.StartStation);
        journeyRecord.EndStation.Should().Be(journey.EndStation);
        journeyRecord.Severity.Should().Be(journey.Severity);
        journeyRecord.PhoneNumber.Should().Be(journey.PhoneNumber);
        journeyRecord.PhoneOS.Should().Be(journey.PhoneOS);
        journeyRecord.EndTime.Should().Be(journey.EndTime);
        journeyRecord.AffectedStations.Should().BeEquivalentTo(journey.AffectedStations);
    }


    [Fact]
    public async Task UserNotifiedRepository_GetJourneysByDisruptionIdAsync_Successfully()
    {
        var multiplexer = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        var userNotifiedRepository = new UserNotifiedRepository(multiplexer);

        var line = new Line(Guid.Parse("c7f7c41a-03d2-4a79-9e8e-b55b1b5a056e"), "Central");
        var startStation = new Station(Guid.Parse("44e87f5b-015d-42f8-a250-232e226de45b"), "Chancery Lane");
        var endStation = new Station(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End");
        var affectedStations = new List<Station>()
        {
           new(Guid.Parse("44e87f5b-015d-42f8-a250-232e226de45b"), "Chancery Lane"),
           new(Guid.Parse("299580df-c896-486f-898d-c51f4a0bd0d2"), "St. Pauls"),
           new(Guid.Parse("aaedc653-e766-4d6b-87e2-4c87322971ef"), "Bank"),
           new(Guid.Parse("db101bcd-350e-4485-b875-7ac2c8c1b6cc"), "Liverpool Street"),
           new(Guid.Parse("b7a5ae67-882b-4509-8df9-4bae2ef1dd2a"), "Bethnal Green"),
           new(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End")
        };

        var disruptionId = Guid.NewGuid();

        var journey1 = new Journey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            disruptionId,
            line,
            startStation,
            endStation,
            Severity.Minor,
            "+441234567890",
            PhoneOS.Android,
            TimeOnly.FromDateTime(DateTime.UtcNow.AddMinutes(30)),
            affectedStations);


        var journey2 = new Journey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            disruptionId,
            line,
            startStation,
            endStation,
            Severity.Severe,
            "+441244562891",
            PhoneOS.IOS,
            TimeOnly.FromDateTime(DateTime.UtcNow.AddMinutes(45)),
            affectedStations);

        var journeys = new List<Journey>
        {
            journey1, journey2
        };

        await userNotifiedRepository.SaveJourneysAsync(journeys);

        var result = await userNotifiedRepository.GetJourneysByDisruptionIdAsync(disruptionId);

        result.Count().Should().Be(2);
        result.Should().BeEquivalentTo([journey1, journey2]);
    }

    [Fact]
    public async Task UserNotifiedRepository_DeleteByDisruptionIdAsync_Successfully()
    {
        var multiplexer = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        var userNotifiedRepository = new UserNotifiedRepository(multiplexer);

        var disruptionId = Guid.NewGuid();

        var line = new Line(Guid.Parse("c7f7c41a-03d2-4a79-9e8e-b55b1b5a056e"), "Central");
        var startStation = new Station(Guid.Parse("44e87f5b-015d-42f8-a250-232e226de45b"), "Chancery Lane");
        var endStation = new Station(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End");
        var affectedStations = new List<Station>()
        {
           new(Guid.Parse("44e87f5b-015d-42f8-a250-232e226de45b"), "Chancery Lane"),
           new(Guid.Parse("299580df-c896-486f-898d-c51f4a0bd0d2"), "St. Pauls"),
           new(Guid.Parse("aaedc653-e766-4d6b-87e2-4c87322971ef"), "Bank"),
           new(Guid.Parse("db101bcd-350e-4485-b875-7ac2c8c1b6cc"), "Liverpool Street"),
           new(Guid.Parse("b7a5ae67-882b-4509-8df9-4bae2ef1dd2a"), "Bethnal Green"),
           new(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End")
        };

        var journey1 = new Journey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            disruptionId,
            line,
            startStation,
            endStation,
            Severity.Minor,
            "+441234567890",
            PhoneOS.Android,
            TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)),
            affectedStations);

        var journey2 = new Journey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            disruptionId,
            line,
            startStation,
            endStation,
            Severity.Severe,
            "+441244562891",
            PhoneOS.IOS,
            TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)),
            affectedStations);

        var journeys = new List<Journey>
        {
            journey1,
            journey2
        };

        await userNotifiedRepository.SaveJourneysAsync(journeys);
        await userNotifiedRepository.DeleteByDisruptionIdAsync(disruptionId);

        var result = await userNotifiedRepository.GetJourneysByDisruptionIdAsync(disruptionId);
        result.Count().Should().Be(0);
    }

    [Fact]
    public async Task UserNotifiedRepository_DeleteJourneysAsync_Successfully()
    {
        var multiplexer = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        var userNotifiedRepository = new UserNotifiedRepository(multiplexer);

        var disruptionId = Guid.NewGuid();

        var line = new Line(Guid.Parse("c7f7c41a-03d2-4a79-9e8e-b55b1b5a056e"), "Central");
        var startStation = new Station(Guid.Parse("44e87f5b-015d-42f8-a250-232e226de45b"), "Chancery Lane");
        var endStation = new Station(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End");
        var affectedStations = new List<Station>()
        {
           new(Guid.Parse("44e87f5b-015d-42f8-a250-232e226de45b"), "Chancery Lane"),
           new(Guid.Parse("299580df-c896-486f-898d-c51f4a0bd0d2"), "St. Pauls"),
           new(Guid.Parse("aaedc653-e766-4d6b-87e2-4c87322971ef"), "Bank"),
           new(Guid.Parse("db101bcd-350e-4485-b875-7ac2c8c1b6cc"), "Liverpool Street"),
           new(Guid.Parse("b7a5ae67-882b-4509-8df9-4bae2ef1dd2a"), "Bethnal Green"),
           new(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End")
        };

        var journey1 = new Journey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            disruptionId,
            line,
            startStation,
            endStation,
            Severity.Minor,
            "+441234567890",
            PhoneOS.Android,
            TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)),
            affectedStations);

        var journey2 = new Journey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            disruptionId,
            line,
            startStation,
            endStation,
            Severity.Severe,
            "+441244562891",
            PhoneOS.IOS,
            TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)),
            affectedStations);

        var journey3 = new Journey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            disruptionId,
            line,
            startStation,
            endStation,
            Severity.Severe,
            "+441244562822",
            PhoneOS.IOS,
            TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(2)),
            affectedStations);

        var users = new List<Journey>
        {
            journey1,
            journey2,
            journey3,
        };

        await userNotifiedRepository.SaveJourneysAsync(users);
        await userNotifiedRepository.DeleteJourneysAsync([journey1, journey2]);

        var result = await userNotifiedRepository.GetJourneysByDisruptionIdAsync(disruptionId);
        result.Count().Should().Be(1);
        result.First().JourneyId.Should().Be(journey3.JourneyId);
    }

    [Fact]
    public async Task UserNotifiedRepository_Keys_ShouldExpire_AfterTTL()
    {
        var multiplexer = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        var repo = new UserNotifiedRepository(multiplexer);

        var disruptionId = Guid.NewGuid();

        var line = new Line(Guid.Parse("c7f7c41a-03d2-4a79-9e8e-b55b1b5a056e"), "Central");
        var startStation = new Station(Guid.Parse("44e87f5b-015d-42f8-a250-232e226de45b"), "Chancery Lane");
        var endStation = new Station(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End");
        var affectedStations = new List<Station>()
        {
           new(Guid.Parse("44e87f5b-015d-42f8-a250-232e226de45b"), "Chancery Lane"),
           new(Guid.Parse("299580df-c896-486f-898d-c51f4a0bd0d2"), "St. Pauls"),
           new(Guid.Parse("aaedc653-e766-4d6b-87e2-4c87322971ef"), "Bank"),
           new(Guid.Parse("db101bcd-350e-4485-b875-7ac2c8c1b6cc"), "Liverpool Street"),
           new(Guid.Parse("b7a5ae67-882b-4509-8df9-4bae2ef1dd2a"), "Bethnal Green"),
           new(Guid.Parse("73bce1de-143f-4903-928a-c34ceb3db42e"), "Mile End")
        };

        var journey = new Journey(
            Guid.NewGuid(),
            Guid.NewGuid(),
            disruptionId,
            line,
            startStation,
            endStation,
            Severity.Minor,
            "+441234567890",
            PhoneOS.Android,
            TimeOnly.FromDateTime(DateTime.UtcNow.AddSeconds(1)),
            affectedStations);

        var journeys = new List<Journey>
        {
            journey
        };

        await repo.SaveJourneysAsync(journeys);
        await Task.Delay(70000);

        var result = await repo.GetJourneysByDisruptionIdAsync(disruptionId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UserNotifiedRepository_DeleteByDisruptionIdAsync_ShouldNotFail_WhenNoUsers()
    {
        var multiplexer = ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString());
        var repo = new UserNotifiedRepository(multiplexer);

        var disruptionId = Guid.NewGuid();

        await repo.DeleteByDisruptionIdAsync(disruptionId);

        var result = await repo.GetJourneysByDisruptionIdAsync(disruptionId);

        result.Should().BeEmpty();
    }
}
