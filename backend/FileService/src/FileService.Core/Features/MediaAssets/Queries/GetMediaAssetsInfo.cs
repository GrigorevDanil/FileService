using FileService.Contracts.MediaAssets.Dtos;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SharedService.Core.Handlers;
using SharedService.Framework.Endpoints;

namespace FileService.Core.Features.MediaAssets.Queries;

public sealed class GetMediaAssetsInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/files/batch", async Task<EndpointResult<IEnumerable<MediaAssetDto>>> (
                [FromQuery] Guid[] mediaAssetIds,
                [FromServices] GetMediaAssetsInfoHandler handler,
                CancellationToken cancellationToken) =>
            (await handler.Handle(new GetMediaAssetsInfoQuery(mediaAssetIds), cancellationToken)).ToArray());
    }
}

public sealed record GetMediaAssetsInfoQuery(Guid[] MediaAssetIds) : IQuery;

public sealed class GetMediaAssetsInfoHandler : IQueryHandler<GetMediaAssetsInfoQuery, IEnumerable<MediaAssetDto>>
{
    private readonly IS3Provider _s3Provider;
    private readonly IReadDbContext _readDbContext;

    public GetMediaAssetsInfoHandler(IS3Provider s3Provider, IReadDbContext readDbContext)
    {
        _s3Provider = s3Provider;
        _readDbContext = readDbContext;
    }

    public async Task<IEnumerable<MediaAssetDto>> Handle(
        GetMediaAssetsInfoQuery query,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var tempMediaAssets = await _readDbContext.MediaAssetsRead
            .Where(x => MediaAssetId.Of(query.MediaAssetIds).AsEnumerable().Contains(x.Id))
            .Select(ma => new
            {
                MediaAssetDto = new MediaAssetDto
                {
                    Id = ma.Id.Value,
                    Status = ma.Status.ToString(),
                    AssetType = ma.AssetType.ToString(),
                    CreatedAt = ma.CreatedAt,
                    UpdatedAt = ma.UpdatedAt,
                    MediaData = new MediaDataDto
                    {
                        ContentType = ma.MediaData.ContentType.Value,
                        FileName = ma.MediaData.FileName.Name,
                        Size = ma.MediaData.Size.Value
                    },
                },
                ma.RawKey
            })
            .ToListAsync(cancellationToken);

        IEnumerable<StorageKey> keys = tempMediaAssets.Select(x => x.RawKey);

        IReadOnlyList<string> downloadUrls = (await _s3Provider.GenerateDownloadUrlsAsync(keys, cancellationToken)).Value;

        for (int i = 0; i < tempMediaAssets.Count; i++)
        {
            if (tempMediaAssets[i].MediaAssetDto.Status == nameof(MediaStatus.READY))
                tempMediaAssets[i].MediaAssetDto.DownloadUrl = downloadUrls[i];
        }

        return tempMediaAssets.Select(x => x.MediaAssetDto);
    }
}