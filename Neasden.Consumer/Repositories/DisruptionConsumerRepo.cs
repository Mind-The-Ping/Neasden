using CSharpFunctionalExtensions;
using Neasden.Consumer.Models;
using Neasden.Repository.Models;
using Neasden.Repository.Repositories;
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

        var result = await _disruptionRepository.AddDisruptionAsync(
                 message!.Id,
                 message.LineId,
                 message.StartStationId,
                 message.EndStationId,
                 message.Description,
                 message.StartTime);

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

        var result = await _disruptionRepository.AddDisruptionSeverityAsync(
            message!.Id,
            message.DisruptionId,
            message.StartTime,
            message.Severity);

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

        var result = await _disruptionRepository
            .AddDisruptionEndTimeAsync(message!.Id, message.EndTime);

        return result;
    }
}
