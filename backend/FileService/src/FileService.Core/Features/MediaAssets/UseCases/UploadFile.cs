using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Requests;
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

public sealed class UploadFileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/files/upload", async Task<EndpointResult<Guid>> (
            [FromForm] UploadFileRequest request,
            [FromServices] UploadFileHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(new UploadFileCommand(request), cancellationToken));
    }
}

public sealed class UploadFileValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileValidator()
    {
        RuleFor(x => x.Request.AssetType)
            .Must(at => Enum.IsDefined(typeof(AssetType), at.ToUpperInvariant()))
            .WithError(GeneralErrors.ValueIsInvalid("Asset type not valid", "assetType"));

        RuleFor(x => x.Request).MustBeValueObject(r => MediaOwner.Of(r.Context, r.EntityId));

        RuleFor(x => x.Request.File.FileName).MustBeValueObject(FileName.Of);
        RuleFor(x => x.Request.File.ContentType).MustBeValueObject(ContentType.Of);
        RuleFor(x => x.Request.File.Length).MustBeValueObject(FileSize.Of);
    }
}

public sealed record UploadFileCommand(UploadFileRequest Request) : ICommand;

public sealed class UploadFileHandler : ICommandHandler<UploadFileCommand, Guid>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<UploadFileCommand> _validator;
    private readonly ILogger<UploadFileHandler> _logger;

    public UploadFileHandler(
        IMediaRepository mediaRepository,
        ITransactionManager transactionManager,
        IS3Provider s3Provider,
        IValidator<UploadFileCommand> validator,
        ILogger<UploadFileHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _transactionManager = transactionManager;
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> Handle(UploadFileCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var mediaAssetId = MediaAssetId.Create();

        MediaData mediaData = new(
            FileName.Of(command.Request.File.FileName).Value,
            ContentType.Of(command.Request.File.ContentType).Value,
            FileSize.Of(command.Request.File.Length).Value,
            ExpectedChunksCount.Of(1).Value);

        MediaOwner mediaOwner = MediaOwner.Of(command.Request.Context, command.Request.EntityId).Value;

        Result<MediaAsset, Error> mediaAssetResult = MediaAsset.CreateForUpload(mediaAssetId, mediaData, command.Request.AssetType.ToAssetType(), mediaOwner);

        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error.ToErrors();

        MediaAsset mediaAsset = mediaAssetResult.Value;

        await _mediaRepository.AddAsync(mediaAsset, cancellationToken);

        UnitResult<Error> commitedResult = await _transactionManager.SaveChangesAsyncWithResult(cancellationToken);

        if (commitedResult.IsFailure)
            return commitedResult.Error.ToErrors();

        UnitResult<Error> uploadResult = await _s3Provider.UploadFileAsync(mediaAsset.RawKey, command.Request.File.OpenReadStream(), mediaData, cancellationToken);

        if (uploadResult.IsFailure)
        {
            mediaAsset.MarkFailed(DateTime.UtcNow);

            UnitResult<Error> commitedResultAfterFailedUpload = await _transactionManager.SaveChangesAsyncWithResult(cancellationToken);

            if (commitedResultAfterFailedUpload.IsFailure)
                return commitedResultAfterFailedUpload.Error.ToErrors();

            return uploadResult.Error.ToErrors();
        }

        mediaAsset.MarkUploaded(DateTime.UtcNow);

        UnitResult<Error> commitedResultAfterSuccessUpload = await _transactionManager.SaveChangesAsyncWithResult(cancellationToken);

        if (commitedResultAfterSuccessUpload.IsFailure)
            return commitedResultAfterSuccessUpload.Error.ToErrors();

        _logger.LogInformation("File {FileName} was successfully uploaded", command.Request.File.FileName);

        return mediaAssetId.Value;
    }
}
