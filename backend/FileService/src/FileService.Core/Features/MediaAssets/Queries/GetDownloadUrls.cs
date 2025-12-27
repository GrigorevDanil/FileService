using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Requests;
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

public class GetDownloadUrlsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/files/download/urls", async Task<EndpointResult<IEnumerable<string>>> (
                [AsParameters] GetDownloadUrlsRequest request,
                [FromServices] GetDownloadUrlsHandler handler,
                CancellationToken cancellationToken) =>
            await handler.Handle(new GetDownloadUrlsQuery(request), cancellationToken));
    }
}

public class GetDownloadUrlsValidator : AbstractValidator<GetDownloadUrlsQuery>
{
    public GetDownloadUrlsValidator()
    {
        RuleForEach(x => x.Request.Paths).MustBeValueObject(StorageKey.Of);
    }
}

public record GetDownloadUrlsQuery(GetDownloadUrlsRequest Request) : IQuery;

public sealed class GetDownloadUrlsHandler : IQueryHandlerWithResult<GetDownloadUrlsQuery, IEnumerable<string>>
{
    private readonly IS3Provider _s3Provider;
    private readonly IValidator<GetDownloadUrlsQuery> _validator;
    private readonly ILogger<GetDownloadUrlsHandler> _logger;

    public GetDownloadUrlsHandler(
        IS3Provider s3Provider,
        IValidator<GetDownloadUrlsQuery> validator,
        ILogger<GetDownloadUrlsHandler> logger)
    {
        _s3Provider = s3Provider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<string>, Errors>> Handle(GetDownloadUrlsQuery query, CancellationToken cancellationToken = default)
    {
        ValidationResult validationResult = await _validator.ValidateAsync(query, cancellationToken);

        if (!validationResult.IsValid)
            return validationResult.ToErrors();

        List<string> urls = [];

        foreach (string path in query.Request.Paths)
        {
            StorageKey storageKey = StorageKey.Of(path).Value;

            Result<string, Error> getResult = await _s3Provider.GenerateDownloadUrlAsync(storageKey);

            if (getResult.IsFailure)
                return getResult.Error.ToErrors();

            urls.Add(getResult.Value);
        }

        _logger.LogInformation("Download url was generated for paths {Paths}", string.Join(", ", query.Request.Paths));

        return urls;
    }
}