using Microsoft.Extensions.Configuration;

namespace VideoProcessingService.Infrastructure.Video;

public record ThumbnailCreationResult(string ImagePath);

public class ThumbnailCreator
{
    private readonly MediaProcessor _processor;
    
    public ThumbnailCreator(IConfiguration configuration)
    {
        _processor = new MediaProcessor(configuration);
    }

    public async Task<ThumbnailCreationResult> Create(string videoPath, int frameTimeInSec)
    {
        string directory = Path.GetDirectoryName(videoPath) ?? Path.GetTempPath();
        string thumbnailPath = Path.Combine(directory, "thumbnail.jpg");
        
        string arguments = $"-i {videoPath} -vf \"select='eq(n,{frameTimeInSec})'\" -vframes 1 {thumbnailPath}";
        
        await _processor.StartProcess(MediaProcessor.Program.FFmpeg, directory, arguments);
        return new ThumbnailCreationResult(thumbnailPath);
    }
}