using Microsoft.AspNetCore.Http;

namespace FileService.Contracts.MediaAssets;

public record UploadFileRequest(IFormFile File, string Context, string AssetType);