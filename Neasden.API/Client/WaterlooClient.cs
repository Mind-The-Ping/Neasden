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

    public async Task<Result<IEnumerable<Line>>> GetLinesById(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
              new AuthenticationHeaderValue("Bearer", _tokenProvider.CreateToken());

            var url = $"{_waterlooOptions.BaseUrl}/{_waterlooOptions.GetLinesById}";

            var response = await _httpClient.PostAsJsonAsync(url, ids, cancellationToken);
            if(response.IsSuccessStatusCode)
            {
                var lines = await response.Content.ReadFromJsonAsync<IEnumerable<Line>>
                    (cancellationToken: cancellationToken);

                if(lines is null || !lines.Any()) {
                    return Result.Failure<IEnumerable<Line>>($"Response from get lines is empty for ids: {ids}");
                }

                return Result.Success(lines);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<IEnumerable<Line>>(
                $"Lines {ids} response failed {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result
                .Failure<IEnumerable<Line>>($"Exception getting lines with ids: {ids} error: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<Station>>> GetStationsById(
        IEnumerable<Guid> ids, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
             new AuthenticationHeaderValue("Bearer", _tokenProvider.CreateToken());

            var url = $"{_waterlooOptions.BaseUrl}/{_waterlooOptions.GetStationsById}";

            var response = await _httpClient.PostAsJsonAsync(url, ids, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var stations = await response.Content.ReadFromJsonAsync<IEnumerable<Station>>
                    (cancellationToken: cancellationToken);

                if (stations is null || !stations.Any()) {
                    return Result.Failure<IEnumerable<Station>>($"Response from get stations is empty for ids: {ids}");
                }

                return Result.Success(stations);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return Result.Failure<IEnumerable<Station>>(
                $"Stations {ids} response failed {response.StatusCode}: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result
               .Failure<IEnumerable<Station>>($"Exception getting stations with ids: {ids} error: {ex.Message}");
        }
    }
}
