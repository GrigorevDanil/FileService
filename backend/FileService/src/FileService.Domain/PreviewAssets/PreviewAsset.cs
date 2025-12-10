using CSharpFunctionalExtensions;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using SharedService.SharedKernel;

namespace FileService.Domain.PreviewAssets;

public sealed class PreviewAsset: MediaAsset
{
    public const long MAX_SIZE = 10_485_760;
    public const string LOCATION = "preview";
    public const string RAW_PREFIX = "raw";
    public const string ALLOWED_CONTENT_TYPE = "image";

    public static readonly string[] AllowedExtensions = ["jpg", "jpeg", "png", "webp"];

    public PreviewAsset(
        MediaAssetId id,
        MediaData mediaData,
        MediaStatus status,
        StorageKey rawKey,
        StorageKey finalKey,
        MediaOwner owner)
        : base(id, mediaData, AssetType.PREVIEW, status, rawKey, finalKey, owner)
    {
    }

    /// <summary>
    /// Для ef core.
    /// </summary>
    private PreviewAsset()
    {
    }

    public static UnitResult<Error> Validate(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
        {
            return GeneralErrors.ValueIsInvalid(
                $"File extension must be one of: {string.Join(", ", AllowedExtensions)}",
                "previewAsset.mediaData.fileName.extension");
        }

        if (mediaData.ContentType.Category != MediaType.IMAGE)
        {
            return GeneralErrors.ValueIsInvalid(
                $"File content type must be {ALLOWED_CONTENT_TYPE}",
                "previewAsset.mediaData.contentType.category");
        }

        if (mediaData.Size.Value > MAX_SIZE)
        {
            return GeneralErrors.ValueIsInvalid(
                $"File size must be less than {MAX_SIZE} bytes",
                "previewAsset.mediaData.size");
        }

        return UnitResult.Success<Error>();
    }

    public static Result<PreviewAsset, Error> CreateForUpload(MediaAssetId id, MediaData mediaData, MediaOwner owner)
    {
        UnitResult<Error> validationResult = Validate(mediaData);
        if (validationResult.IsFailure)
            return validationResult.Error;

        Result<StorageKey, Error> keyResult = StorageKey.Of(LOCATION, RAW_PREFIX, id.Value.ToString());

        if (keyResult.IsFailure)
            return keyResult.Error;

        return new PreviewAsset(id, mediaData, MediaStatus.UPLOADING, keyResult.Value, keyResult.Value, owner);
    }

    public UnitResult<Error> CompleteUpload(DateTime timestamp)
    {
        UnitResult<Error> markUploadedResult = MarkUploaded(timestamp);

        if (markUploadedResult.IsFailure)
            return markUploadedResult;

        UnitResult<Error> markReadyResult = MarkReady(RawKey, timestamp);

        if (markReadyResult.IsFailure)
            return markReadyResult;

        return UnitResult.Success<Error>();
    }
}