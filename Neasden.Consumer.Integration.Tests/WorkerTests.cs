using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neasden.Repository.Database;
using Neasden.Repository.Models;
using System.Text.Json;

namespace Neasden.Consumer.Integration.Tests;

public class WorkerTests
{
    [Fact]
    public async Task Worker_DisruptionMessages_Save_Successfully()
    {
        var testObjects = await TestInitalizerAsync("queue.1");

        var testDisruption = new Disruption
        {
            Id = Guid.NewGuid(),
            LineId = Guid.NewGuid(),
            StartStationId = Guid.NewGuid(),
            EndStationId = Guid.NewGuid(),
            Description = "Test disruption",
            StartTime = DateTime.UtcNow
        };

        var messageBody = new BinaryData(JsonSerializer.Serialize(testDisruption));
        await testObjects.Item2.SendMessageAsync(new ServiceBusMessage(messageBody));

        await Task.Delay(1000);

        using var assertScope = testObjects.Item1.Services.CreateScope();
        var dbContext = assertScope.ServiceProvider.GetRequiredService<NeasdenDbContext>();

        var saved = await dbContext.Disruptions.SingleOrDefaultAsync(d => d.Id == testDisruption.Id);

        saved.Should().NotBeNull();
        saved.Id.Should().Be(testDisruption.Id);
        saved.LineId.Should().Be(testDisruption.LineId);
        saved.StartStationId.Should().Be(testDisruption.StartStationId);
        saved.EndStationId.Should().Be(testDisruption.EndStationId);
        saved.Description.Should().Be(testDisruption.Description);
        saved.StartTime.Should().BeCloseTo(testDisruption.StartTime, precision: TimeSpan.FromMilliseconds(1));

        await testObjects.Item1.StopAsync();
    }

    [Fact]
    public async Task Worker_DisruptionSeverityMessages_Save_Successfully()
    {
        var testObjects = await TestInitalizerAsync("queue.2");
        var testDisruptionSeverity = new DisruptionSeverity
        {
            Id = Guid.NewGuid(),
            DisruptionId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            Severity = Severity.Suspended
        };

        var messageBody = new BinaryData(JsonSerializer.Serialize(testDisruptionSeverity));
        await testObjects.Item2.SendMessageAsync(new ServiceBusMessage(messageBody));

        await Task.Delay(1000);

        using var assertScope = testObjects.Item1.Services.CreateScope();
        var dbContext = assertScope.ServiceProvider.GetRequiredService<NeasdenDbContext>();

        var saved = await dbContext.Severitys.SingleOrDefaultAsync(d => d.Id == testDisruptionSeverity.Id);

        saved.Should().NotBeNull();
        saved.Id.Should().Be(testDisruptionSeverity.Id);
        saved.DisruptionId.Should().Be(testDisruptionSeverity.DisruptionId);
        saved.StartTime.Should().BeCloseTo(testDisruptionSeverity.StartTime, precision: TimeSpan.FromMilliseconds(1));
        saved.Severity.Should().Be(testDisruptionSeverity.Severity);
    }

    private async Task<(IHost, ServiceBusSender)> TestInitalizerAsync (string queueName)
    {
        var host = HostFactory.CreateHost(Array.Empty<string>(), forTesting: true);

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NeasdenDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        await host.StartAsync();

        var serviceBusClient = host.Services.GetRequiredService<ServiceBusClient>();
        return (host, serviceBusClient.CreateSender(queueName));
    }
}
