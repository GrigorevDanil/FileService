using CSharpFunctionalExtensions;
using FileService.Domain.MediaAssets.ValueObjects;
using FileService.Domain.PreviewAssets;
using FileService.Domain.VideoAssets;
using SharedService.SharedKernel;

namespace FileService.Domain;

public interface IMediaAssetFactory
{
    Result<VideoAsset, Error> CreateVideoForUpload(MediaData mediaData, MediaOwner owner);
    Result<PreviewAsset, Error> CreatePreviewForUpload(MediaData mediaData, MediaOwner owner);
}