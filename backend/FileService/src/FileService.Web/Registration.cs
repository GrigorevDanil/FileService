using System.Reflection;
using FileService.Core;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.S3;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SharedService.Framework.Endpoints;
using SharedService.Framework.Logging;
using SharedService.Framework.Middlewares;
using SharedService.Framework.Swagger;

namespace FileService.Web;

public static class Registration
{
    public static async Task<IApplicationBuilder> Configure(this WebApplication app)
    {
        app.UseExceptionMiddleware();
        app.UseRequestCorrelationId();
        app.UseSerilogRequestLogging();

        app.UseSwagger();
        app.UseSwaggerUI();

        if (app.Environment.IsDevelopment())
        {
            await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

            await scope.UseAutoMigrateAsync();
        }

        RouteGroupBuilder apiGroup = app.MapGroup("/api").DisableAntiforgery();

        app.MapEndpoints(apiGroup);

        return app;
    }

    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOpenApi()
            .AddSerilogLogging(configuration, "FileService")
            .AddCustomSwagger(configuration)
            .AddCore()
            .AddInfrastructurePostgres(configuration)
            .AddInfrastructureS3(configuration)
            .AddEndpoints(Assembly.Load("FileService.Core"));

        return services;
    }

    private static async Task UseAutoMigrateAsync(this AsyncServiceScope scope)
    {
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}