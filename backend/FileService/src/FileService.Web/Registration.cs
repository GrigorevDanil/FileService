using System.Reflection;
using FileService.Core;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.S3;
using Serilog;
using SharedService.Framework.Endpoints;
using SharedService.Framework.Logging;
using SharedService.Framework.Middlewares;
using SharedService.Framework.Swagger;

namespace FileService.Web;

public static class Registration
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.UseRequestCorrelationId();
        app.UseSerilogRequestLogging();

        app.UseSwagger();
        app.UseSwaggerUI();

        RouteGroupBuilder apiGroup = app.MapGroup("/api");

        app.MapEndpoints(apiGroup);

        return app;
    }

    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOpenApi()
            .AddSerilogLogging(configuration, "FileService")
            .AddCustomSwagger(configuration)
            .AddInfrastructurePostgres(configuration)
            .AddInfrastructureS3(configuration)
            .AddEndpoints(Assembly.Load("FileService.Core"));

        return services;
    }
}