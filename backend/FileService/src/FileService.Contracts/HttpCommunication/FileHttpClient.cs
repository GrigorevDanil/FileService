using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Dtos;
using FileService.Contracts.MediaAssets.Requests;
using Microsoft.Extensions.Logging;
using SharedService.Core.HttpCommunication;
using SharedService.SharedKernel;

namespace FileService.Contracts.HttpCommunication;

internal sealed class FileHttpClient: IFileCommunicationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileHttpClient> _logger;

    public FileHttpClient(HttpClient httpClient, ILogger<FileHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<MediaAssetDto?, Errors>> GetMediaAsset(
        Guid mediaAssetId,
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"api/files/{mediaAssetId}", cancellationToken);

            return await response.HandleResponseAsync<MediaAssetDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media asset for mediaAsset by id {MediaAssetId}",  mediaAssetId);

            return GeneralErrors.Failure(ex.Message).ToErrors();
        }
    }

    public async Task<Result<IEnumerable<MediaAssetDto>, Errors>> GetMediaAssets(
        Guid[] mediaAssetIds,
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            GetMediaAssetsInfoRequest request = new(mediaAssetIds);

            HttpResponseMessage response = await _httpClient.GetAsync("api/files/batch", request, cancellationToken);

            return (await response.HandleResponseAsync<IEnumerable<MediaAssetDto>>(cancellationToken))!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting media assets for mediaAsset by ids {MediaAssetIds}", string.Join(", ", mediaAssetIds));

            return GeneralErrors.Failure(ex.Message).ToErrors();
        }
    }
}