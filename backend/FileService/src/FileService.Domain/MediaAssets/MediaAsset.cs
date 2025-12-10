using CSharpFunctionalExtensions;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using FileService.Domain.PreviewAssets;
using FileService.Domain.VideoAssets;
using SharedService.SharedKernel;

namespace FileService.Domain.MediaAssets;

public abstract class MediaAsset : BaseEntity<MediaAssetId>
{
    public MediaData MediaData { get; protected set; } = null!;

    public AssetType AssetType { get; protected set; }

    public MediaStatus Status { get; protected set; }

    public StorageKey RawKey { get; protected set; } = null!;

    public StorageKey FinalKey { get; protected set; } = null!;

    public MediaOwner Owner { get; protected set; } = null!;

    protected MediaAsset(
        MediaAssetId id,
        MediaData mediaData,
        AssetType assetType,
        MediaStatus status,
        StorageKey rawKey,
        StorageKey finalKey,
        MediaOwner owner)
    {
        Id = id;
        MediaData = mediaData;
        AssetType = assetType;
        Status = status;
        RawKey = rawKey;
        FinalKey = finalKey;
        Owner = owner;
    }

    /// <summary>
    /// Для ef core.
    /// </summary>
    protected MediaAsset()
    {
    }

    public static Result<MediaAsset, Error> CreateForUpload(MediaAssetId id, MediaData mediaData, AssetType assetType, MediaOwner owner)
    {
        switch (assetType)
        {
            case AssetType.VIDEO:
                    Result<VideoAsset, Error> videoAssetResult = VideoAsset.CreateForUpload(id,  mediaData, owner);
                    return videoAssetResult.IsFailure ? videoAssetResult.Error : videoAssetResult.Value;
            case AssetType.PREVIEW:
                    Result<PreviewAsset, Error> previewAssetResult = PreviewAsset.CreateForUpload(id,  mediaData, owner);
                    return previewAssetResult.IsFailure ? previewAssetResult.Error : previewAssetResult.Value;
            default: throw new ArgumentOutOfRangeException(nameof(assetType), assetType, null);
        }
    }

    /// <summary>
    /// Помечает медиа файл как загруженный.
    /// </summary>
    /// <param name="date">Дата изменения.</param>
    /// <returns>Результат пометки.</returns>
    public UnitResult<Error> MarkUploaded(DateTime date)
    {
        if (Status is MediaStatus.UPLOADING)
        {
            Status = MediaStatus.UPLOADED;
            UpdatedAt = date;
            return UnitResult.Success<Error>();
        }

        return GeneralErrors.ValueIsInvalid("Media asset can be marked as deleted only if its status is uploading", "mediaAsset.status");
    }

    /// <summary>
    /// Помечает медиа файл как готовый.
    /// </summary>
    /// <param name="finalKey">Финальный ключ.</param>
    /// <param name="date">Дата изменения.</param>
    /// <returns>Результат пометки.</returns>
    public UnitResult<Error> MarkReady(StorageKey finalKey, DateTime date)
    {
        if (Status is MediaStatus.UPLOADED)
        {
            FinalKey = finalKey;
            Status = MediaStatus.FAILED;
            UpdatedAt = date;
            return UnitResult.Success<Error>();
        }

        return GeneralErrors.ValueIsInvalid("Media asset can be marked as deleted only if its status is uploaded", "mediaAsset.status");
    }

    /// <summary>
    /// Помечает медиа файл с ошибкой загрузки.
    /// </summary>
    /// <param name="date">Дата изменения.</param>
    /// <returns>Результат пометки.</returns>
    public UnitResult<Error> MarkFailed(DateTime date)
    {
        if (Status is MediaStatus.UPLOADED or MediaStatus.UPLOADING)
        {
            Status = MediaStatus.FAILED;
            UpdatedAt = date;
            return UnitResult.Success<Error>();
        }

        return GeneralErrors.ValueIsInvalid("Media asset can be marked as deleted only if its status is uploaded or uploading", "mediaAsset.status");
    }

    /// <summary>
    /// Помечает медиа файл как удаленный.
    /// </summary>
    /// <param name="date">Дата изменения.</param>
    /// <returns>Результат пометки.</returns>
    public UnitResult<Error> MarkDeleted(DateTime date)
    {
        if (Status is MediaStatus.READY or MediaStatus.FAILED or MediaStatus.UPLOADED)
        {
            Status = MediaStatus.DELETED;
            UpdatedAt = date;
            return UnitResult.Success<Error>();
        }

        return GeneralErrors.ValueIsInvalid("Media asset can be marked as deleted only if its status is ready, failed or uploaded", "mediaAsset.status");
    }
}
