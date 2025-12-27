namespace FileService.Contracts.MediaAssets.Dtos;

public record MediaDataDto
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long Size { get; init; }
}