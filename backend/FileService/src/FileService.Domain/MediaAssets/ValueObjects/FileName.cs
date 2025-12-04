using CSharpFunctionalExtensions;
using SharedService.SharedKernel;

namespace FileService.Domain.MediaAssets.ValueObjects;

/// <summary>
/// Название файла.
/// </summary>
public sealed record FileName
{
    private const string INVALID_FIELD = "fileName";

    private FileName(string name, string extension)
    {
        Name = name;
        Extension = extension;
    }

    public string Name { get; private set; }

    public string Extension { get; private set; }

    /// <summary>
    /// Создает новый объект <see cref="FileName"/>.
    /// </summary>
    /// <param name="fileName">Входящее значение.</param>
    /// <returns>Новый объект <see cref="FileName"/> или ошибка <see cref="Error"/>.</returns>
    public static Result<FileName, Error> Of(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return GeneralErrors.ValueIsRequired(INVALID_FIELD);

        int lastDot = fileName.LastIndexOf('.');
        if (lastDot == -1 || lastDot == fileName.Length - 1)
            return GeneralErrors.ValueIsInvalid("File must have extension", INVALID_FIELD);

        string extension = fileName[(lastDot + 1)..].ToLowerInvariant();

        return new FileName(fileName, extension);
    }
}