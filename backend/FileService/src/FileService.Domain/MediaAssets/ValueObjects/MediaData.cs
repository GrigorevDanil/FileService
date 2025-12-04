namespace FileService.Domain.MediaAssets.ValueObjects;

/// <summary>
/// Данные о медиа файле.
/// </summary>
public sealed record MediaData
{
    public MediaData(FileName fileName, ContentType contentType, FileSize size, ExpectedChunksCount expectedChunksCount)
    {
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        ExpectedChunksCount = expectedChunksCount;
    }

    public FileName FileName { get; private set; } = null!;

    public ContentType ContentType { get; private set; } = null!;

    public FileSize Size { get; } = null!;

    public ExpectedChunksCount ExpectedChunksCount { get; } = null!;
}