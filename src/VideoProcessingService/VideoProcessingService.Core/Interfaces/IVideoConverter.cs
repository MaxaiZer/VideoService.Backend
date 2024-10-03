using VideoProcessingService.Core.Models;

namespace VideoProcessingService.Core.Interfaces
{
    public interface IVideoConverter
    {
        Task<ConversionResult> ConvertAsync(string inputFileFullPath, string outputDirectory);
    }
}
