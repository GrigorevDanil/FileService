namespace FileService.Contracts.MediaAssets.Requests;

public record StartMultipartUploadRequest(
    string FileName,
    string ContentType,
    long FileSize,
    string AssetType,
    string Context,
    Guid EntityId);