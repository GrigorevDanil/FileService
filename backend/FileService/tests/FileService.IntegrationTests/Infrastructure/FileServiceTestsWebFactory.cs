using System.Data.Common;
using Amazon.S3;
using FileService.Core;
using FileService.Infrastructure.Postgres;
using FileService.Infrastructure.S3;
using FileService.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using Respawn;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace FileService.IntegrationTests.Infrastructure;

public class FileServiceTestsWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres")
        .WithDatabase("file_service_db_tests")
        .WithUsername("admin")
        .WithPassword("admin")
        .Build();

    private readonly MinioContainer _minioContainer = new MinioBuilder()
        .WithImage("minio/minio")
        .WithUsername("admin")
        .WithPassword("adminadmin")
        .Build();

    private Respawner _respawner = null!;

    private DbConnection _dbConnection = null!;

    public async Task InitializeAsync()
    {
        await _minioContainer.StartAsync();

        await _dbContainer.StartAsync();

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        await InitializeRespawnerAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();

        await _dbConnection.CloseAsync();
        await _dbConnection.DisposeAsync();

        await _minioContainer.StopAsync();
        await _minioContainer.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Tests.json"), optional:true);
        });

        builder.ConfigureTestServices(services =>
        {
            services.ConfigureDbContext(_dbContainer.GetConnectionString());

            services.ConfigureS3($"http://{_minioContainer.Hostname}:{_minioContainer.GetMappedPublicPort(9000)}");
        });
    }

    private async Task InitializeRespawnerAsync()
    {
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });
    }
}

public static class ConfiguratorDbContext
{
    public static void ConfigureDbContext(this IServiceCollection services, string connectionString)
    {
        services.RemoveAll<AppDbContext>();
        services.RemoveAll<IReadDbContext>();

        services.AddDbContextPool<AppDbContext>((_, options) =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddDbContextPool<IReadDbContext, AppDbContext>((_, options) =>
        {
            options.UseNpgsql(connectionString);
        });
    }
}

public static class ConfiguratorS3
{
    public static void ConfigureS3(this IServiceCollection services, string serviceUrl)
    {
        services.RemoveAll<IAmazonS3>();

        services.AddSingleton<IAmazonS3>(sp =>
        {
            S3Options s3Options = sp.GetRequiredService<IOptions<S3Options>>().Value;

            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                UseHttp = true,
                ForcePathStyle = true
            };

            return new AmazonS3Client(s3Options.AccessKey, s3Options.SecretKey, config);
        });
    }
}