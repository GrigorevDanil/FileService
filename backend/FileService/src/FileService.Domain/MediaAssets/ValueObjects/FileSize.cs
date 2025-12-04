using CSharpFunctionalExtensions;
using SharedService.SharedKernel;

namespace FileService.Domain.MediaAssets.ValueObjects;

public sealed record FileSize
{
    private const string INVALID_FIELD = "fileSize";

    private FileSize(long value)
    {
        Value = value;
    }

    public long Value { get; private set; }

    /// <summary>
    /// Создает новый объект <see cref="FileSize"/>.
    /// </summary>
    /// <param name="value">Входящее значение.</param>
    /// <returns>Новый объект <see cref="FileSize"/> или ошибка <see cref="Error"/>.</returns>
    public static Result<FileSize, Error> Of(long value)
    {
        if (value <= 0)
            return GeneralErrors.ValueIsInvalid(INVALID_FIELD);

        return new FileSize(value);
    }
}