using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Dtos;
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

public sealed class GetMediaAssetInfoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/files/{mediaAssetId::guid}", async Task<EndpointResult<MediaAssetDto?>> (
                [FromRoute] Guid mediaAssetId,
                [FromServices] GetMediaAssetInfoHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new GetMediaAssetInfoQuery(mediaAssetId), cancellationToken));
    }
}

public sealed record GetMediaAssetInfoQuery(Guid MediaAssetId) : IQuery;

public sealed class GetMediaAssetInfoHandler : IQueryHandlerWithResult<GetMediaAssetInfoQuery, MediaAssetDto?>
{
    private readonly IS3Provider _s3Provider;
    private readonly IReadDbContext _readDbContext;

    public GetMediaAssetInfoHandler(IS3Provider s3Provider, IReadDbContext readDbContext)
    {
        _s3Provider = s3Provider;
        _readDbContext = readDbContext;
    }

    public async Task<Result<MediaAssetDto?, Errors>> Handle(
        GetMediaAssetInfoQuery query,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var tempMediaAsset = await _readDbContext.MediaAssetsRead
            .Where(x => x.Id == MediaAssetId.Of(query.MediaAssetId))
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
            .FirstOrDefaultAsync(cancellationToken);

        if (tempMediaAsset is null)
            return GeneralErrors.NotFound(query.MediaAssetId).ToErrors();

        if (tempMediaAsset.MediaAssetDto.Status == nameof(MediaStatus.READY))
        {
            Result<string, Error> generateUrlResult = await _s3Provider.GenerateDownloadUrlAsync(tempMediaAsset.RawKey);

            if (generateUrlResult.IsFailure)
                return generateUrlResult.Error.ToErrors();

            tempMediaAsset.MediaAssetDto.DownloadUrl = generateUrlResult.Value;
        }

        return tempMediaAsset.MediaAssetDto;
    }
}