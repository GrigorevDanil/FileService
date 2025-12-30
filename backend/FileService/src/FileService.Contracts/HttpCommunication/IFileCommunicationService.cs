using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Dtos;
using SharedService.SharedKernel;

namespace FileService.Contracts.HttpCommunication;

public interface IFileCommunicationService
{
    Task<Result<MediaAssetDto?, Errors>> GetMediaAsset(
        Guid mediaAssetId,
        CancellationToken cancellationToken = new CancellationToken());

    Task<Result<IEnumerable<MediaAssetDto>, Errors>> GetMediaAssets(
        Guid[] mediaAssetIds,
        CancellationToken cancellationToken = new CancellationToken());
}