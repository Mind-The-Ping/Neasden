using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Neasden.Models;
using Neasden.Repository.NotificationCount;

namespace Neasden.Repository.Integration.Tests;

public class NotificationCountRepositoryTests
{
    private readonly IMongoDatabase _mongoDatabase;
    private readonly IMongoCollection<UnReadNotification> _notificationCollection;
    private readonly NotificationCountRepository _notificationCountRepository;
    private readonly ILogger<NotificationCountRepository> _logger;
    private readonly IOptions<DatabaseOptions> _options;

    private readonly string _databaseName = $"testdb_{Guid.NewGuid():N}";

    public NotificationCountRepositoryTests()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        _mongoDatabase = client.GetDatabase(_databaseName);

        var databaseOptions = new DatabaseOptions()
        {
            Name = "Neasden",
            Collection = "UnReadNotifications",
            ConnectionString = "mongodb://localhost:27017"
        };

        _options = Options.Create(databaseOptions);
        _logger = NSubstitute.Substitute.For<ILogger<NotificationCountRepository>>();

        _notificationCountRepository = new NotificationCountRepository(
            _options, 
            _mongoDatabase,
            _logger);

        _notificationCollection = _mongoDatabase
            .GetCollection<UnReadNotification>(databaseOptions.Collection);
    }

    private async Task InitializeAsync()
    {
        await _mongoDatabase.Client.DropDatabaseAsync(_databaseName);
    }

    [Fact]
    public void NotificationCountRepository_Ctor_DatabaseOptions_Throws_ArguementNullException()
    {
        var exception = Assert
           .Throws<ArgumentNullException>(() => new NotificationCountRepository(
              null!,
              _mongoDatabase,
              _logger));

        exception.Message.Should().Be("Value cannot be null. (Parameter 'databaseOptions')");
    }

    [Fact]
    public void NotificationCountRepository_Ctor_Database_Throws_ArguementNullException()
    {
        var exception = Assert
           .Throws<ArgumentNullException>(() => new NotificationCountRepository(
              _options,
              null!,
              _logger));

        exception.Message.Should().Be("Value cannot be null. (Parameter 'mongoDatabase')");
    }

    [Fact]
    public void NotificationCountRepository_Ctor_Logger_Throws_ArguementNullException()
    {
        var exception = Assert
           .Throws<ArgumentNullException>(() => new NotificationCountRepository(
              _options,
              _mongoDatabase,
              null!));

        exception.Message.Should().Be("Value cannot be null. (Parameter 'logger')");
    }

    [Fact]
    public async Task NotificationCountRepository_AddToCountAsync_Successful()
    {
        await InitializeAsync();

        var notification = new UnReadNotification(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            DateTime.UtcNow);

        var result = await _notificationCountRepository.AddToCountAsync(notification);

        result.IsSuccess.Should().BeTrue();

        var record = await _notificationCollection
            .Find(x => x.NotificationId == notification.NotificationId)
            .SingleOrDefaultAsync();

        record.NotificationId.Should().Be(notification.NotificationId);
        record.UserId.Should().Be(notification.UserId);
        record.CreatedAt.Should().BeCloseTo(notification.CreatedAt, TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task NotificationCountRepository_RemoveFromCountAsync_Successful()
    {
        await InitializeAsync();

        var notification = new UnReadNotification(
           Guid.NewGuid(),
           Guid.NewGuid(),
           DateTime.UtcNow);

        var insertResult = await _notificationCountRepository.AddToCountAsync(notification);
        insertResult.IsSuccess.Should().BeTrue();

        var result = await _notificationCountRepository
            .RemoveFromCountAsync(notification.NotificationId);

        result.IsSuccess.Should().BeTrue();

        var record = await _notificationCollection
           .Find(x => x.NotificationId == notification.NotificationId)
           .SingleOrDefaultAsync();

        record.Should().BeNull();
    }

    [Fact]
    public async Task NotificationCountRepository_GetUserNotificationCountAsync_Successful()
    {
        await InitializeAsync();

        var notification = new UnReadNotification(
           Guid.NewGuid(),
           Guid.NewGuid(),
           DateTime.UtcNow);

        var insertResult = await _notificationCountRepository.AddToCountAsync(notification);
        insertResult.IsSuccess.Should().BeTrue();

        var result = await _notificationCountRepository.GetUserNotificationCountAsync(notification.UserId);
        result.Should().Be(1);
    }
}
