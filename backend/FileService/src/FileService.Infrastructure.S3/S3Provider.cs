using Amazon.S3;
using Amazon.S3.Model;
using CSharpFunctionalExtensions;
using FileService.Contracts.MediaAssets.Dtos;
using FileService.Core;
using FileService.Domain.MediaAssets.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedService.SharedKernel;

namespace FileService.Infrastructure.S3;

public class S3Provider : IS3Provider, IDisposable
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Options _s3Options;
    private readonly ILogger<S3Provider> _logger;
    private readonly SemaphoreSlim _requestsSemaphore;

    public S3Provider(IAmazonS3 s3Client, IOptions<S3Options> s3Options, ILogger<S3Provider> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
        _s3Options = s3Options.Value;
        _requestsSemaphore = new SemaphoreSlim(_s3Options.MaxConcurrentRequests);
    }

    public async Task<Result<string, Error>> StartMultipartUpload(StorageKey key, MediaData mediaData,
        CancellationToken cancellationToken = default)
    {
        InitiateMultipartUploadRequest request = new()
        {
            BucketName = key.Location,
            Key = key.Value,
            ContentType = mediaData.ContentType.Value
        };

        try
        {
            InitiateMultipartUploadResponse response = await _s3Client.InitiateMultipartUploadAsync(request, cancellationToken);
            return response.UploadId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error start multipart upload");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<ChunkUploadUrl, Error>> GenerateChunkUploadUrl(StorageKey key, string uploadId, int partNumber)
    {
        GetPreSignedUrlRequest request = new()
        {
            BucketName = key.Location,
            Key = key.Value,
            Verb = HttpVerb.PUT,
            UploadId = uploadId,
            PartNumber = partNumber,
            Expires = DateTime.UtcNow.AddMinutes(_s3Options.UploadUrlExpirationMinutes),
            Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP,
        };

        try
        {
            return new ChunkUploadUrl(partNumber, await _s3Client.GetPreSignedURLAsync(request));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<ChunkUploadUrl>, Errors>> GenerateAllChunkUploadUrl(StorageKey key, string uploadId,
        int totalChunks,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Task<Result<ChunkUploadUrl, Error>>> tasks = Enumerable.Range(1, totalChunks)
            .Select(async partNumber =>
            {
                await _requestsSemaphore.WaitAsync(cancellationToken);

                try
                {
                    return await GenerateChunkUploadUrl(key, uploadId, partNumber);
                }
                finally
                {
                    _requestsSemaphore.Release();
                }
            });

        Result<ChunkUploadUrl, Error>[] allChuckUploadUrlResult = await Task.WhenAll(tasks);

        Error[] errors = allChuckUploadUrlResult
            .Where(res => res.IsFailure)
            .Select(res => res.Error)
            .ToArray();

        if (errors.Any())
            return new Errors(errors);

        return allChuckUploadUrlResult.Select(res => res.Value).ToList();
    }

    public async Task<Result<string, Error>> CompleteMultipartUploadAsync(StorageKey key, string uploadId,
        IReadOnlyList<PartETagDto> partEtags,
        CancellationToken cancellationToken = default)
    {
        CompleteMultipartUploadRequest request = new()
        {
            BucketName = key.Location,
            Key = key.Value,
            UploadId = uploadId,
            PartETags = partEtags.Select(pe => new PartETag
            {
                ETag = pe.Etag,
                PartNumber = pe.PartNumber
            }).ToList()
        };

        try
        {
            CompleteMultipartUploadResponse response = await _s3Client.CompleteMultipartUploadAsync(request, cancellationToken);
            return response.Key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<UnitResult<Error>> AbortMultipartUploadAsync(StorageKey key, string uploadId,
        CancellationToken cancellationToken = default)
    {
        var request = new AbortMultipartUploadRequest()
        {
            BucketName = key.Location,
            Key = key.Value,
            UploadId = uploadId
        };

        try
        {
            await _s3Client.AbortMultipartUploadAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abort multipart upload");
            return S3ErrorMapper.ToError(ex);
        }

        return UnitResult.Success<Error>();
    }

    public async Task<UnitResult<Error>> UploadFileAsync(StorageKey key, Stream stream, MediaData mediaData,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = key.Location,
            ContentType = mediaData.ContentType.Value,
            Key = key.Value,
            InputStream = stream,
        };

        try
        {
            await _s3Client.PutObjectAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return S3ErrorMapper.ToError(ex);
        }

        return UnitResult.Success<Error>();
    }

    public async Task<Result<string, Error>> DownloadFileAsync(StorageKey key, string tempPath,
        CancellationToken cancellationToken = default)
    {
        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), tempPath);

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);

        var request = new GetObjectRequest
        {
            BucketName = key.Location,
            Key = key.Value
        };

        try
        {
            using GetObjectResponse response = await _s3Client.GetObjectAsync(request, cancellationToken);

            string originalFileName = Path.GetFileName(key.Value);
            string extension = Path.GetExtension(originalFileName);

            string fileName = $"{key.Key}.{extension}";
            string fullPath = Path.Combine(directoryPath, fileName);

            await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await response.ResponseStream.CopyToAsync(fileStream, cancellationToken);

            return Path.Combine(tempPath, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error download file");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> DeleteFileAsync(StorageKey key, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = key.Location,
            Key = key.Value,
        };

        try
        {
            DeleteObjectResponse response = await _s3Client.DeleteObjectAsync(request,  cancellationToken);

            return response.DeleteMarker;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> GenerateUploadUrlAsync(StorageKey key, MediaData mediaData,
        CancellationToken cancellationToken = default)
    {
        GetPreSignedUrlRequest request = new()
        {
            BucketName = key.Location,
            Key = key.Value,
            Verb = HttpVerb.PUT,
            ContentType = mediaData.ContentType.Value,
            Expires = DateTime.UtcNow.AddMinutes(_s3Options.UploadUrlExpirationMinutes),
            Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP,
        };

        try
        {
            return await _s3Client.GetPreSignedURLAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<string, Error>> GenerateDownloadUrlAsync(StorageKey key)
    {
        GetPreSignedUrlRequest request = new()
        {
            BucketName = key.Location,
            Key = key.Value,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddHours(_s3Options.DownloadUrlExpirationHours),
            Protocol = _s3Options.WithSsl ? Protocol.HTTPS : Protocol.HTTP
        };

        try
        {
            return await _s3Client.GetPreSignedURLAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return S3ErrorMapper.ToError(ex);
        }
    }

    public async Task<Result<IReadOnlyList<string>, Errors>> GenerateDownloadUrlsAsync(
        IEnumerable<StorageKey> keys,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Task<Result<string, Error>>> tasks = keys.Select(async key =>
        {
            await _requestsSemaphore.WaitAsync(cancellationToken);

            try
            {
                return await GenerateDownloadUrlAsync(key);
            }
            finally
            {
                _requestsSemaphore.Release();
            }
        });

        Result<string, Error>[] downloadUrlsResult = await Task.WhenAll(tasks);

        Error[] errors = downloadUrlsResult
            .Where(res => res.IsFailure)
            .Select(res => res.Error)
            .ToArray();

        if (errors.Any())
            return new Errors(errors);

        return downloadUrlsResult.Select(res => res.Value).ToList();
    }

    public void Dispose()
    {
        _requestsSemaphore.Release();
        _requestsSemaphore.Dispose();
    }
}