using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Neasden.Consumer.Repositories;
using Neasden.Repository.Database;
using Neasden.Repository.Repositories;

namespace Neasden.Consumer.Unit.Tests;

public class DisruptionConsumerRepoTests
{
    private readonly DisruptionConsumerRepo _repo;

    public DisruptionConsumerRepoTests()
    {
        var options = new DbContextOptionsBuilder<NeasdenDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        using var context = new NeasdenDbContext(options);
        var repository = new DisruptionRepository(context);
        _repo = new DisruptionConsumerRepo(repository);
    }

    [Fact]
    public async Task DisruptionConsumerRepo_AddDisruptionAsync_Wrong_Fails()
    {
        var body = new BinaryData([]);
        var result = await _repo.AddDisruptionAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Disruption message could not be deserialized.");
    }

    [Fact]
    public async Task DisruptionConsumerRepo_UpdateDisruptionSeverityAsync_Wrong_Fails()
    {
        var body = new BinaryData([]);
        var result = await _repo.UpdateDisruptionSeverityAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Disruption severity message could not be deserialized.");
    }

    [Fact]
    public async Task DisruptionConsumerRepo_AddDisruptionEndTimeAsync_Wrong_Fails()
    {
        var body = new BinaryData([]);
        var result = await _repo.AddDisruptionEndTimeAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Disruption end time message could not be deserialized.");
    }
}
