using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Requests;
using FileService.Core.Features.MediaAssets.Queries;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SharedService.Core.Handlers;
using SharedService.Core.Validation;
using SharedService.Framework.Endpoints;
using SharedService.SharedKernel;

namespace FileService.Core.Features.MediaAssets.UseCases;

public sealed class GetUploadUrlEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPut("/files/upload/url", async Task<EndpointResult<string>> (
                [FromBody] GetUploadUrlRequest request,
                [FromServices] GetUploadUrlHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new GetUploadUrlCommand(request), cancellationToken));
    }
}

public sealed class GetUploadUrlValidator : AbstractValidator<GetUploadUrlCommand>
{
    public GetUploadUrlValidator()
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

public sealed record GetUploadUrlCommand(GetUploadUrlRequest Request) : ICommand;

public sealed class GetUploadUrlHandler : ICommandHandler<GetUploadUrlCommand, string>
{
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<GetUploadUrlCommand> _validator;
    private readonly ILogger<GetDownloadUrlHandler> _logger;

    public GetUploadUrlHandler(
        IS3Provider s3Provider,
        IValidator<GetUploadUrlCommand> validator,
        ILogger<GetDownloadUrlHandler> logger)
    {
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string, Errors>> Handle(GetUploadUrlCommand command, CancellationToken cancellationToken = default)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var mediaAssetId = MediaAssetId.Create();

        MediaData mediaData = new(
            FileName.Of(command.Request.FileName).Value,
            ContentType.Of(command.Request.ContentType).Value,
            FileSize.Of(command.Request.FileSize).Value,
            ExpectedChunksCount.Of(1).Value);

        MediaOwner mediaOwner = MediaOwner.Of(command.Request.Context, command.Request.EntityId).Value;

        Result<MediaAsset, Error> mediaAssetResult = MediaAsset.CreateForUpload(mediaAssetId, mediaData, command.Request.AssetType.ToAssetType(), mediaOwner);

        if (mediaAssetResult.IsFailure)
            return mediaAssetResult.Error.ToErrors();

        MediaAsset mediaAsset = mediaAssetResult.Value;

        Result<string, Error> generateResult = await _s3Provider.GenerateUploadUrlAsync(mediaAsset.RawKey, mediaData, cancellationToken);

        if (generateResult.IsFailure)
            return generateResult.Error.ToErrors();

        _logger.LogInformation("Upload url was generated for file {FileName}", command.Request.FileName);

        return generateResult.Value;
    }
}