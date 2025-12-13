using Microsoft.AspNetCore.Http;

namespace FileService.Contracts.MediaAssets.Requests;

public record UploadFileRequest(IFormFile File, string Context, Guid EntityId, string AssetType);