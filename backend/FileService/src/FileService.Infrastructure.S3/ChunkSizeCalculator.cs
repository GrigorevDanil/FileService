using CSharpFunctionalExtensions;
using FileService.Core.MediaAssets;
using Microsoft.Extensions.Options;
using SharedService.SharedKernel;

namespace FileService.Infrastructure.S3;

public sealed class ChunkSizeCalculator : IChunkSizeCalculator
{
    private readonly S3Options _s3Options;

    public ChunkSizeCalculator(IOptions<S3Options> s3Options)
    {
        _s3Options = s3Options.Value;
    }

    public Result<(int ChuckSize, int TotalChunks), Error> Calculate(long fileSize, long recommendedChunksSizeBytes, int maxChunks)
    {
        if (fileSize <= 0)
            return GeneralErrors.ValueIsInvalid("File size must be greater than zero", "fileSize");

        if (recommendedChunksSizeBytes <= 0)
        {
            return GeneralErrors.ValueIsInvalid(
                "Recommended chunks size bytes must be greater than zero",
                "recommendedChunksSizeBytes");
        }

        if (maxChunks <= 0)
            return GeneralErrors.ValueIsInvalid("Max chunks must be greater than zero", "maxChunks");

        if (fileSize <= recommendedChunksSizeBytes)
            return ((int)fileSize, 1);

        int calculatedChunks = (int)Math.Ceiling((double)fileSize / recommendedChunksSizeBytes);

        int actualChunks = Math.Min(calculatedChunks, maxChunks);

        long chunkSize = (fileSize + actualChunks - 1) / actualChunks;

        return ((int)chunkSize, actualChunks);
    }

    public Result<(int, int), Error> Calculate(long fileSize) =>
        Calculate(fileSize, _s3Options.RecommendedChunksSizeBytes, _s3Options.MaxChunks);
}