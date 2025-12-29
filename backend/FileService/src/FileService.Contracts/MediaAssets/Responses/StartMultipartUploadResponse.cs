using FileService.Contracts.MediaAssets.Dtos;

namespace FileService.Contracts.MediaAssets.Responses;

public record StartMultipartUploadResponse(Guid MediaAssetId, string UploadId, IReadOnlyList<ChunkUploadUrl> ChunkUploadUrls, int ChunkSize);