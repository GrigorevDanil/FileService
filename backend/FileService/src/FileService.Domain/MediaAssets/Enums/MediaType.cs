namespace FileService.Domain.MediaAssets.Enums;

/// <summary>
/// Тип медиа файла.
/// </summary>
public enum MediaType
{
    /// <summary>Неизвестно.</summary>
    UNKNOWN,

    /// <summary>Видео.</summary>
    VIDEO,

    /// <summary>Изображение.</summary>
    IMAGE,

    /// <summary>Аудио.</summary>
    AUDIO,

    /// <summary>Документ.</summary>
    DOCUMENT
}