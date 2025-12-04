namespace FileService.Domain.MediaAssets.Enums;

/// <summary>
/// Статус медиа.
/// </summary>
public enum MediaStatus
{
    /// <summary>Загружается.</summary>
    UPLOADING,

    /// <summary>Загружено.</summary>
    UPLOADED,

    /// <summary>Готов.</summary>
    READY,

    /// <summary>Произошла ошибка.</summary>
    FAILED,

    /// <summary>Удален.</summary>
    DELETED
}