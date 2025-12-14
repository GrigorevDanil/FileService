using FileService.Core;
using FileService.Core.MediaAssets;
using FileService.Infrastructure.Postgres.Database;
using FileService.Infrastructure.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedService.Core.Database;

namespace FileService.Infrastructure.Postgres;

public static class Registration
{
    public static IServiceCollection AddInfrastructurePostgres(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextPool<AppDbContext>(ConfigureDbContext);

        services.AddDbContextPool<IReadDbContext, AppDbContext>(ConfigureDbContext);

        services.AddScoped<IMediaRepository, MediaRepository>();

        services.AddScoped<ITransactionManager, TransactionManager>();

        return services;

        void ConfigureDbContext(IServiceProvider sp, DbContextOptionsBuilder options)
        {
            string? connectionString = configuration.GetConnectionString(Constants.DATABASE);

            IHostEnvironment hostEnvironment = sp.GetRequiredService<IHostEnvironment>();
            ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            options.UseNpgsql(connectionString);

            if (hostEnvironment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            options.UseLoggerFactory(loggerFactory);
        }
    }
}