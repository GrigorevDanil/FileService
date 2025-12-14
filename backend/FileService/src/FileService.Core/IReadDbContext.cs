using FileService.Domain.MediaAssets;

namespace FileService.Core;

public interface IReadDbContext
{
    IQueryable<MediaAsset> MediaAssetsRead { get; }
}