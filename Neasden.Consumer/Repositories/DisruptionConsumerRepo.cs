using CSharpFunctionalExtensions;
using Neasden.Repository.Redis;
using Neasden.Models;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Neasden.Consumer.Repositories;
public class DisruptionConsumerRepo
{
    public readonly DisruptionRepository _disruptionRepository;
    public readonly ILogger<DisruptionConsumerRepo> _logger;

    public DisruptionConsumerRepo(
        DisruptionRepository disruptionRepository, 
        ILogger<DisruptionConsumerRepo> logger)
    {
        _disruptionRepository = disruptionRepository ?? 
            throw new ArgumentNullException(nameof(disruptionRepository));

        _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> AddDisruptionAsync(BinaryData body)
    {
        Disruption? message;
        try
        {
            var json = body.ToArray();
            message = JsonSerializer.Deserialize<Disruption>(json);
        }
        catch 
        {
            var errorMessage = "Disruption message could not be deserialized.";

            _logger.LogError(errorMessage);
            return Result.Failure(errorMessage);
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
        catch 
        {
            var errorMessage = "Disruption severity message could not be deserialized.";

            _logger.LogError(errorMessage);
            return Result.Failure(errorMessage);
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
        catch 
        {
            var errorMessage = "Disruption end time message could not be deserialized.";

            _logger.LogError(errorMessage);
            return Result.Failure(errorMessage);
        }

        var result = await _disruptionRepository.SaveDisruptionEndAsync(message!);
        return result;
    }

    public async Task<Result> AddDisruptionDescriptionAsync(BinaryData body)
    {
        DisruptionDescription? message;
        try
        {
            var json = body.ToArray();
            message = JsonSerializer.Deserialize<DisruptionDescription>(json);
        }
        catch 
        {
            var errorMessage = "Disruption description message could not be deserialized.";

            _logger.LogError(errorMessage);
            return Result.Failure(errorMessage);
        }
        
        var result = await _disruptionRepository.SaveDisruptionDescriptionAsync(message!);
        return result;
    }
}
