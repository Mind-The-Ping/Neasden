using CSharpFunctionalExtensions;
using Neasden.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace Neasden.Consumer.Repositories;
public class UserNotifiedRepository : IUserNotifiedRepository
{
    private readonly IDatabase _database;

    private static string GetKey(Journey journey) =>
      $"notified:{journey.DisruptionId}:{journey.JourneyId}";

    public UserNotifiedRepository(ConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<Result> SaveJourneysAsync(IEnumerable<Journey> journeys)
    {
        var errors = new List<string>();
        var batch = _database.CreateBatch();

        var tasks = new List<Task>();

        foreach (var journey in journeys)
        {
            var key = GetKey(journey);
            var value = JsonSerializer.Serialize(journey);

            var now = DateTime.UtcNow;
            var todayEndTime = now.Date.Add(journey.EndTime.ToTimeSpan());
            var ttl = todayEndTime - now;

            if (ttl <= TimeSpan.Zero)
            {
                errors.Add($"TTL already expired for user {journey.JourneyId}.");
                continue;
            }

            var stringSetTask = batch.StringSetAsync(key, value, ttl + TimeSpan.FromMinutes(1));
            var setAddTask = batch.SetAddAsync($"notified_index:{journey.DisruptionId}", key);

            tasks.Add(stringSetTask.ContinueWith(t =>
            {
                if (!t.Result)
                    errors.Add($"Failed to save user {journey.JourneyId} to database.");
            }));

            tasks.Add(setAddTask.ContinueWith(t =>
            {
                if (!t.Result)
                    errors.Add($"Failed to index user {journey.JourneyId} for disruption {journey.DisruptionId}.");
            }));
        }

        batch.Execute();
        await Task.WhenAll(tasks);

        return errors.Count != 0
            ? Result.Failure(string.Join("; ", errors))
            : Result.Success();
    }

    public async Task<IEnumerable<Journey>> GetJourneysByDisruptionIdAsync(Guid disruptionId)
    {
        var indexKey = (RedisKey)$"notified_index:{disruptionId}";
        var results = new List<Journey>();

        RedisValue[] members = await _database.SetMembersAsync(indexKey);

        foreach (var member in members)
        {
            if (member.IsNullOrEmpty) {
                continue;
            }

            RedisKey userKey = member.ToString();
            var data = await _database.StringGetAsync(userKey);

            if (data.IsNullOrEmpty)
            {
                await _database.SetRemoveAsync(indexKey, member);
                continue;
            }

            var journey = JsonSerializer.Deserialize<Journey>(data!);
            if (journey != null) {
                results.Add(journey);
            }
        }

        return results;
    }

    public async Task DeleteByDisruptionIdAsync(Guid disruptionId)
    {
        var indexKey = $"notified_index:{disruptionId}";

        var keys = (await _database.SetMembersAsync(indexKey))
            .Select(x => (RedisKey)x.ToString())
            .ToArray();

        if (keys.Length > 0)
        {
            var batch = _database.CreateBatch();
            var deleteTasks = new List<Task>();

            foreach (var key in keys) {
                deleteTasks.Add(batch.KeyDeleteAsync(key));
            }

            deleteTasks.Add(batch.KeyDeleteAsync(indexKey));

            batch.Execute();
            await Task.WhenAll(deleteTasks);
        }
        else
        {
            await _database.KeyDeleteAsync(indexKey);
        }
    }

    public async Task DeleteJourneysAsync(IEnumerable<Journey> journeys)
    {
        var batch = _database.CreateBatch();
        var tasks = new List<Task>();

        foreach (var journey in journeys)
        {
            var key = $"notified:{journey.DisruptionId}:{journey.JourneyId}";
            tasks.Add(batch.KeyDeleteAsync(key));
            tasks.Add(batch.SetRemoveAsync($"notified_index:{journey.DisruptionId}", key));
        }

        batch.Execute();
        await Task.WhenAll(tasks);
    }
}
