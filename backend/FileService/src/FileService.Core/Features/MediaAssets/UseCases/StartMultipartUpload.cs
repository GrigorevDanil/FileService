using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Dtos;
using FileService.Contracts.MediaAssets.Requests;
using FileService.Contracts.MediaAssets.Responses;
using FileService.Core.MediaAssets;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SharedService.Core.Database;
using SharedService.Core.Handlers;
using SharedService.Core.Validation;
using SharedService.Framework.Endpoints;
using SharedService.SharedKernel;

namespace FileService.Core.Features.MediaAssets.UseCases;

public sealed class StartMultipartUploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/files/multipart/start", async Task<EndpointResult<StartMultipartUploadResponse>> (
                [FromBody] StartMultipartUploadRequest request,
                [FromServices] StartMultipartUploadHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new StartMultipartUploadCommand(request), cancellationToken));
    }
}

public sealed class StartMultipartUploadValidator : AbstractValidator<StartMultipartUploadCommand>
{
    public StartMultipartUploadValidator()
    {
        RuleFor(x => x.Request.AssetType)
            .Must(at => Enum.IsDefined(typeof(AssetType), at.ToUpperInvariant()))
            .WithError(GeneralErrors.ValueIsInvalid("Asset type not valid", "assetType"));

        RuleFor(x => x.Request).MustBeValueObject(r => MediaOwner.Of(r.Context, r.EntityId));

        RuleFor(x => x.Request.FileName).MustBeValueObject(FileName.Of);
        RuleFor(x => x.Request.ContentType).MustBeValueObject(ContentType.Of);
        RuleFor(x => x.Request.FileSize).MustBeValueObject(FileSize.Of);
    }
}

public sealed record StartMultipartUploadCommand(StartMultipartUploadRequest Request) : ICommand;

public sealed class StartMultipartUploadHandler : ICommandHandler<StartMultipartUploadCommand, StartMultipartUploadResponse>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IS3Provider _s3Provider;
    private readonly IChunkSizeCalculator _chunkSizeCalculator;
    private readonly IValidator<StartMultipartUploadCommand> _validator;
    private readonly ILogger<StartMultipartUploadHandler> _logger;

    public StartMultipartUploadHandler(
        IMediaRepository mediaRepository,
        ITransactionManager transactionManager,
        IS3Provider s3Provider,
        IChunkSizeCalculator chunkSizeCalculator,
        IValidator<StartMultipartUploadCommand> validator,
        ILogger<StartMultipartUploadHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _transactionManager = transactionManager;
        _s3Provider = s3Provider;
        _chunkSizeCalculator = chunkSizeCalculator;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<StartMultipartUploadResponse, Errors>> Handle(
        StartMultipartUploadCommand command,
        CancellationToken cancellationToken = new CancellationToken())
    {
        ValidationResult validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var mediaAssetId = MediaAssetId.Create();

        FileName fileName = FileName.Of(command.Request.FileName).Value;

        ContentType contentType = ContentType.Of(command.Request.ContentType).Value;

        FileSize fileSize = FileSize.Of(command.Request.FileSize).Value;

        Result<(int ChuckSize, int TotalChunks), Error> calculateChunkResult = _chunkSizeCalculator.Calculate(command.Request.FileSize);

        if (calculateChunkResult.IsFailure)
            return calculateChunkResult.Error.ToErrors();

        (int chuckSize, int totalChunks) = calculateChunkResult.Value;

        Result<ExpectedChunksCount, Error> expectedChunksCountResult = ExpectedChunksCount.Of(totalChunks);

        if (expectedChunksCountResult.IsFailure)
            return expectedChunksCountResult.Error.ToErrors();

        var mediaData = new MediaData(fileName, contentType, fileSize, expectedChunksCountResult.Value);

        MediaOwner mediaOwner = MediaOwner.Of(command.Request.Context, command.Request.EntityId).Value;

        Result<MediaAsset, Error> mediaAssetResult = MediaAsset.CreateForUpload(
            mediaAssetId,
            mediaData,
            command.Request.AssetType.ToAssetType(),
            mediaOwner);

        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error.ToErrors();

        MediaAsset mediaAsset = mediaAssetResult.Value;

        await _mediaRepository.AddAsync(mediaAsset, cancellationToken);

        UnitResult<Error> commitedResult = await _transactionManager.SaveChangesAsyncWithResult(cancellationToken);

        if (commitedResult.IsFailure)
            return commitedResult.Error.ToErrors();

        Result<string, Error> startMultipartUploadResult = await _s3Provider.StartMultipartUpload(mediaAsset.RawKey, mediaData, cancellationToken);

        if (startMultipartUploadResult.IsFailure)
        {
            UnitResult<Error> markResult = mediaAsset.MarkFailed(DateTime.UtcNow);

            if (markResult.IsFailure)
                return markResult.Error.ToErrors();

            UnitResult<Error> failedStartCommitedResult = await _transactionManager.SaveChangesAsyncWithResult(cancellationToken);

            if (failedStartCommitedResult.IsFailure)
                return failedStartCommitedResult.Error.ToErrors();

            return startMultipartUploadResult.Error.ToErrors();
        }

        string uploadId = startMultipartUploadResult.Value;

        Result<IReadOnlyList<ChunkUploadUrl>, Errors> generateAllChunkResult = await _s3Provider.GenerateAllChunkUploadUrl(
            mediaAsset.RawKey,
            uploadId,
            totalChunks,
            cancellationToken);

        if (generateAllChunkResult.IsFailure)
            return generateAllChunkResult.Error;

        _logger.LogInformation("Media asset by id {MediaAssetId} was created for multipart upload with key {Key}", mediaAssetId.Value, mediaAsset.RawKey.Value);

        return new StartMultipartUploadResponse(
            mediaAssetId.Value,
            uploadId,
            generateAllChunkResult.Value,
            chuckSize);
    }
}