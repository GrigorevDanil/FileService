using SharedService.SharedKernel;

namespace FileService.Domain;

public static class FileErrors
{
    public static Error BucketNotFound(string? bucketName = null)
        => Error.NotFound($"Bucket not found", "BUCKET_NOT_FOUND");

    public static Error UploadNotFound(string? uploadId = null)
        => Error.NotFound($"Download session not found", "UPLOAD_NOT_FOUND");

    public static Error ObjectNotFound(string? objectKey = null)
    {
        string text = objectKey is not null ? $"with key {objectKey} " : string.Empty;
        return Error.NotFound($"Object {text}not found", "OBJECT_NOT_FOUND");
    }

    public static Error ValidationFailed()
        => Error.Validation("The request contains uncorrected data", null, "VALIDATION_FAILED");

    public static Error InternalServerError()
        => Error.Failure("Internal server error", "INTERNAL_SERVER_ERROR");

    public static Error NetworkError()
        => Error.Failure("Network error when interacting with file storage", "NETWORK_ERROR");

    public static Error OperationCanceled()
        => Error.Failure("The operation was cancelled", "OPERATION_CANCELLED");
}