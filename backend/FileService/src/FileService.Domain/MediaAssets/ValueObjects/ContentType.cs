using CSharpFunctionalExtensions;
using FileService.Domain.MediaAssets.Enums;
using SharedService.SharedKernel;

namespace FileService.Domain.MediaAssets.ValueObjects;

/// <summary>
/// Тип контента.
/// </summary>
public sealed record ContentType
{
    private const string INVALID_FIELD = "contentType";

    private ContentType(string value, MediaType type)
    {
        Value = value;
        Category = type;
    }

    public string Value { get; private set; }

    public MediaType Category { get; private set; }

    /// <summary>
    /// Создает новый объект <see cref="ContentType"/>.
    /// </summary>
    /// <param name="contentType">Входящее значение.</param>
    /// <returns>Новый объект <see cref="ContentType"/> или ошибка <see cref="Error"/>.</returns>
    public static Result<ContentType, Error> Of(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return GeneralErrors.ValueIsRequired(INVALID_FIELD);

        MediaType category = contentType switch
        {
            _ when contentType.Contains("video", StringComparison.InvariantCultureIgnoreCase) => MediaType.VIDEO,
            _ when contentType.Contains("image", StringComparison.InvariantCultureIgnoreCase) => MediaType.IMAGE,
            _ when contentType.Contains("audio", StringComparison.InvariantCultureIgnoreCase) => MediaType.AUDIO,
            _ when contentType.Contains("document", StringComparison.InvariantCultureIgnoreCase) => MediaType.DOCUMENT,
            _ => MediaType.UNKNOWN
        };

        return new ContentType(contentType, category);
    }
}