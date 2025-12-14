using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Dtos;
using FileService.Core.Models;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SharedService.Core.Handlers;
using SharedService.Framework.Endpoints;
using SharedService.SharedKernel;

namespace FileService.Core.Features.MediaAssets.Queries;

public sealed class GetMediaAssetsInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/files/batch", async Task<EndpointResult<IEnumerable<MediaAssetDto>>> (
                [FromQuery] Guid[] mediaAssetIds,
                [FromServices] GetMediaAssetsInfoHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new GetMediaAssetsInfoQuery(mediaAssetIds), cancellationToken));
    }
}

public sealed record GetMediaAssetsInfoQuery(Guid[] MediaAssetIds) : IQuery;

public sealed class GetMediaAssetsInfoHandler : IQueryHandlerWithResult<GetMediaAssetsInfoQuery, IEnumerable<MediaAssetDto>>
{
    private readonly IS3Provider _s3Provider;
    private readonly IReadDbContext _readDbContext;

    public GetMediaAssetsInfoHandler(IS3Provider s3Provider, IReadDbContext readDbContext)
    {
        _s3Provider = s3Provider;
        _readDbContext = readDbContext;
    }

    public async Task<Result<IEnumerable<MediaAssetDto>, Errors>> Handle(
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

        var readyTempMediaAssets = tempMediaAssets
            .Where(x => x.MediaAssetDto.Status == nameof(MediaStatus.READY)).ToList();

        IEnumerable<StorageKey> keys = readyTempMediaAssets.Select(x => x.RawKey);

        Result<IReadOnlyList<MediaUrl>, Errors> generateUrlsResult = await _s3Provider.GenerateDownloadUrlsAsync(keys, cancellationToken);

        if (generateUrlsResult.IsFailure)
            return generateUrlsResult.Error;

        Dictionary<StorageKey, string> urlsDict = generateUrlsResult.Value.ToDictionary(x => x.Key, x => x.PresignedUrl);

        foreach (var tempMediaAsset in tempMediaAssets)
        {
            urlsDict.TryGetValue(tempMediaAsset.RawKey, out string? downloadUrl);

            if (downloadUrl is not null)
                tempMediaAsset.MediaAssetDto.DownloadUrl = downloadUrl;
        }

        return Result.Success<IEnumerable<MediaAssetDto>, Errors>(tempMediaAssets.Select(x => x.MediaAssetDto));
    }
}