using CSharpFunctionalExtensions;
using SharedService.SharedKernel;

namespace FileService.Core.MediaAssets;

public interface IChunkSizeCalculator
{
    Result<(long ChuckSize, int TotalChunks), Error> Calculate(
        long fileSize,
        long recommendedChunksSizeBytes,
        int maxChunks);

    Result<(long ChuckSize, int TotalChunks), Error> Calculate(long fileSize);
}