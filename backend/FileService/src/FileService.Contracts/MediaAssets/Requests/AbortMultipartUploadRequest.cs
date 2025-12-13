namespace FileService.Contracts.MediaAssets.Requests;

public record AbortMultipartUploadRequest(Guid MediaAssetId, string UploadId);