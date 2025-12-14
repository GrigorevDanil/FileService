namespace FileService.Contracts.MediaAssets.Dtos;

public record MediaAssetDto
{
    public Guid Id { get; init; }

    public string Status { get; init; } = string.Empty;

    public string AssetType { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public required MediaDataDto MediaData { get; init; }

    public string? DownloadUrl { get; set; }
}