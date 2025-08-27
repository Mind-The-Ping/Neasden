using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Neasden.Consumer.Repositories;
using Neasden.Repository.Database;
using Neasden.Repository.Repositories;

namespace Neasden.Consumer.Unit.Tests;
public class NotificationConsumerRepoTests
{
    private NotificationConsumerRepo _repo;

    public NotificationConsumerRepoTests()
    {
        var options = new DbContextOptionsBuilder<NeasdenDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        using var context = new NeasdenDbContext(options);
        var repository = new NotificationRepository(context);
        _repo = new NotificationConsumerRepo(repository);
    }

    [Fact]
    public async Task NotificationConsumerRepo_AddNotificationAsync_Wrong_Fails()
    {
        var body = new BinaryData([]);
        var result = await _repo.AddNotificationAsync(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Notification message could not be deserialized.");
    }
}
