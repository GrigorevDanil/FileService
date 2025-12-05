namespace FileService.Domain.MediaAssets.Enums;

/// <summary>
/// Тип файла сервиса.
/// </summary>
public enum AssetType
{
    /// <summary>Видео.</summary>
    VIDEO,

    /// <summary>Превьюхи.</summary>
    PREVIEW,
}

public static class AssetTypeExtensions
{
    public static AssetType ToAssetType(this string value)
    {
        return value switch
        {
            "video" => AssetType.VIDEO,
            "preview" => AssetType.PREVIEW,
            _ => throw new ArgumentException($"Invalid asset type: {value}")
        };
    }
}