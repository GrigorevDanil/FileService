using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileService.Infrastructure.S3;

public class S3BucketInitializationBackgroundService : BackgroundService
{
    private readonly S3Options _s3Options;
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3BucketInitializationBackgroundService> _logger;

    public S3BucketInitializationBackgroundService(
        IOptions<S3Options> s3Options,
        IAmazonS3 s3Client,
        ILogger<S3BucketInitializationBackgroundService> logger)
    {
        _s3Options = s3Options.Value;
        _s3Client = s3Client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("S3BucketInitialization started");

            if (_s3Options.RequiredBuckets.Count == 0)
            {
                _logger.LogInformation("S3BucketInitialization required buckets");

                throw new ArgumentException("S3BucketInitialization required buckets");
            }

            _logger.LogInformation("S3BucketInitialization required buckets: {Buckets}", string.Join(", ", _s3Options.RequiredBuckets));

            Task[] tasks = _s3Options.RequiredBuckets.Select(bucket => InitializeBucketAsync(bucket, stoppingToken)).ToArray();

            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("S3BucketInitialization cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "S3BucketInitialization terminated");
            throw;
        }
    }

    private async Task InitializeBucketAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        try
        {
            bool bucketExist = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, bucketName);

            if (bucketExist)
            {
                _logger.LogInformation("Bucket {Bucket} already exists", bucketName);
                return;
            }

            _logger.LogInformation("Creating bucket {Bucket}", bucketName);

            var putBucketRequest = new PutBucketRequest { BucketName = bucketName };

            await _s3Client.PutBucketAsync(putBucketRequest, cancellationToken);

            string policy = $$"""
                             {
                                "Version": "2012-10-17",
                                "Statement": [
                                    {
                                        "Effect": "Allow",
                                        "Principal": {
                                            "AWS": ["*"]
                                        },
                                        "Action": ["s3:GetObject"],
                                        "Resource": ["arn:aws:s3:::{{bucketName}}/*"]
                                    }
                                ]    
                             }
                             """;

            var putPolicyRequest = new PutBucketPolicyRequest { BucketName = bucketName, Policy = policy };

            await _s3Client.PutBucketPolicyAsync(putPolicyRequest, cancellationToken);

            _logger.LogInformation("Bucket '{Bucket}' created successfully", bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bucket {Bucket}", bucketName);
            throw;
        }
    }
}