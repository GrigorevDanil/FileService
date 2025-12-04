namespace FileService.Domain.MediaAssets.ValueObjects;

/// <summary>
/// Уникальный идентификатор медиа файла.
/// </summary>
public record MediaAssetId
{
    private MediaAssetId(Guid value) => Value = value;

    public Guid Value { get; private set; }

    /// <summary>
    /// Создание нового идентификатора для медиа файла.
    /// </summary>
    /// <returns>Новый идентификатор медиа файла.</returns>
    public static MediaAssetId Create() => new(Guid.NewGuid());

    /// <summary>
    /// Создание идентификатора медиа файла из входящего идентификатора.
    /// </summary>
    /// <param name="locationId">Входящий идентификатор.</param>
    /// <returns>Идентификатор медиа файла.</returns>
    public static MediaAssetId Of(Guid locationId) => new(locationId);

    /// <summary>
    /// Создание идентификаторов медиа файлов из входящих идентификаторов.
    /// </summary>
    /// <param name="locationIds">Входящие идентификаторы.</param>
    /// <returns>Идентификаторы медиа файлов.</returns>
    public static MediaAssetId[] Of(Guid[] locationIds) => locationIds.Select(Of).ToArray();
}