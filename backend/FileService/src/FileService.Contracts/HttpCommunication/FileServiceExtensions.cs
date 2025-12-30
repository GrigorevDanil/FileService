using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileService.Contracts.HttpCommunication;

public static class FileServiceExtensions
{
    public static IServiceCollection AddFileServiceHttpCommunication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileServiceOptions>(configuration.GetSection(nameof(FileServiceOptions)));

        services.AddHttpClient<IFileCommunicationService, FileHttpClient>((sp, config) =>
        {
            FileServiceOptions fileOptions = sp.GetRequiredService<IOptions<FileServiceOptions>>().Value;

            config.BaseAddress = new Uri(fileOptions.Url);

            config.Timeout = TimeSpan.FromSeconds(fileOptions.TimeoutSeconds);
        });

        return services;
    }
}