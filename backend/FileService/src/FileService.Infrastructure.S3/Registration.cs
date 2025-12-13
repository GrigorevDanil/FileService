using Amazon.S3;
using FileService.Core;
using FileService.Core.MediaAssets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public static class Registration
{
    public static IServiceCollection AddInfrastructureS3(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<S3Options>(configuration.GetSection(nameof(S3Options)));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            S3Options s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;

            var config = new AmazonS3Config
            {
                ServiceURL = s3Options.Endpoint,
                UseHttp = !s3Options.WithSsl,
                ForcePathStyle = true
            };

            return new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, config);
        });

        services.AddScoped<IS3Provider, S3Provider>();

        services.AddHostedService<S3BucketInitializationBackgroundService>();

        services.AddTransient<IChunkSizeCalculator, ChunkSizeCalculator>();

        return services;
    }
}