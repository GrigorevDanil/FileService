using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Dtos;
using FileService.Core.Features.MediaAssets.Queries;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using FileService.Domain.VideoAssets;
using FileService.IntegrationTests.Infrastructure;
using SharedService.Core.HttpCommunication;
using SharedService.SharedKernel;

namespace FileService.IntegrationTests.Features;

public class GetMediaAssetInfoTests : FileServiceBaseTests
{
    public GetMediaAssetInfoTests(FileServiceTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task GetMediaAssetsInfo_Should_Success()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo testFile = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        VideoAsset videoAsset1 = await CreateVideoAssetAsync(testFile, cancellationToken);

        VideoAsset videoAsset2 = await CreateVideoAssetAsync(testFile, MediaStatus.READY, cancellationToken);

        // act
        Result<IEnumerable<MediaAssetDto>, Errors> getResult = await GetMediaAssetsInfo([videoAsset1.Id.Value, videoAsset2.Id.Value], cancellationToken);

        // assert
        Assert.True(testFile.Exists);

        Assert.True(getResult.IsSuccess);

        Assert.NotNull(getResult.Value);

        MediaAssetDto[] mediaAssets = getResult.Value.ToArray();

        Assert.NotNull(mediaAssets[0]);

        Assert.NotNull(mediaAssets[1]);

        Assert.Null(mediaAssets[0].DownloadUrl);

        Assert.NotNull(mediaAssets[1].DownloadUrl);

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset1 = await dbContext.MediaAssets.FindAsync([MediaAssetId.Of(mediaAssets[0].Id)], cancellationToken);

            Assert.NotNull(mediaAsset1);

            MediaAsset? mediaAsset2 = await dbContext.MediaAssets.FindAsync([MediaAssetId.Of(mediaAssets[1].Id)], cancellationToken);

            Assert.NotNull(mediaAsset2);
        });
    }

    [Fact]
    public async Task GetMediaAssetInfo_WithStatusUploaded_Should_Success()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo testFile = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        VideoAsset videoAsset = await CreateVideoAssetAsync(testFile, cancellationToken);

        // act
        Result<MediaAssetDto?, Errors> getResult = await GetMediaAssetInfo(videoAsset.Id.Value, cancellationToken);

        // assert
        Assert.True(testFile.Exists);

        Assert.True(getResult.IsSuccess);

        Assert.NotNull(getResult.Value);

        Assert.Null(getResult.Value.DownloadUrl);

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset = await dbContext.MediaAssets.FindAsync([videoAsset.Id], cancellationToken);

            Assert.NotNull(mediaAsset);
        });
    }

    [Fact]
    public async Task GetMediaAssetInfo_WithStatusReady_Should_Success()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo testFile = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        VideoAsset videoAsset = await CreateVideoAssetAsync(testFile, MediaStatus.READY, cancellationToken);

        // act
        Result<MediaAssetDto?, Errors> getResult = await GetMediaAssetInfo(videoAsset.Id.Value, cancellationToken);

        // assert
        Assert.True(testFile.Exists);

        Assert.True(getResult.IsSuccess);

        Assert.NotNull(getResult.Value);

        Assert.NotNull(getResult.Value.DownloadUrl);

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset = await dbContext.MediaAssets.FindAsync([videoAsset.Id], cancellationToken);

            Assert.NotNull(mediaAsset);
        });
    }

    private async Task<Result<MediaAssetDto?, Errors>> GetMediaAssetInfo(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage getMediaAssetResponse = await AppHttpClient.GetAsync("/api/files/" + mediaAssetId, cancellationToken);

        return await getMediaAssetResponse.HandleResponseAsync<MediaAssetDto>(cancellationToken);
    }

    private async Task<Result<IEnumerable<MediaAssetDto>, Errors>> GetMediaAssetsInfo(Guid[] mediaAssetIds, CancellationToken cancellationToken = default)
    {
        GetMediaAssetsInfoQuery query = new(mediaAssetIds);

        HttpResponseMessage getMediaAssetResponse = await AppHttpClient.GetAsync("/api/files/batch", query, cancellationToken);

        return (await getMediaAssetResponse.HandleResponseAsync<IEnumerable<MediaAssetDto>>(cancellationToken))!;
    }
}