//using CoreService.Application.Dto;

using VideoProcessingService.Core.Models;

namespace VideoProcessingService.Core.Interfaces
{
    public interface IVideoConverter
    {
        Task<HlsConversionResult> ConvertToHlsAsync(string inputFileFullPath, string outputDirectory);
    }
}
