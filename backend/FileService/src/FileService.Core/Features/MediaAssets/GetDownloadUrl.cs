using CSharpFunctionalExtensions;
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

namespace FileService.Core.Features.MediaAssets;

public class GetDownloadUrlEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/files/download/url/{path}", async Task<EndpointResult<string>> (
                [FromRoute] string path,
                [FromServices] GetDownloadUrlHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new GetDownloadUrlCommand(path), cancellationToken)).DisableAntiforgery();
    }
}

public class GetDownloadUrlValidator : AbstractValidator<GetDownloadUrlCommand>
{
    public GetDownloadUrlValidator()
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

public record GetDownloadUrlCommand(string Path) : ICommand;

public class GetDownloadUrlHandler : ICommandHandler<GetDownloadUrlCommand, string>
{
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<GetDownloadUrlCommand> _validator;
    private readonly ILogger<GetDownloadUrlHandler> _logger;

    public GetDownloadUrlHandler(
        IS3Provider s3Provider,
        IValidator<GetDownloadUrlCommand> validator,
        ILogger<GetDownloadUrlHandler> logger)
    {
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string, Errors>> Handle(GetDownloadUrlCommand command, CancellationToken cancellationToken = default)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        (string location, string? prefix, string key) = PathParser.ParsePath(command.Path);

        StorageKey storageKey = StorageKey.Of(location, prefix, key).Value;

        Result<string, Error> getResult = await _s3Provider.GenerateDownloadUrlAsync(storageKey);

        if (getResult.IsFailure)
            return getResult.Error.ToErrors();

        _logger.LogInformation("Download url was generated for path {Path}", command.Path);

        return getResult.Value;
    }
}