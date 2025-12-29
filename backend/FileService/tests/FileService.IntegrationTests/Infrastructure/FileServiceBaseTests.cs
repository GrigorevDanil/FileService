using Amazon.S3;
using CSharpFunctionalExtensions;
using FileService.Core;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using FileService.Domain.VideoAssets;
using FileService.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;
using SharedService.SharedKernel;

namespace FileService.IntegrationTests.Infrastructure;

public class FileServiceBaseTests : IClassFixture<FileServiceTestsWebFactory>, IAsyncLifetime
{
    protected const string TestFileName = "test_file.mp4";

    protected HttpClient AppHttpClient { get; }

    protected HttpClient HttpClient { get; }

    private readonly IServiceProvider _serviceProvider;

    private readonly Func<Task> _resetDatabaseAsync;

    protected FileServiceBaseTests(FileServiceTestsWebFactory factory)
    {
        AppHttpClient = factory.CreateClient();
        HttpClient = new HttpClient();
        _serviceProvider = factory.Services;
        _resetDatabaseAsync = factory.ResetDatabaseAsync;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _resetDatabaseAsync();
        AppHttpClient.Dispose();
        HttpClient.Dispose();
    }

    protected async Task<VideoAsset> CreateVideoAssetAsync(FileInfo file, MediaStatus status, DateTime statusChangeAt, CancellationToken cancellationToken = default)
    {
        VideoAsset videoAsset = await ExecuteInDb(async dbContext =>
        {
            var mediaAssetId = MediaAssetId.Create();

            var mediaData = new MediaData(
                FileName.Of(file.Name).Value,
                ContentType.Of("video/mp4").Value,
                FileSize.Of(file.Length).Value,
                ExpectedChunksCount.Of(1).Value);

            var mediaOwner = new MediaOwner("department", Guid.NewGuid());

            VideoAsset videoAsset = VideoAsset.CreateForUpload(mediaAssetId, mediaData, mediaOwner).Value;

            dbContext.MediaAssets.Add(videoAsset);
            await dbContext.SaveChangesAsync(cancellationToken);

            await ExecuteInS3Provider(async s3Provider =>
            {
                await s3Provider.UploadFileAsync(videoAsset.RawKey, file.OpenRead(), mediaData, cancellationToken);
            });

            if (status != MediaStatus.UPLOADING)
            {
                switch (status)
                {
                    case MediaStatus.FAILED: videoAsset.MarkFailed(statusChangeAt); break;

                    case MediaStatus.UPLOADED: videoAsset.MarkUploaded(statusChangeAt); break;

                    case MediaStatus.DELETED: {
                            videoAsset.MarkUploaded(statusChangeAt);
                            videoAsset.MarkDeleted(statusChangeAt);
                            break;
                        }

                    case MediaStatus.READY:
                        {
                            videoAsset.MarkUploaded(statusChangeAt);
                            videoAsset.MarkReady(videoAsset.RawKey, statusChangeAt);
                            break;
                        }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return videoAsset;
        });

        return videoAsset;
    }

    protected Task<VideoAsset> CreateVideoAssetAsync(FileInfo file, MediaStatus status, CancellationToken cancellationToken = default) =>
        CreateVideoAssetAsync(file, status, DateTime.UtcNow, cancellationToken);

    protected Task<VideoAsset> CreateVideoAssetAsync(FileInfo file, CancellationToken cancellationToken = default) =>
        CreateVideoAssetAsync(file, MediaStatus.UPLOADED, DateTime.UtcNow, cancellationToken);

    protected async Task<TResult> Execute<TResult, TService>(Func<TService, Task<TResult>> action)
        where TService : notnull
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        TService handler = scope.ServiceProvider.GetRequiredService<TService>();
        return await action(handler);
    }

    protected async Task Execute<TService>(Func<TService, Task> action)
        where TService : notnull
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        TService handler = scope.ServiceProvider.GetRequiredService<TService>();
        await action(handler);
    }

    protected Task<T> ExecuteInDb<T>(Func<AppDbContext, Task<T>> action) => Execute(action);

    protected Task ExecuteInDb(Func<AppDbContext, Task> action) => Execute(action);

    protected Task ExecuteInS3Client(Func<IAmazonS3, Task> action) => Execute(action);

    protected Task<T> ExecuteInS3Client<T>(Func<IAmazonS3, Task<T>> action) => Execute(action);

    protected Task ExecuteInS3Provider(Func<IS3Provider, Task> action) => Execute(action);
}