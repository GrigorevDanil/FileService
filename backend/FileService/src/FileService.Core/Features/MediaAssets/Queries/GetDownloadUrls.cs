using CSharpFunctionalExtensions;
using FileService.Core.MediaAssets;
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

namespace FileService.Core.Features.MediaAssets.Queries;

public class GetDownloadUrlsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/files/download/urls", async Task<EndpointResult<IEnumerable<string>>> (
                [FromQuery] string[] paths,
                [FromServices] GetDownloadUrlsHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new GetDownloadUrlsCommand(paths), cancellationToken));
    }
}

public class GetDownloadUrlsValidator : AbstractValidator<GetDownloadUrlsCommand>
{
    public GetDownloadUrlsValidator()
    {
        RuleForEach(x => x.Paths)
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

public record GetDownloadUrlsCommand(string[] Paths) : ICommand;

public sealed class GetDownloadUrlsHandler : ICommandHandler<GetDownloadUrlsCommand, IEnumerable<string>>
{
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<GetDownloadUrlsCommand> _validator;
    private readonly ILogger<GetDownloadUrlsHandler> _logger;

    public GetDownloadUrlsHandler(
        IS3Provider s3Provider,
        IValidator<GetDownloadUrlsCommand> validator,
        ILogger<GetDownloadUrlsHandler> logger)
    {
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<string>, Errors>> Handle(GetDownloadUrlsCommand command, CancellationToken cancellationToken = default)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(command, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        List<string> urls = [];

        foreach (string path in command.Paths)
        {
            (string location, string? prefix, string key) = PathParser.ParsePath(path);

            StorageKey storageKey = StorageKey.Of(location, prefix, key).Value;

            Result<string, Error> getResult = await _s3Provider.GenerateDownloadUrlAsync(storageKey);

            if (getResult.IsFailure)
                return getResult.Error.ToErrors();

            urls.Add(getResult.Value);
        }

        _logger.LogInformation("Download url was generated for paths {Paths}", string.Join(", ", command.Paths));

        return urls;
    }
}