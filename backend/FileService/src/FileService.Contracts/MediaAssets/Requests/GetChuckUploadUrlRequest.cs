namespace FileService.Contracts.MediaAssets.Requests;

public record GetChuckUploadUrlRequest(Guid MediaAssetId, string UploadId, int PartNumber);