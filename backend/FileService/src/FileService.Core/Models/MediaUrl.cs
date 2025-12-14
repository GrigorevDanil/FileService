using FileService.Domain.MediaAssets.ValueObjects;

namespace FileService.Core.Models;

public record MediaUrl(StorageKey Key, string PresignedUrl);