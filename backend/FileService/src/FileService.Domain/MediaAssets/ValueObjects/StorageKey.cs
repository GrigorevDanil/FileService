using CSharpFunctionalExtensions;
using SharedService.SharedKernel;

namespace FileService.Domain.MediaAssets.ValueObjects;

public sealed record StorageKey
{
    private StorageKey(string location, string prefix, string key)
    {
        Key = key;
        Prefix = prefix;
        Location = location;
        Value = string.IsNullOrEmpty(prefix) ? key : $"{prefix}/{key}";
        FullPath = $"{location}/{Value}";
    }

    public static StorageKey None => new (string.Empty, string.Empty, string.Empty);

    public string Key { get; }

    public string Prefix { get; }

    public string Location { get; }

    public string Value { get; }

    public string FullPath { get; }

    /// <summary>
    /// Создает новый объект <see cref="StorageKey"/>.
    /// </summary>
    /// <param name="location">Место хранения.</param>
    /// <param name="prefix">Префикс.</param>
    /// <param name="key">Ключ.</param>
    /// <returns>Новый объект <see cref="StorageKey"/> или ошибка <see cref="Error"/>.</returns>
    public static Result<StorageKey, Error> Of(string location, string? prefix, string key)
    {
        if (string.IsNullOrWhiteSpace(location))
            return GeneralErrors.ValueIsInvalid("storageKey.location");

        Result<string, Error> normalizeKeyResult = NormalizeSegment(key);

        if (normalizeKeyResult.IsFailure)
            return normalizeKeyResult.Error;

        Result<string, Error> normalizePrefixResult = NormalizePrefix(prefix);

        if (normalizePrefixResult.IsFailure)
            return normalizePrefixResult.Error;

        return new StorageKey(location.Trim(), normalizePrefixResult.Value, normalizeKeyResult.Value);
    }

    /// <summary>
    /// Создает новый объект <see cref="StorageKey"/> с вложенным путем.
    /// </summary>
    /// <param name="segment">Сегмент.</param>
    /// <returns>Новый объект <see cref="StorageKey"/> или ошибка <see cref="Error"/>.</returns>
    public Result<StorageKey, Error> AppendSegment(string segment)
    {
        return Of(Location, Prefix, Key + "/" + segment);
    }

    private static Result<string, Error> NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return string.Empty;

        string[] segments = prefix.Trim().Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<string> normalizedSegments = [];
        foreach (string segment in segments)
        {
            Result<string, Error> normalizeSegmentResult = NormalizeSegment(segment);

            if (normalizeSegmentResult.IsFailure)
                return normalizeSegmentResult;

            if (!string.IsNullOrEmpty(normalizeSegmentResult.Value))
                normalizedSegments.Add(normalizeSegmentResult.Value);
        }

        return string.Join("/", normalizedSegments);
    }

    private static Result<string, Error> NormalizeSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string trimmed = value.Trim();

        if (trimmed.Contains('/', StringComparison.Ordinal) || trimmed.Contains('\\', StringComparison.Ordinal))
            return GeneralErrors.ValueIsInvalid("storageKey.key");

        return trimmed;
    }
}