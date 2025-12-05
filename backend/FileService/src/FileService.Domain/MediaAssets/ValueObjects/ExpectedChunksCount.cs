using CSharpFunctionalExtensions;
using SharedService.SharedKernel;

namespace FileService.Domain.MediaAssets.ValueObjects;

public sealed record ExpectedChunksCount
{
    private const string INVALID_FIELD = "expectedChunksCount";

    private ExpectedChunksCount(int value)
    {
        Value = value;
    }

    public int Value { get; private set; }

    /// <summary>
    /// Создает новый объект <see cref="ExpectedChunksCount"/>.
    /// </summary>
    /// <param name="value">Входящее значение.</param>
    /// <returns>Новый объект <see cref="ExpectedChunksCount"/> или ошибка <see cref="Error"/>.</returns>
    public static Result<ExpectedChunksCount, Error> Of(int value)
    {
        if (value <= 0)
            return GeneralErrors.ValueIsInvalid(INVALID_FIELD);

        return new ExpectedChunksCount(value);
    }
}