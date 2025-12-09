using Amazon.S3;
using FileService.Domain;
using Microsoft.AspNetCore.Authentication;
using SharedService.SharedKernel;

namespace FileService.Infrastructure.S3;

public static class S3ErrorMapper
{
    public static Error ToError(Exception ex) => ex switch
    {
        AmazonS3Exception { ErrorCode: "NoSuchBucket" } => FileErrors.BucketNotFound(),

        AmazonS3Exception { ErrorCode: "AccessDenied" or "SignatureDoesNotMatch" or "InvalidAccessKeyId" }
            => GeneralErrors.Forbidden(),

        AmazonS3Exception { ErrorCode: "InvalidRequest" or "InvalidArgument" } => FileErrors.ValidationFailed(),

        AmazonS3Exception { ErrorCode: "InternalError" } => FileErrors.InternalServerError(),

        AmazonS3Exception { ErrorCode: "NoSuchKey" } => FileErrors.ObjectNotFound(),

        AmazonS3Exception { ErrorCode: "NoSuchUpload" } => FileErrors.UploadNotFound(),

        ArgumentException => FileErrors.ValidationFailed(),

        HttpRequestException => FileErrors.NetworkError(),

        OperationCanceledException => FileErrors.OperationCanceled(),

        _ => GeneralErrors.Failure(ex.Message)
    };
}