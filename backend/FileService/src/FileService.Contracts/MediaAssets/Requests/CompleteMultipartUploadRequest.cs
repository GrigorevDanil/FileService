using FileService.Contracts.MediaAssets.Dtos;

namespace FileService.Contracts.MediaAssets.Requests;

public record CompleteMultipartUploadRequest(Guid MediaAssetId, string UploadId, IReadOnlyList<PartETagDto> PartETags);