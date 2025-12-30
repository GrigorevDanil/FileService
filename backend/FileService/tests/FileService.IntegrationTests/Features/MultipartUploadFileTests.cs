using System.Net.Http.Json;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Dtos;
using FileService.Contracts.MediaAssets.Requests;
using FileService.Contracts.MediaAssets.Responses;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using FileService.IntegrationTests.Infrastructure;
using SharedService.Core.HttpCommunication;
using SharedService.SharedKernel;
using AbortMultipartUploadRequest = FileService.Contracts.MediaAssets.Requests.AbortMultipartUploadRequest;
using CompleteMultipartUploadRequest = FileService.Contracts.MediaAssets.Requests.CompleteMultipartUploadRequest;

namespace FileService.IntegrationTests.Features;

public class MultipartUploadFileTests : FileServiceBaseTests
{
    public MultipartUploadFileTests(FileServiceTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task AbortMultipartUpload_Should_Success()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo testFile = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        // act
        StartMultipartUploadResponse startMultipartUploadResponse = await StartMultipartUpload(testFile, cancellationToken);

        UnitResult<Errors> abortResult = await AbortMultipartUpload(startMultipartUploadResponse, cancellationToken);

        // assert
        Assert.True(testFile.Exists);

        Assert.True(abortResult.IsSuccess);

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset = await dbContext.MediaAssets.FindAsync([MediaAssetId.Of(startMultipartUploadResponse.MediaAssetId)], cancellationToken);

            Assert.NotNull(mediaAsset);
            Assert.Equal(MediaStatus.FAILED, mediaAsset.Status);
        });
    }

    [Fact]
    public async Task GetChunkUploadUrl_Should_Success()
    {
        // arrange
        const int requestedPartNumber = 1;

        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo testFile = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        // act
        StartMultipartUploadResponse startMultipartUploadResponse = await StartMultipartUpload(testFile, cancellationToken);

        Result<string, Errors> getChunkUploadUrlResult = await GetChunkUploadUrl(startMultipartUploadResponse, requestedPartNumber, cancellationToken);

        // assert
        Assert.True(testFile.Exists);

        Assert.True(getChunkUploadUrlResult.IsSuccess);

        Assert.NotNull(getChunkUploadUrlResult.Value);

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset = await dbContext.MediaAssets.FindAsync([MediaAssetId.Of(startMultipartUploadResponse.MediaAssetId)], cancellationToken);

            Assert.NotNull(mediaAsset);
            Assert.Equal(MediaStatus.UPLOADING, mediaAsset.Status);
        });
    }

    [Fact]
    public async Task GetChunkUploadUrl_Should_Fail()
    {
        // arrange
        const int requestedPartNumber = 2;

        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo testFile = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        // act
        StartMultipartUploadResponse? startMultipartUploadResponse = await StartMultipartUpload(testFile, cancellationToken);

        Result<string, Errors> getChunkUploadUrlResult = await GetChunkUploadUrl(startMultipartUploadResponse, requestedPartNumber, cancellationToken);

        // assert
        Assert.True(testFile.Exists);

        Assert.True(getChunkUploadUrlResult.IsFailure);

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset = await dbContext.MediaAssets.FindAsync([MediaAssetId.Of(startMultipartUploadResponse.MediaAssetId)], cancellationToken);

            Assert.NotNull(mediaAsset);
            Assert.Equal(MediaStatus.UPLOADING, mediaAsset.Status);
        });
    }

    [Fact]
    public async Task MultipartUpload_FullCycle_PersistsMediaFile()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo testFile = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        // act
        StartMultipartUploadResponse startMultipartUploadResponse = await StartMultipartUpload(testFile, cancellationToken);

        List<PartETagDto> parts = await UploadChunksAsync(
            testFile,
            startMultipartUploadResponse,
            cancellationToken);

        UnitResult<Errors> completeResult = await CompleteMultipartUpload(startMultipartUploadResponse, parts, cancellationToken);

        // assert
        Assert.True(testFile.Exists);

        Assert.True(completeResult.IsSuccess);

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset = await dbContext.MediaAssets.FindAsync([MediaAssetId.Of(startMultipartUploadResponse.MediaAssetId)], cancellationToken);

            Assert.NotNull(mediaAsset);
            Assert.Equal(MediaStatus.UPLOADED, mediaAsset.Status);

            await ExecuteInS3Client(async s3Client =>
            {
                GetObjectResponse objectResponse = await s3Client.GetObjectAsync(
                    mediaAsset.RawKey.Location,
                    mediaAsset.RawKey.Value,
                    cancellationToken);

                Assert.Equal(testFile.Length, objectResponse.ContentLength);
                Assert.Equal(mediaAsset.RawKey.Value, objectResponse.Key);
            });
        });
    }

    private async Task<UnitResult<Errors>> AbortMultipartUpload(StartMultipartUploadResponse startResponse, CancellationToken cancellationToken = default)
    {
        AbortMultipartUploadRequest request = new(startResponse.MediaAssetId, startResponse.UploadId);

        HttpResponseMessage abortResponse = await AppHttpClient.PostAsJsonAsync("/api/files/multipart/abort", request, cancellationToken);

        return await abortResponse.HandleResponseAsync(cancellationToken);
    }

    private async Task<Result<string, Errors>> GetChunkUploadUrl(StartMultipartUploadResponse startResponse, int partNumber, CancellationToken cancellationToken = default)
    {
        GetChuckUploadUrlRequest request = new(startResponse.MediaAssetId, startResponse.UploadId, partNumber);

        HttpResponseMessage getChunkUploadUrlResponse = await AppHttpClient.GetAsync("/api/files/multipart/url", request, cancellationToken);

        return (await getChunkUploadUrlResponse.HandleResponseAsync<string>(cancellationToken))!;
    }

    private async Task<StartMultipartUploadResponse> StartMultipartUpload(FileInfo file, CancellationToken cancellationToken = default)
    {
        // arrange
        StartMultipartUploadRequest request = new(
            file.Name,
            "video/mp4",
            file.Length,
            "video",
            "department",
            Guid.NewGuid());

        // act
        HttpResponseMessage startMultipartResponse = await AppHttpClient.PostAsJsonAsync("/api/files/multipart/start", request, cancellationToken);

        Result<StartMultipartUploadResponse?, Errors> startMultipartResult = await startMultipartResponse.HandleResponseAsync<StartMultipartUploadResponse>(cancellationToken);

        // assert
        Assert.True(startMultipartResult.IsSuccess);

        Assert.NotNull(startMultipartResult.Value);

        Assert.NotNull(startMultipartResult.Value.UploadId);

        StartMultipartUploadResponse startMultipartData = startMultipartResult.Value;

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset = await dbContext.MediaAssets.FindAsync([MediaAssetId.Of(startMultipartData.MediaAssetId)], cancellationToken);

            Assert.NotNull(mediaAsset);
            Assert.Equal(MediaStatus.UPLOADING, mediaAsset.Status);
        });

        return startMultipartData;
    }

    private async Task<UnitResult<Errors>> CompleteMultipartUpload(StartMultipartUploadResponse startResponse, IReadOnlyList<PartETagDto> parts, CancellationToken cancellationToken = default)
    {
        var request = new CompleteMultipartUploadRequest(
            startResponse.MediaAssetId,
            startResponse.UploadId,
            parts);

        HttpResponseMessage startMultipartResponse = await AppHttpClient.PostAsJsonAsync("/api/files/multipart/complete", request, cancellationToken);

        return await startMultipartResponse.HandleResponseAsync(cancellationToken);
    }

    private async Task<List<PartETagDto>> UploadChunksAsync(FileInfo file, StartMultipartUploadResponse startResponse, CancellationToken cancellationToken = default)
    {
        await using FileStream stream = file.OpenRead();

        List<PartETagDto> parts = [];

        foreach (ChunkUploadUrl chunkUploadUrl in startResponse.ChunkUploadUrls.OrderBy(x => x.PartNumber))
        {
            byte[] chunk = new byte[startResponse.ChunkSize];
            int bytesRead = await stream.ReadAsync(chunk.AsMemory(0, startResponse.ChunkSize), cancellationToken);

            if (bytesRead == 0)
                break;

            var content = new ByteArrayContent(chunk);

            HttpResponseMessage response = await HttpClient.PutAsync(chunkUploadUrl.UploadUrl, content, cancellationToken);

            string? etag = response.Headers.ETag?.Tag.Trim('"');

            parts.Add(new PartETagDto(chunkUploadUrl.PartNumber, etag!));
        }

        return parts;
    }
}