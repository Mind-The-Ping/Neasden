using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neasden.Models;
using Neasden.Repository.Redis.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace Neasden.Repository.Redis;

public class DisruptionRepository
{
    private readonly IDatabase _database;

    private readonly string _disruptionKey;
    private readonly string _disruptionEndKey;
    private readonly string _disruptionSeverityKey;
    private readonly string _descriptionKey;
    private readonly ILogger<DisruptionRepository> _logger;


    public DisruptionRepository(
        IOptions<RedisOptions> options,
        ConnectionMultiplexer redis,
        ILogger<DisruptionRepository> logger)
    {
        var redisOptions = options.Value ??
          throw new ArgumentNullException(nameof(options));

        _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));

        _database = redis.GetDatabase();

        _disruptionKey = redisOptions.DisruptionKey;
        _descriptionKey = redisOptions.DescriptionKey;
        _disruptionEndKey = redisOptions.DisruptionEndKey;
        _disruptionSeverityKey = redisOptions.DisruptionSeverityKey;
    }

    public async Task<Result> SaveDisruptionAsync(Disruption disruption)
    {
        var json = JsonSerializer.Serialize(disruption);

        if (string.IsNullOrWhiteSpace(json)) 
        {
            var message = $"Could not serialize disruption {disruption.Id}.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        var setKey = $"{_disruptionKey}:ids";
        var added = await _database.SetAddAsync(setKey, disruption.Id.ToString());
        
        if (!added) {
            return Result.Success();
        }

        var result = await _database.ListRightPushAsync(_disruptionKey, json);

        if(result <= 0)
        {
            var message = $"Could not save disruption {disruption.Id} to Redis.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        return Result.Success();
    }

    public async Task<Result> SaveDisruptionSeverityAsync(DisruptionSeverity disruptionSeverity)
    {
        var json = JsonSerializer.Serialize(disruptionSeverity);

        if (string.IsNullOrWhiteSpace(json)) 
        {
            var message = $"Could not serialize disruption severity {disruptionSeverity.Id}.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        var result = await _database.ListRightPushAsync(_disruptionSeverityKey, json);

        if(result <= 0)
        {
            var message = $"Could not save disruption severity {disruptionSeverity.Id} to Redis.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        return Result.Success();
    }

    public async Task<Result> SaveDisruptionEndAsync(DisruptionEnd disruptionEnd)
    {
        var json = JsonSerializer.Serialize(disruptionEnd);

        if (string.IsNullOrWhiteSpace(json)) 
        {
            var message = $"Could not serialize disruption end time {disruptionEnd.Id}.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        var result = await _database.ListRightPushAsync(_disruptionEndKey, json);

        if (result <= 0)
        {
            var message = $"Could not save disruption end {disruptionEnd.Id} to Redis.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        return Result.Success();
    }

    public async Task<Result> SaveDisruptionDescriptionAsync(DisruptionDescription disruptionDescription)
    {
        var json = JsonSerializer.Serialize(disruptionDescription);

        if (string.IsNullOrWhiteSpace(json)) 
        {
            var message = $"Could not serialize disruption description {disruptionDescription.Id}.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        var result = await _database.ListRightPushAsync(_descriptionKey, json);

        if(result <= 0)
        {
            var message = $"Could not save disruption description {disruptionDescription.Id} to Redis.";
            
            _logger.LogError(message);
            return Result.Failure(message);
        }

        return Result.Success();
    }

    public async Task<Result<IEnumerable<Disruption>>> GetDisruptionsAsync()
    {
        var values = await _database.ListRangeAsync(_disruptionKey, 0, -1);

        if (values.Length == 0) 
        {
            var message = "No disruptions found in Redis.";

            _logger.LogError(message);
            return Result.Failure<IEnumerable<Disruption>>(message);
        }

        var disruptions = values
        .Select(v => JsonSerializer.Deserialize<Disruption>(v!))
        .Where(d => d != null)
        .ToList()!;

        return Result.Success<IEnumerable<Disruption>>(disruptions!);
    }

    public async Task<Result<IEnumerable<DisruptionSeverity>>> GetDisruptionSeveritiesAsync()
    {
        var values = await _database.ListRangeAsync(_disruptionSeverityKey, 0, -1);

        if (values.Length == 0) 
        {
            var message = "No disruption severities found in Redis.";

            _logger.LogError(message);
            return Result.Failure<IEnumerable<DisruptionSeverity>>(message);
        }

        var disruptions = values
        .Select(v => JsonSerializer.Deserialize<DisruptionSeverity>(v!))
        .Where(d => d != null)
        .ToList()!;

        return Result.Success<IEnumerable<DisruptionSeverity>>(disruptions!);
    }

    public async Task<Result<IEnumerable<DisruptionEnd>>> GetDisruptionEndsAsync()
    {
        var values = await _database.ListRangeAsync(_disruptionEndKey, 0, -1);

        if (values.Length == 0) 
        {
            var message = "No disruption ends found in Redis.";

            _logger.LogError(message);
            return Result.Failure<IEnumerable<DisruptionEnd>>(message);
        }

        var disruptionEnds = values
        .Select(v => JsonSerializer.Deserialize<DisruptionEnd>(v!))
        .Where(d => d != null)
        .ToList()!;

        return Result.Success<IEnumerable<DisruptionEnd>>(disruptionEnds!);
    }

    public async Task<Result<IEnumerable<DisruptionDescription>>> GetDisruptionDescriptionsAsync()
    {
        var values = await _database.ListRangeAsync(_descriptionKey, 0, -1);

        if (values.Length == 0) 
        {
            var message = "No disruption descriptions found in Redis.";

            _logger.LogError(message);
            return Result.Failure<IEnumerable<DisruptionDescription>>(message);
        }

        var disruptionDescriptions = values
           .Select(v => JsonSerializer.Deserialize<DisruptionDescription>(v!))
           .Where(d => d != null)
           .ToList()!;

        return Result.Success<IEnumerable<DisruptionDescription>>(disruptionDescriptions!);
    }

    public async Task<Result> DeleteDisruptionsAsync()
    {
        var listKey = _disruptionKey;
        var setKey = $"{_disruptionKey}:ids";

        var deleted = await _database.KeyDeleteAsync([listKey, setKey]);

        if(deleted <= 0)
        {
            var message = "No disruptions found to delete.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteDisruptionSeveritiesAsync()
    {
        var deleted = await _database.KeyDeleteAsync(_disruptionSeverityKey);

        if(!deleted)
        {
            var message = "No disruption severties found to delete.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteDisruptionEndsAsync()
    {
        var deleted = await _database.KeyDeleteAsync(_disruptionEndKey);

        if(!deleted)
        {
            var message = "No disruption ends found to delete.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteDisruptionDescriptionsAsync()
    {
        var deleted = await _database.KeyDeleteAsync(_descriptionKey);

        if(!deleted)
        {
            var message = "No disruption descriptions found to delete.";

            _logger.LogError(message);
            return Result.Failure(message);
        }

        return Result.Success();
    }

    public async Task<long> GetDisruptionCountAsync() =>
         await _database.SetLengthAsync($"{_disruptionKey}:ids");

    public async Task<long> GetDisruptionSeverityCountAsync() =>
        await _database.ListLengthAsync(_disruptionSeverityKey);

    public async Task<long> GetDisruptionEndCountAsync() =>
         await _database.ListLengthAsync(_disruptionEndKey);

    public async Task<long> GetDisruptionDescriptionCountAsync() =>
        await _database.ListLengthAsync(_descriptionKey);
}
