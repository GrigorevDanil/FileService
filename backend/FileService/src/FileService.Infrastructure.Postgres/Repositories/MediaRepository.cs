using System.Linq.Expressions;
using CSharpFunctionalExtensions;
using FileService.Core.Features.MediaAssets;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.ValueObjects;
using Microsoft.EntityFrameworkCore;
using SharedService.SharedKernel;

namespace FileService.Infrastructure.Postgres.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly AppDbContext _dbContext;

    public MediaRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MediaAssetId> AddAsync(MediaAsset mediaAsset, CancellationToken cancellationToken = default)
    {
        await _dbContext.MediaAssets.AddAsync(mediaAsset, cancellationToken);
        return mediaAsset.Id;
    }

    public async Task<Result<MediaAsset, Error>> GetBy(
        Expression<Func<MediaAsset, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        MediaAsset? mediaAsset = await _dbContext.MediaAssets.FirstOrDefaultAsync(predicate, cancellationToken);

        if (mediaAsset is null)
            return GeneralErrors.NotFound();

        return mediaAsset;
    }
}