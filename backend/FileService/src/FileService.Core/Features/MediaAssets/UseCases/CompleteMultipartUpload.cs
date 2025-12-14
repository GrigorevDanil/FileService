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

public sealed class CompleteMultipartUploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/files/multipart/complete", async Task<EndpointResult> (
                [FromBody] CompleteMultipartUploadRequest request,
                [FromServices] CompleteMultipartUploadHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new CompleteMultipartUploadCommand(request), cancellationToken));
    }
}

public sealed record CompleteMultipartUploadCommand(CompleteMultipartUploadRequest Request) : ICommand;

public sealed class CompleteMultipartUploadHandler : ICommandHandler<CompleteMultipartUploadCommand>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IS3Provider _s3Provider;
    private readonly ILogger<CompleteMultipartUploadHandler> _logger;

    public CompleteMultipartUploadHandler(
        IMediaRepository mediaRepository,
        ITransactionManager transactionManager,
        IS3Provider s3Provider,
        ILogger<CompleteMultipartUploadHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _transactionManager = transactionManager;
        _s3Provider = s3Provider;
        _logger = logger;
    }

    public async Task<UnitResult<Errors>> Handle(
        CompleteMultipartUploadCommand command,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var mediaAssetId = MediaAssetId.Of(command.Request.MediaAssetId);

        Result<MediaAsset, Error> getMediaAssetResult = await _mediaRepository.GetBy(ma => ma.Id == mediaAssetId, cancellationToken);

        if (getMediaAssetResult.IsFailure)
            return getMediaAssetResult.Error.ToErrors();

        MediaAsset mediaAsset = getMediaAssetResult.Value;

        if (mediaAsset.MediaData.ExpectedChunksCount.Value != command.Request.PartETags.Count)
            return GeneralErrors.Failure("The eTag count does not equal the chunk count.").ToErrors();

        Result<string, Error> completeResult = await _s3Provider.CompleteMultipartUploadAsync(
            mediaAsset.RawKey,
            command.Request.UploadId,
            command.Request.PartETags,
            cancellationToken);

        if (completeResult.IsFailure)
        {
            mediaAsset.MarkFailed(DateTime.UtcNow);

            UnitResult<Error> failedCommitedResult = await _transactionManager.SaveChangesAsyncWithResult(cancellationToken);

            if (failedCommitedResult.IsFailure)
                return failedCommitedResult.Error.ToErrors();

            return completeResult.Error.ToErrors();
        }

        UnitResult<Error> markUploadedResult = mediaAsset.MarkUploaded(DateTime.UtcNow);

        if (markUploadedResult.IsFailure)
            return markUploadedResult.Error.ToErrors();

        UnitResult<Error> commitedResult = await _transactionManager.SaveChangesAsyncWithResult(cancellationToken);

        if (commitedResult.IsFailure)
            return commitedResult.Error.ToErrors();

        _logger.LogInformation("Multipart upload complete for media asset by id {Id}",  mediaAssetId.Value);

        return UnitResult.Success<Errors>();
    }
}