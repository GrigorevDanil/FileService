using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Dtos;
using FileService.Contracts.MediaAssets.Requests;
using FileService.Core.MediaAssets;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SharedService.Core.Handlers;
using SharedService.Core.Validation;
using SharedService.Framework.Endpoints;
using SharedService.SharedKernel;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace FileService.Core.Features.MediaAssets.Queries;

public sealed class GetChuckUploadUrlEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/files/multipart/url", async Task<EndpointResult<string>> (
                [AsParameters] GetChuckUploadUrlRequest request,
                [FromServices] GetChuckUploadUrlHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new GetChuckUploadUrlCommand(request), cancellationToken));
    }
}

public sealed class GetChuckUploadUrlValidator : AbstractValidator<GetChuckUploadUrlCommand>
{
    public GetChuckUploadUrlValidator()
    {
        RuleFor(x => x.Request.PartNumber)
            .Must(pn => pn > 0)
            .WithError(GeneralErrors.ValueIsInvalid("PartNumber must  be greater than one"));
    }
}

public sealed record GetChuckUploadUrlCommand(GetChuckUploadUrlRequest Request) : ICommand;

public sealed class GetChuckUploadUrlHandler : ICommandHandler<GetChuckUploadUrlCommand, string>
{
    private readonly IS3Provider _s3Provider;
    private readonly IMediaRepository _mediaRepository;
    private readonly IValidator<GetChuckUploadUrlCommand> _validator;
    private readonly ILogger<GetDownloadUrlHandler> _logger;

    public GetChuckUploadUrlHandler(
        IS3Provider s3Provider,
        IMediaRepository mediaRepository,
        IValidator<GetChuckUploadUrlCommand> validator,
        ILogger<GetDownloadUrlHandler> logger)
    {
        _s3Provider = s3Provider;
        _mediaRepository = mediaRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string, Errors>> Handle(GetChuckUploadUrlCommand command, CancellationToken cancellationToken = default)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        var mediaAssetId = MediaAssetId.Of(command.Request.MediaAssetId);

        Result<MediaAsset, Error> getMediaAssetResult = await _mediaRepository.GetBy(x => x.Id == mediaAssetId, cancellationToken);

        if (getMediaAssetResult.IsFailure)
            return getMediaAssetResult.Error.ToErrors();

        MediaAsset mediaAsset = getMediaAssetResult.Value;

        Result<ChunkUploadUrl, Error> generateChunkUploadUrlResult = await _s3Provider.GenerateChunkUploadUrl(mediaAsset.RawKey, command.Request.UploadId, command.Request.PartNumber);

        if (generateChunkUploadUrlResult.IsFailure)
            return generateChunkUploadUrlResult.Error.ToErrors();

        return generateChunkUploadUrlResult.Value.UploadUrl;
    }
}