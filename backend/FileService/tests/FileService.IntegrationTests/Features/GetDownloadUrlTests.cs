using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Requests;
using FileService.Core.HttpCommunication;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.ValueObjects;
using FileService.Domain.VideoAssets;
using FileService.IntegrationTests.Infrastructure;
using SharedService.SharedKernel;

namespace FileService.IntegrationTests.Features;

public class GetDownloadUrlTests : FileServiceBaseTests
{
    public GetDownloadUrlTests(FileServiceTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetDownloadUrl_Should_Success()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo testFile = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        VideoAsset videoAsset = await CreateVideoAssetAsync(testFile, cancellationToken);

        // act
        Result<string, Errors> getDownloadUrlResult = await GetDownloadUrl(videoAsset.RawKey, cancellationToken);

        // assert
        Assert.True(testFile.Exists);

        Assert.True(getDownloadUrlResult.IsSuccess);

        Assert.NotNull(getDownloadUrlResult.Value);

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset = await dbContext.MediaAssets.FindAsync([videoAsset.Id], cancellationToken);

            Assert.NotNull(mediaAsset);
        });
    }

    [Fact]
    public async Task GetDownloadUrls_Should_Success()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo testFile = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        VideoAsset videoAsset1 = await CreateVideoAssetAsync(testFile, cancellationToken);

        VideoAsset videoAsset2 = await CreateVideoAssetAsync(testFile, cancellationToken);

        // act
        Result<string[], Errors> getDownloadUrlsResult = await GetDownloadUrls([videoAsset1.RawKey, videoAsset2.RawKey], cancellationToken);

        // assert
        Assert.True(testFile.Exists);

        Assert.True(getDownloadUrlsResult.IsSuccess);

        Assert.NotNull(getDownloadUrlsResult.Value);

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset1 = await dbContext.MediaAssets.FindAsync([videoAsset1.Id], cancellationToken);
            MediaAsset? mediaAsset2 = await dbContext.MediaAssets.FindAsync([videoAsset2.Id], cancellationToken);

            Assert.NotNull(mediaAsset1);
            Assert.NotNull(mediaAsset2);
        });
    }

    private async Task<Result<string, Errors>> GetDownloadUrl(StorageKey key, CancellationToken cancellationToken = default)
    {
        GetDownloadUrlRequest request = new(key.FullPath);

        HttpResponseMessage getDownloadUrlResponse = await AppHttpClient.GetAsync("/api/files/download/url", request, cancellationToken);

        return await getDownloadUrlResponse.HandleResponseAsync<string>(cancellationToken);
    }

    private async Task<Result<string[], Errors>> GetDownloadUrls(StorageKey[] keys, CancellationToken cancellationToken = default)
    {
        GetDownloadUrlsRequest request = new(keys.Select(x => x.FullPath).ToArray());

        HttpResponseMessage getDownloadUrlResponse = await AppHttpClient.GetAsync("/api/files/download/urls", request, cancellationToken);

        return await getDownloadUrlResponse.HandleResponseAsync<string[]>(cancellationToken);
    }
}