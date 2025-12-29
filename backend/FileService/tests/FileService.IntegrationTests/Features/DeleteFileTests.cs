using Amazon.S3;
using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Requests;
using FileService.Core.HttpCommunication;
using FileService.Domain;
using FileService.Domain.MediaAssets;
using FileService.Domain.MediaAssets.Enums;
using FileService.Domain.MediaAssets.ValueObjects;
using FileService.Domain.VideoAssets;
using FileService.Infrastructure.S3;
using FileService.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedService.SharedKernel;

namespace FileService.IntegrationTests.Features;

public class DeleteFileTests : FileServiceBaseTests
{
    public DeleteFileTests(FileServiceTestsWebFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task DeleteFile_Should_Success()
    {
        // arrange
        CancellationToken cancellationToken = new CancellationTokenSource().Token;

        FileInfo testFile = new(Path.Combine(AppContext.BaseDirectory, "Resources", TestFileName));

        VideoAsset videoAsset = await CreateVideoAssetAsync(testFile, cancellationToken);

        // act
        Result<Guid, Errors> deleteFileResult = await DeleteFile(videoAsset.RawKey, cancellationToken);

        // assert
        Assert.True(testFile.Exists);

        Assert.True(deleteFileResult.IsSuccess);

        await ExecuteInDb(async dbContext =>
        {
            MediaAsset? mediaAsset = await dbContext.MediaAssets.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == videoAsset.Id && x.Status == MediaStatus.DELETED, cancellationToken);

            Assert.NotNull(mediaAsset);
        });

        await ExecuteInS3Client(async s3Client =>
        {
            AmazonS3Exception ex = await Assert.ThrowsAsync<AmazonS3Exception>(async () =>
            {
                await s3Client.GetObjectAsync(
                    videoAsset.RawKey.Location,
                    videoAsset.RawKey.Value,
                    cancellationToken);
            });

            var error = S3ErrorMapper.ToError(ex);

            Assert.Equal(FileErrors.ObjectNotFound(), error);
        });
    }

    private async Task<Result<Guid, Errors>> DeleteFile(StorageKey key, CancellationToken cancellationToken = default)
    {
        DeleteFileRequest request = new(key.FullPath);

        HttpResponseMessage deleteFileResponse = await AppHttpClient.DeleteAsync("/api/files/delete", request, cancellationToken);

        return await deleteFileResponse.HandleResponseAsync<Guid>(cancellationToken);
    }
}