using CSharpFunctionalExtensions;
using FileService.Domain.MediaAssets.ValueObjects;
using SharedService.SharedKernel;

namespace FileService.Core;

public interface IS3Provider
{
    Task<UnitResult<Error>> UploadFileAsync(StorageKey key, Stream stream, MediaData mediaData,
        CancellationToken cancellationToken = default);

    Task<Result<string, Error>> DownloadFileAsync(StorageKey key, string tempPath, CancellationToken cancellationToken = default);

    Task<Result<string, Error>> DeleteFileAsync(StorageKey key, CancellationToken cancellationToken = default);

    Task<Result<string, Error>> GenerateUploadUrlAsync(StorageKey key, MediaData mediaData, CancellationToken cancellationToken = default);

    Task<Result<string, Error>> GenerateDownloadUrlAsync(StorageKey key);

    Task<Result<IReadOnlyList<string>, Errors>> GenerateDownloadUrlsAsync(IEnumerable<StorageKey> keys, CancellationToken cancellationToken = default);
}