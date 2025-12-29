using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Requests;
using FileService.Core.MediaAssets;
using FileService.Domain.MediaAssets.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SharedService.Core.Handlers;
using SharedService.Core.Validation;
using SharedService.Framework.Endpoints;
using SharedService.SharedKernel;

namespace FileService.Core.Features.MediaAssets.Queries;

public sealed class GetDownloadUrlEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/files/download/url", async Task<EndpointResult<string>> (
                [AsParameters] GetDownloadUrlRequest request,
                [FromServices] GetDownloadUrlHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new GetDownloadUrlQuery(request), cancellationToken));
    }
}

public sealed class GetDownloadUrlValidator : AbstractValidator<GetDownloadUrlQuery>
{
    public GetDownloadUrlValidator()
    {
        RuleFor(x => x.Request.Path).MustBeValueObject(StorageKey.Of);
    }
}

public sealed record GetDownloadUrlQuery(GetDownloadUrlRequest Request) : IQuery;

public sealed class GetDownloadUrlHandler : IQueryHandlerWithResult<GetDownloadUrlQuery, string>
{
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<GetDownloadUrlQuery> _validator;
    private readonly ILogger<GetDownloadUrlHandler> _logger;

    public GetDownloadUrlHandler(
        IS3Provider s3Provider,
        IValidator<GetDownloadUrlQuery> validator,
        ILogger<GetDownloadUrlHandler> logger)
    {
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<string, Errors>> Handle(GetDownloadUrlQuery query, CancellationToken cancellationToken = default)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(query, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        StorageKey storageKey = StorageKey.Of(query.Request.Path).Value;

        Result<string, Error> getResult = await _s3Provider.GenerateDownloadUrlAsync(storageKey);

        if (getResult.IsFailure)
            return getResult.Error.ToErrors();

        _logger.LogInformation("Download url was generated for path {Path}", query.Request.Path);

        return getResult.Value;
    }
}