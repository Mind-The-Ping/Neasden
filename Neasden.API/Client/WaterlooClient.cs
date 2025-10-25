using CSharpFunctionalExtensions;
using Microsoft.Extensions.Options;
using Neasden.API.Model;
using Neasden.API.Options;
using System.Net.Http.Headers;

namespace Neasden.API.Client;

public class WaterlooClient : IWaterlooClient
{
    private readonly HttpClient _httpClient;
    private readonly TokenProvider _tokenProvider;
    private readonly WaterlooOptions _waterlooOptions;

    public WaterlooClient(
         HttpClient httpClient,
         TokenProvider tokenProvider,
         IOptions<WaterlooOptions> waterlooOptions)
    {
        _httpClient = httpClient ??
           throw new ArgumentNullException(nameof(httpClient));
        _tokenProvider = tokenProvider ??
            throw new ArgumentNullException(nameof(tokenProvider));
        _waterlooOptions = waterlooOptions.Value ??
            throw new ArgumentNullException(nameof(waterlooOptions));
    }

    public async Task<Result<Line>> GetLineById(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
              new AuthenticationHeaderValue("Bearer", _tokenProvider.CreateToken());

            var url = $"{_waterlooOptions.BaseUrl}/{_waterlooOptions.GetLineById}{id}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if(response.IsSuccessStatusCode)
            {
                var line = await response.Content.ReadFromJsonAsync<Line>();
                return line is null
                    ? Result.Failure<Line>(
                        $"Null response for line id: {id}")
                    : Result.Success(line);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<Line>(
                $"Line {id} response failed {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result
                .Failure<Line>($"Exception getting line with id: {id} error: {ex.Message}");
        }
    }

    public async Task<Result<Station>> GetStationById(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
             new AuthenticationHeaderValue("Bearer", _tokenProvider.CreateToken());

            var url = $"{_waterlooOptions.BaseUrl}/{_waterlooOptions.GetStationById}{id}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var line = await response.Content.ReadFromJsonAsync<Station>();
                return line is null
                    ? Result.Failure<Station>(
                        $"Null response for station id: {id}")
                    : Result.Success(line);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<Station>(
                $"Station {id} response failed {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result
               .Failure<Station>($"Exception getting station with id: {id} error: {ex.Message}");
        }
    }
}
