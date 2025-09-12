using CSharpFunctionalExtensions;
using Neasden.Repository.Redis;
using Neasden.Models;
using System.Text.Json;

namespace Neasden.Consumer.Repositories;
public class DisruptionConsumerRepo
{
    public readonly DisruptionRepository _disruptionRepository;

    public DisruptionConsumerRepo(DisruptionRepository disruptionRepository)
    {
        _disruptionRepository = disruptionRepository;
    }

    public async Task<Result> AddDisruptionAsync(BinaryData body)
    {
        Disruption? message;
        try
        {
            var json = body.ToArray();
            message = JsonSerializer.Deserialize<Disruption>(json);
        }
        catch {
            return Result.Failure("Disruption message could not be deserialized.");
        }

        var result = await _disruptionRepository.SaveDisruptionAsync(message!);
        return result;
    }

    public async Task<Result> UpdateDisruptionSeverityAsync(BinaryData body)
    {
        DisruptionSeverity? message;
        try
        {
            var json = body.ToArray();
            message = JsonSerializer.Deserialize<DisruptionSeverity>(json);
        }
        catch {
            return Result.Failure("Disruption severity message could not be deserialized.");
        }

        var result = await _disruptionRepository.SaveDisruptionSeverityAsync(message!);
        return result;
    }

    public async Task<Result> AddDisruptionEndTimeAsync(BinaryData body)
    {
        DisruptionEnd? message;
        try
        {
            var json = body.ToArray();
            message = JsonSerializer.Deserialize<DisruptionEnd>(json);
        }
        catch {
            return Result.Failure("Disruption end time message could not be deserialized.");
        }

        var result = await _disruptionRepository.SaveDisruptionEndAsync(message!);
        return result;
    }
}
