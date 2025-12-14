namespace FileService.Contracts.MediaAssets.Requests;

public record GetUploadUrlRequest(string FileName, string ContentType, long FileSize, string Context, Guid EntityId, string AssetType);