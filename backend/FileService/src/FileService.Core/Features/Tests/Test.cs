using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using SharedService.Framework.Endpoints;

namespace FileService.Core.Features.Tests;

public sealed class TestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/test", (ILogger<TestEndpoint> logger) =>
        {
            logger.LogInformation("Test endpoint");
        });
    }
}