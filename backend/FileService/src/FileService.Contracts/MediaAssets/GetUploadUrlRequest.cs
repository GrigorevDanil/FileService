using Microsoft.AspNetCore.Http;

namespace FileService.Contracts.MediaAssets;

public record GetUploadUrlRequest(string FileName, string ContentType, long FileSize, string Context, string AssetType);