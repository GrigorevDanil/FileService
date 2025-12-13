using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Requests;
using FileService.Core.MediaAssets;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.ValueObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SharedService.Core.Database;
using SharedService.Core.Handlers;
using SharedService.Framework.Endpoints;
using SharedService.SharedKernel;

namespace FileService.Core.Features.MediaAssets.UseCases;

public class AbortMultipartUploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/files/multipart/abort", async Task<EndpointResult> (
                [FromBody] AbortMultipartUploadRequest request,
                [FromServices] AbortMultipartUploadHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new AbortMultipartUploadCommand(request), cancellationToken));
    }
}

public sealed record AbortMultipartUploadCommand(AbortMultipartUploadRequest Request) : ICommand;

public sealed class AbortMultipartUploadHandler : ICommandHandler<AbortMultipartUploadCommand>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IS3Provider _s3Provider;
    private readonly ILogger<AbortMultipartUploadHandler> _logger;

    public AbortMultipartUploadHandler(
        IMediaRepository mediaRepository,
        ITransactionManager transactionManager,
        IS3Provider s3Provider,
        ILogger<AbortMultipartUploadHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _transactionManager = transactionManager;
        _s3Provider = s3Provider;
        _logger = logger;
    }

    public async Task<UnitResult<Errors>> Handle(
        AbortMultipartUploadCommand command,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var mediaAssetId = MediaAssetId.Of(command.Request.MediaAssetId);

        Result<MediaAsset, Error> getMediaAssetResult = await _mediaRepository.GetBy(ma => ma.Id == mediaAssetId, cancellationToken);

        if (getMediaAssetResult.IsFailure)
            return getMediaAssetResult.Error.ToErrors();

        MediaAsset mediaAsset = getMediaAssetResult.Value;

        UnitResult<Error> abortResult = await _s3Provider.AbortMultipartUploadAsync(mediaAsset.RawKey, command.Request.UploadId, cancellationToken);

        if (abortResult.IsFailure)
            return abortResult.Error.ToErrors();

        UnitResult<Error> markResult = mediaAsset.MarkFailed(DateTime.UtcNow);

        if (markResult.IsFailure)
            return markResult.Error.ToErrors();

        UnitResult<Error> commitedResult = await _transactionManager.SaveChangesAsyncWithResult(cancellationToken);

        if (commitedResult.IsFailure)
            return commitedResult.Error.ToErrors();

        return UnitResult.Success<Errors>();
    }
}