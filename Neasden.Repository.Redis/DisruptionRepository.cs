using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Neasden.Models;
using Neasden.Repository.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace Neasden.Repository.Redis;

public class DisruptionRepository
{
    private readonly IDatabase _database;

    private readonly string _disruptionKey;
    private readonly string _disruptionEndKey;
    private readonly string _disruptionSeverityKey;

    public DisruptionRepository(IOptions<RedisOptions> options)
    {
        var redisOptions = options.Value ??
          throw new ArgumentNullException(nameof(options));

        var redis = ConnectionMultiplexer.Connect(redisOptions.ConnectionString);

        _database = redis.GetDatabase();

        _disruptionKey = redisOptions.DisruptionKey;
        _disruptionEndKey = redisOptions.DisruptionEndKey;
        _disruptionSeverityKey = redisOptions.DisruptionSeverityKey;
    }

    public async Task<Result> SaveDisruptionAsync(Disruption disruption)
    {
        var json = JsonSerializer.Serialize(disruption);

        if (string.IsNullOrWhiteSpace(json)) {
            return Result.Failure($"Could not serialize disruption {disruption.Id}.");
        }

        var setKey = $"{_disruptionKey}:ids";
        var added = await _database.SetAddAsync(setKey, disruption.Id.ToString());
        
        if (!added) {
            return Result.Success();
        }

        var result = await _database.ListRightPushAsync(_disruptionKey, json);

        return result > 0
            ? Result.Success()
            : Result.Failure($"Could not save disruption {disruption.Id} to Redis.");
    }

    public async Task<Result> SaveDisruptionSeverityAsync(DisruptionSeverity disruptionSeverity)
    {
        var json = JsonSerializer.Serialize(disruptionSeverity);

        if (string.IsNullOrWhiteSpace(json)) {
            return Result.Failure($"Could not serialize disruption severity {disruptionSeverity.Id}.");
        }

        var result = await _database.ListRightPushAsync(_disruptionSeverityKey, json);

        return result > 0
            ? Result.Success()
            : Result.Failure($"Could not save disruption severity {disruptionSeverity.Id} to Redis.");
    }

    public async Task<Result> SaveDisruptionEndAsync(DisruptionEnd disruptionEnd)
    {
        var json = JsonSerializer.Serialize(disruptionEnd);

        if (string.IsNullOrWhiteSpace(json)) {
            return Result.Failure($"Could not serialize disruption end time {disruptionEnd.Id}.");
        }

        var result = await _database.ListRightPushAsync(_disruptionEndKey, json);

        return result > 0
             ? Result.Success()
             : Result.Failure($"Could not save disruption end {disruptionEnd.Id} to Redis.");
    }

    public async Task<Result<IEnumerable<Disruption>>> GetDisruptionsAsync()
    {
        var values = await _database.ListRangeAsync(_disruptionKey, 0, -1);

        if (values.Length == 0) {
            return Result.Failure<IEnumerable<Disruption>>("No disruptions found in Redis.");
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

        if (values.Length == 0) {
            return Result.Failure<IEnumerable<DisruptionSeverity>>("No disruption severities found in Redis.");
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

        if (values.Length == 0) {
            return Result.Failure<IEnumerable<DisruptionEnd>>("No disruption ends found in Redis.");
        }

        var disruptionEnds = values
        .Select(v => JsonSerializer.Deserialize<DisruptionEnd>(v!))
        .Where(d => d != null)
        .ToList()!;

        return Result.Success<IEnumerable<DisruptionEnd>>(disruptionEnds!);
    }

    public async Task<Result> DeleteDisruptionsAsync()
    {
        var listKey = _disruptionKey;
        var setKey = $"{_disruptionKey}:ids";

        var deleted = await _database.KeyDeleteAsync([listKey, setKey]);

        return deleted > 0
            ? Result.Success()
            : Result.Failure("No disruptions found to delete.");
    }

    public async Task<Result> DeleteDisruptionSeveritiesAsync()
    {
        var deleted = await _database.KeyDeleteAsync(_disruptionSeverityKey);

        return deleted
           ? Result.Success()
           : Result.Failure("No disruption severties found to delete.");
    }

    public async Task<Result> DeleteDisruptionEndsAsync()
    {
        var deleted = await _database.KeyDeleteAsync(_disruptionEndKey);

        return deleted
           ? Result.Success()
           : Result.Failure("No disruption ends found to delete.");
    }
}
