using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Dtos;
using FileService.Domain.MediaAssets.ValueObjects;
using SharedService.SharedKernel;

namespace FileService.Core;

public interface IS3Provider
{
    Task<Result<string, Error>> StartMultipartUpload(StorageKey key, MediaData mediaData, CancellationToken cancellationToken = default);

    Task<Result<ChunkUploadUrl, Error>> GenerateChunkUploadUrl(StorageKey key, string uploadId, int partNumber);

    Task<Result<IReadOnlyList<ChunkUploadUrl>, Errors>> GenerateAllChunkUploadUrl(StorageKey key, string uploadId, int totalChunks, CancellationToken cancellationToken = default);

    Task<Result<string, Error>> CompleteMultipartUploadAsync(StorageKey key, string uploadId, IReadOnlyList<PartETagDto> partEtags, CancellationToken cancellationToken = default);

    Task<UnitResult<Error>> AbortMultipartUploadAsync(StorageKey key, string uploadId, CancellationToken cancellationToken = default);

    Task<UnitResult<Error>> UploadFileAsync(StorageKey key, Stream stream, MediaData mediaData,
        CancellationToken cancellationToken = default);

    Task<Result<string, Error>> DownloadFileAsync(StorageKey key, string tempPath, CancellationToken cancellationToken = default);

    Task<Result<string, Error>> DeleteFileAsync(StorageKey key, CancellationToken cancellationToken = default);

    Task<Result<string, Error>> GenerateUploadUrlAsync(StorageKey key, MediaData mediaData, CancellationToken cancellationToken = default);

    Task<Result<string, Error>> GenerateDownloadUrlAsync(StorageKey key);

    Task<Result<IReadOnlyList<string>, Errors>> GenerateDownloadUrlsAsync(IEnumerable<StorageKey> keys, CancellationToken cancellationToken = default);
}