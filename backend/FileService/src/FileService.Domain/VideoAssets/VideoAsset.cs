using CSharpFunctionalExtensions;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using SharedService.SharedKernel;

namespace FileService.Domain.VideoAssets;

public class VideoAsset : MediaAsset
{
    public const long MAX_SIZE = 5_368_709_120;
    public const string LOCATION = "videos";
    public const string HLS_PREFIX = "hls";
    public const string RAW_PREFIX = "raw";
    public const string MASTER_PLAYLIST_NAME = "master.m3u8";
    public const string ALLOWED_CONTENT_TYPE = "video";

    public static readonly string[] AllowedExtensions = ["mp4", "mkv", "avi", "mov"];

    public VideoAsset(
        MediaAssetId id,
        MediaData mediaData,
        MediaStatus status,
        StorageKey rawKey,
        StorageKey finalKey,
        StorageKey hlsRootKey,
        MediaOwner owner)
        : base(id, mediaData, AssetType.VIDEO, status, rawKey, finalKey, owner)
    {
        HlsRootKey = hlsRootKey;
    }

    public StorageKey HlsRootKey { get; private set; }

    public static UnitResult<Error> Validate(MediaData mediaData)
    {
        if (!AllowedExtensions.Contains(mediaData.FileName.Extension))
        {
            return GeneralErrors.ValueIsInvalid(
                $"File extension must be one of: {string.Join(", ", AllowedExtensions)}",
                "videoAsset.mediaData.fileName.extension");
        }

        if (mediaData.ContentType.Category != MediaType.VIDEO)
        {
            return GeneralErrors.ValueIsInvalid(
                $"File content type must be {ALLOWED_CONTENT_TYPE}",
                "videoAsset.mediaData.contentType.category");
        }

        if (mediaData.Size.Value > MAX_SIZE)
        {
            return GeneralErrors.ValueIsInvalid(
                $"File size must be less than {MAX_SIZE} bytes",
                "videoAsset.mediaData.size");
        }

        return UnitResult.Success<Error>();
    }

    public static Result<VideoAsset, Error> CreateForUpload(MediaAssetId id, MediaData mediaData, MediaOwner owner)
    {
        UnitResult<Error> validationResult = Validate(mediaData);

        if (validationResult.IsFailure)
            return validationResult.Error;

        Result<StorageKey, Error> rawKeyResult = StorageKey.Of(LOCATION, RAW_PREFIX, id.Value.ToString());

        if (rawKeyResult.IsFailure)
            return rawKeyResult.Error;

        Result<StorageKey, Error> hlsKeyResult = StorageKey.Of(LOCATION, HLS_PREFIX, id.Value.ToString());

        if (hlsKeyResult.IsFailure)
            return hlsKeyResult.Error;

        return new VideoAsset(id, mediaData, MediaStatus.UPLOADING,  rawKeyResult.Value, hlsKeyResult.Value, hlsKeyResult.Value, owner);
    }

    public UnitResult<Error> CompleteProcessing(DateTime timestamp)
    {
        Result<StorageKey, Error> appendResult = HlsRootKey.AppendSegment(MASTER_PLAYLIST_NAME);

        if (appendResult.IsFailure)
            return appendResult;

        HlsRootKey = appendResult.Value;

        UnitResult<Error> markResult = MarkReady(HlsRootKey, timestamp);

        if (markResult.IsFailure)
            return markResult;

        return UnitResult.Success<Error>();
    }
}