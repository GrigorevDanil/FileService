namespace FileService.Core;

public interface IS3Provider
{
    Task UploadFileAsync(string bucketName, string key, string contentType, Stream stream,
        CancellationToken cancellationToken = default);

    Task<string> GenerateDownloadUrlAsync(string bucketName, string key);
}