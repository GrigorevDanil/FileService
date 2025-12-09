using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.ValueObjects;
using SharedService.SharedKernel;

namespace FileService.Core.Features.MediaAssets;

public interface IMediaRepository
{
    Task<MediaAssetId> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default);

    Task<Result<MediaAsset, Error>> GetBy(Expression<Func<MediaAsset, bool>> predicate, CancellationToken cancellationToken = default);
}