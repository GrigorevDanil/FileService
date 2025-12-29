using CSharpFunctionalExtensions;
using SharedService.SharedKernel;

namespace FileService.Core.MediaAssets;

public interface IChunkSizeCalculator
{
    Result<(int ChuckSize, int TotalChunks), Error> Calculate(
        long fileSize,
        long recommendedChunksSizeBytes,
        int maxChunks);

    Result<(int ChuckSize, int TotalChunks), Error> Calculate(long fileSize);
}