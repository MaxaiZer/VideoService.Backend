using System.Globalization;
using Microsoft.Extensions.Options;

namespace VideoProcessingService.Infrastructure.Video;

public record ThumbnailCreationResult(string ImagePath);

public class ThumbnailCreator
{
    private readonly MediaProcessor _processor;
    
    public ThumbnailCreator(IOptions<ConversionConfiguration> conversionConfig)
    {
        _processor = new MediaProcessor(conversionConfig);
    }

    public async Task<ThumbnailCreationResult> Create(string videoPath, double timestampSeconds)
    {
        string directory = Path.GetDirectoryName(videoPath) ?? Path.GetTempPath();
        string thumbnailPath = Path.Combine(directory, "thumbnail.jpg");
        
        string arguments = $"-ss {timestampSeconds.ToString("0.0", CultureInfo.InvariantCulture)} -i {videoPath} -vframes 1 -update 1 {thumbnailPath}";
        
        await _processor.StartProcess(MediaProcessor.Program.FFmpeg, directory, arguments);
        return new ThumbnailCreationResult(thumbnailPath);
    }
}