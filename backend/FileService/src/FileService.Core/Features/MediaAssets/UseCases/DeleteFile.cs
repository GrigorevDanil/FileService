using CSharpFunctionalExtensions;
using FileService.Core.MediaAssets;
using FileService.Domain.MediaAssets;
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

public sealed class DeleteFileEndpoint : IEndpoint
{
     public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapDelete("/files/delete/{path}", async Task<EndpointResult<Guid>> (
            [FromRoute] string path,
            [FromServices] DeleteFileHandler handler,
            CancellationToken cancellationToken) =>
            await handler.Handle(new DeleteFileCommand(path), cancellationToken));
    }
}

public sealed class DeleteFileValidator : AbstractValidator<DeleteFileCommand>
{
    public DeleteFileValidator()
    {
        RuleFor(x => x.Path)
            .NotEmpty()
            .WithError(GeneralErrors.ValueIsRequired("Path is required"))
            .Must(PathParser.BeValidPathStructure)
            .WithError(GeneralErrors.ValueIsInvalid("Path must have at least 2 parts separated by slashes", "path"))
            .MustBeValueObject(p =>
            {
                (string location, string? prefix, string key) = PathParser.ParsePath(p);
                return StorageKey.Of(location, prefix, key);
            });
    }
}

public sealed record DeleteFileCommand(string Path) : ICommand;

public sealed class DeleteFileHandler : ICommandHandler<DeleteFileCommand, Guid>
{
    private readonly IMediaRepository _mediaRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<DeleteFileCommand> _validator;
    private readonly ILogger<DeleteFileHandler> _logger;

    public DeleteFileHandler(
        IMediaRepository mediaRepository,
        ITransactionManager transactionManager,
        IS3Provider s3Provider,
        IValidator<DeleteFileCommand> validator,
        ILogger<DeleteFileHandler> logger)
    {
        _mediaRepository = mediaRepository;
        _transactionManager = transactionManager;
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> Handle(DeleteFileCommand command, CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        (string location, string? prefix, string key) = PathParser.ParsePath(command.Path);

        StorageKey storageKey = StorageKey.Of(location, prefix, key).Value;

        Result<MediaAsset, Error> getResult = await _mediaRepository.GetBy(x => x.RawKey == storageKey, cancellationToken);

        if (getResult.IsFailure)
            return getResult.Error.ToErrors();

        Result<string, Error> deleteResult = await _s3Provider.DeleteFileAsync(storageKey, cancellationToken);

        if (deleteResult.IsFailure)
            return deleteResult.Error.ToErrors();

        MediaAsset mediaAsset = getResult.Value;

        mediaAsset.MarkDeleted(DateTime.UtcNow);

        UnitResult<Error> commitedResult = await _transactionManager.SaveChangesAsyncWithResult(cancellationToken);

        if (commitedResult.IsFailure)
            return commitedResult.Error.ToErrors();

        _logger.LogInformation("Object by path {Path} was deleted", command.Path);

        return mediaAsset.Id.Value;
    }
}
